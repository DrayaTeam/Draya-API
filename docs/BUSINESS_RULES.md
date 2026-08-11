# BUSINESS_RULES.md

> Every rule below was explicitly agreed during requirements gathering. Where the team's answer changed between rounds (e.g., parent report approval), the **latest/final** answer is presented as the rule, with the superseded version noted for traceability. Nothing has been invented — anything not explicitly discussed is marked **Assumption**.

---

## 1. Accounts & Authentication

**Description:** Identity and access for the three platform roles.

**User roles:** Teacher, Student, SuperAdmin.

**Business rules:**
- Teachers **self-register** — no invite, no admin approval required to create an account.
- Students **self-register** — no invite, no admin approval required.
- There is **no Academy Admin role**. Teachers operate independently; no organizational layer exists above them except the platform-level SuperAdmin.
- The **Platform Super Admin** manages: teacher account management, subscription management, platform monitoring, abuse handling, technical support, global platform settings. The Super Admin **does not** manage teachers' classrooms or student data.
- Every Student profile must have exactly **one** parent/guardian email address. Parents themselves have **no account, no login, no RBAC** — this was an explicit decision, not an oversight.

**Edge cases:**
- A student cannot register without providing a guardian email (required field).
- Deactivating a teacher account (Super Admin action) does not delete their classrooms/data — this is a soft-deactivation, not a destructive delete (consistent with the soft-delete strategy in `DATABASE.md`).

**Validation rules:**
- Email uniqueness enforced platform-wide across all roles (single `AppUser.Email` unique constraint).
- Password policy: minimum 8 characters, at least 1 number, at least 1 uppercase letter (as documented in `API_CONTRACT.md`).

**Permission rules:**
- A Teacher can only read/write their own classrooms, materials, exams, and question bank.
- A Student can only read classrooms they are enrolled in, and cannot view draft/unpublished exams.
- SuperAdmin has platform-wide visibility into accounts, plans, and payouts, but not into individual classroom content or grading data.

---

## 2. Classrooms & Enrollment

**Description:** The core teaching unit and how students join it.

**User roles:** Teacher (owner/creator), Student (joiner).

**Business rules:**
- A **Classroom** belongs to exactly one Teacher and represents exactly one Subject.
- Students **enroll in classrooms**, not directly under a teacher — a student can be enrolled in **multiple classrooms across multiple different teachers** simultaneously.
- Students join a classroom via an **enrollment code** provided by the teacher.
- **No approval workflow exists for enrollment.** Once a student enters a valid code (and, for paid classrooms, completes payment — see §3), enrollment is instant. There is no `PendingEnrollment` state.
- Subjects are treated as a **shared, platform-wide taxonomy** (e.g., "Math," "Physics") rather than private per-teacher free text. **[Assumption — flagged, not explicitly confirmed; can be changed to per-teacher scoping if wrong.]**

**Enrollment/payment rules:** *(see §3 for the full payment flow)*
- **Free classrooms:** instant enrollment on valid code entry.
- **Paid classrooms:** enrollment code entry alone is insufficient — the student must also complete a Paymob checkout, and enrollment is only finalized after a verified payment webhook is received.
- A student cannot enroll twice in the same classroom (unique constraint on Student+Classroom).
- No capacity or subscription limits exist on enrollment.

**Edge cases:**
- A teacher regenerating the enrollment code invalidates the old code immediately.
- A teacher can remove a student from a classroom at any time — this does not currently refund a paid enrollment (refund policy is undefined — see `DECISIONS.md` open items).

**Validation rules:**
- Enrollment code must be unique per classroom and currently active.

**Permission rules:**
- Only the owning Teacher can view the full roster, remove students, regenerate codes, or set pricing.
- A Student can only see classrooms they are personally enrolled in.

---

## 3. Payments (Paymob) & Wallet Model

**Description:** Monetiation of classroom enrollment, teacher wallet top-ups, payouts, and AI exam usage.

**User roles:** Teacher (sets price, earns, requests withdrawal, top-ups), Student (pays for enrollment), SuperAdmin (manages platform settings, processes withdrawals, makes adjustments).

**Business rules:**
- Teachers may set a **price** for a classroom (`0` = free, which is the default). Currency is **EGP only**.
- **Paymob** is the payment gateway for classroom enrollment and teacher top-ups.
- **Draya is the merchant of record** — a single platform-level Paymob integration is used.
- Draya deducts a **configurable platform commission percentage** (`PlatformSetting.PlatformCommissionPercent`) snapshotted at payment time upon successful classroom enrollment webhook processing.
- The net teacher amount (`GrossAmount - CommissionAmount`) is credited to `TeacherWallet.EarnedBalance` via an append-only `WalletTransaction` (`Type: ClassroomEarning`).
- Teachers can top up non-withdrawable `PurchasedBalance` for AI exam generation beyond monthly free quota (`PlatformSetting.FreeMonthlyAIExamQuota`).
- Teachers can request withdrawals of `EarnedBalance` up to their available earned balance. SuperAdmin reviews and approves/rejects/marks as paid.
- **The client (web/mobile app) is never trusted to report payment success.** Only signature-verified webhooks from Paymob finalize payments.
- Webhook processing must be strictly **idempotent**.

**Edge cases:**
- **Duplicate webhook delivery:** Paymob may retry webhook delivery; the system must recognize an already-`Paid` transaction and no-op rather than double-process it or error.
- **Payment succeeds, but Enrollment creation fails** (e.g., a capacity race — two students paying for the last slot simultaneously): this must trigger a refund; the exact automated workflow for this is **not yet defined** — flagged as an open item.
- **Teacher deletes/deactivates a classroom after a student has paid but before use:** refund policy is **not yet defined** — flagged as an open item.
- A student attempting to pay for a classroom they're already enrolled in should be blocked (409 Conflict).
- A student attempting to pay for a free classroom should be redirected to the free-enrollment flow instead (422 error).

**Validation rules:**
- `Price` must be `>= 0`.
- A `PaymentTransaction.Amount` must always match the classroom's price *at the moment checkout was initiated* — it is never accepted from client input.

**Permission rules:**
- Only the owning Teacher can set/update a classroom's price.
- A Teacher can view only their own payout statements; SuperAdmin can view all.
- Only SuperAdmin can mark a payout statement as paid (this reflects an out-of-band bank transfer, not an in-app payment gateway call).

---

## 4. Learning Materials & RAG Content Pipeline

**Description:** How lesson content is uploaded, versioned, and prepared for AI use.

**User roles:** Teacher (uploads), Student (consumes, read-only).

**Business rules:**
- Supported formats for **AI-processed** lesson materials: **PDF, DOCX, PPTX**.
- **Video** and **Image** files are supported as learning resources with full in-platform streaming/viewing, but are **explicitly excluded** from the RAG pipeline and AI question generation.
- The RAG pipeline extracts **paragraphs, headings, lists, and text-based tables (best effort)** only. **Images, diagrams, charts, and complex mathematical equations are out of scope** and this limitation is formally acknowledged, not a bug to fix.
- **Lesson versioning:** every upload or edit of a `LearningMaterial` creates a new `MaterialVersion`, preserving all prior versions.
- **Exams remain permanently linked to the exact lesson version used at generation time.** Updating a lesson's content does **not** modify any exam already generated from a prior version — only future exam generations use the newest version.
- Content is chunked using **fixed-size chunking with overlap**: 1,000 characters per chunk, 150-character overlap, tagged with `SubjectId`, `LessonId`, and `DifficultyLevel` metadata to enable pre-filtering in Qdrant before vector similarity search.

**Edge cases:**
- Deleting a `LearningMaterial` is a **soft delete** — historical exams referencing its versions remain intact and functional.
- A material stuck in `ParseStatus: Failed` should not silently block exam generation from other, successfully parsed materials.

**Validation rules:**
- File size is bounded by the uploading teacher's `SubscriptionPlan.MaxStorageMB`.
- Only PDF/DOCX/PPTX/Video/Image are accepted `materialType` values.

**Permission rules:**
- Only the owning Teacher can upload/edit/delete materials for their classroom.
- Enrolled students have read/stream access only.

---

## 5. Question Bank & Exams

**Description:** How questions and exams are created, either by AI or manually.

**User roles:** Teacher (author/curator), Student (taker, published exams only).

**Business rules:**
- Supported question types: **Multiple Choice (MCQ), True/False, Fill in the Blank, Short Answer, Essay**.
- **Questions belong to a per-teacher Question Bank**, independent of any single exam — the same question can be reused across multiple exams (with possibly different point values on each).
- **Teachers can manually create, edit, organize, and delete questions.** AI is an **assistant** that can generate suggested questions — it is not the only way to create content.
- **Agent 1 (Exam Builder)** determines the exact text snippets extracted from RAG chunks and the distractor options for MCQ questions, based on the targeted lesson difficulty.
- For **Essay and Short-Answer** questions, the AI **automatically generates a grading rubric** alongside the question. During exam review, the teacher can add **additional grading instructions/constraints**. The **final rubric used for grading = AI-generated rubric + teacher instructions** (never one or the other alone).
- **Hallucination guardrail (`DATA_UNAVAILABLE`):** when the AI determines available lesson content is insufficient to generate a valid question, it returns `DATA_UNAVAILABLE` instead of inventing content. Exam generation continues with whatever material is available — the generated exam **may contain fewer questions than requested**, and the teacher is notified that either more content is needed or missing questions should be created manually.
- Exams support: randomized question order, randomized option order, a configurable time limit, and **question pools** (for anti-cheating variety).
- An exam must be explicitly **published** by the teacher before students can see or attempt it (Draft → Published transition). Publishing is blocked if the exam has zero questions, or if any Essay/Short-Answer question is missing a rubric.

**Edge cases:**
- A question deleted from the bank after being used on a published exam should not break that exam's historical record — deletion is a **deactivation** (`IsActive = false`), not a hard delete.
- Reordering/repointing exam questions never changes historical `ExamAttempt.MaxScore` snapshots for attempts already in progress or completed.

**Validation rules:**
- Exactly one `QuestionOption` must be marked `IsCorrect = true` for MCQ and True/False question types (enforced at the Application layer — not easily expressible as a single-column SQL CHECK constraint).
- `ExamQuestion.Points` must be `> 0`.

**Permission rules:**
- Only the owning Teacher can create/edit/publish exams and questions in their own bank.
- Students can only see **Published** exams for classrooms they're enrolled in — never Draft or Archived.

---

## 6. Exam Attempts & Anti-Cheating

**Description:** The student-facing exam-taking experience and academic integrity enforcement.

**User roles:** Student (attempts), Teacher (reviews flags).

**Business rules:**
- **[Assumption — flagged]** One attempt per student per exam. No retake policy was specified by the team; if retakes are required later, this is a schema + API change (see `DATABASE.md` §8, `API_CONTRACT.md` §5.14).
- Anti-cheating mechanisms differ by platform:
  - **Web:** browser tab-switching detection, copy/paste restriction, general browser behavior monitoring.
  - **Mobile (Flutter):** detecting the app being minimized/backgrounded during an active exam, and monitoring application focus changes.
- **No camera, microphone, or AI-based visual proctoring** is included — explicitly ruled out.
- **Violation escalation is fixed and platform-wide (not configurable per teacher or per exam):**
  1. First detected suspicious behavior → the student receives an immediate **warning**.
  2. A second violation after the warning → the exam attempt is immediately **flagged** as suspicious for teacher review.
- **The system never automatically fails the student or terminates the exam attempt** — a flag only surfaces the attempt for teacher review; the teacher makes the final call.

**Edge cases:**
- A flagged attempt still completes grading normally — the flag is informational for the teacher, not a blocking state.

**Validation rules:**
- `AntiCheatingEvent.SequenceNumber` must increment per attempt; the 2nd (and any subsequent) event triggers the flag, regardless of event type mix.

**Permission rules:**
- Only the student taking the exam can submit answers/events for their own attempt.
- Only the owning Teacher can view flagged attempts and anti-cheating event history.

---

## 7. Grading

**Description:** How student answers are scored, released, and potentially corrected.

**User roles:** Student (recipient), Teacher (reviewer/overrider), AI Grader Agent (initial scorer).

**Exam/grading rules:**
- **Objective questions** (MCQ, True/False, Fill in the Blank) are graded **deterministically** — no AI judgment involved.
- **Subjective questions** (Essay, Short Answer) are graded by **Agent 2 (Grader)** against the final rubric (AI rubric + teacher instructions, per §5).
- **AI-generated grades are released immediately** after grading completes — for **all** question types, objective and subjective alike. **There is no teacher-approval gate before a student sees their grade.**
- Teachers **can review and override any AI-assigned score at any time**, with no time limit on when an override can happen.
- **When a teacher overrides a grade, the updated score is immediately reflected everywhere** (student view, parent report data, classroom analytics).
- **Every override is permanently logged** with: original AI score, updated score, the overriding teacher's ID, a timestamp, and an optional reason. This history is **never destructively overwritten** — a second override creates a second log entry, not a replacement of the first.
- **Confidence scores** are shown to the teacher for every AI evaluation, to help them decide whether to review/override.

**Edge cases:**
- If grading is still `Processing` when a client requests results, the API returns a `409` rather than a partial/incomplete result.
- Overriding a score beyond the question's maximum point value is rejected (`422`).

**Validation rules:**
- `FinalScore` for any answer can never exceed the question's assigned `Points` in `ExamQuestion`.

**Permission rules:**
- Only the owning Teacher can override scores for their own classroom's exams.
- A Student can see their own graded results but cannot modify any grading data.

**Audit rules:**
- Every `GradeOverride` is an append-only row — this is the platform's grading-specific audit trail, separate from the general-purpose `AuditLog` (see §11).

---

## 8. Reports & Parent Communication

**Description:** AI-generated performance analysis and how it reaches parents.

**User roles:** Teacher (views full detail), Student (views own), Parent (email-only recipient, no account).

**Reporting rules:**
- **Agent 3 (Report Generator)** aggregates student performance data, identifies weak learning areas/trends, and generates structured summaries — both teacher-facing detail and parent-facing summaries.
- **Parents have no accounts and no login.** Each Student profile stores exactly **one** parent/guardian email address.
- Parent communication is **fully automated and event-driven**: after **every completed exam**, the platform automatically emails the registered guardian address with the student's results, performance summary, and progress information. **No weekly/monthly digest exists.**
- **⚠️ Resolved conflict, documented for traceability:** the original project pitch described a teacher-approval gate (`ParentReport.IsApproved`, defaulting to `false`, requiring the teacher to click "Approve" before a report becomes visible to parents). The later, more specific clarification round established that parent emails are sent **automatically and immediately**, with **no linking or approval workflow** for parents at all. **The later ruling is treated as final** — there is currently **no approval gate** before a parent report email sends. If the original approval-gate behavior is actually still desired (as a separate concern from the grade-release timing in §7), this needs to be explicitly re-confirmed with the team, since the two statements genuinely conflict and were never reconciled by the team itself.
- All emails sent to parents are logged (`ParentReportLog`) for audit/traceability purposes, one row per completed `ExamAttempt`.

**Edge cases:**
- A report generation failure should not silently drop the parent email — it should be retried or surfaced for teacher visibility (specific retry policy not yet defined).

**Validation rules:**
- `ParentReportLog.RecipientEmail` is a snapshot of the guardian email at send-time (protects the audit record even if the guardian email is later changed).

**Permission rules:**
- Only the owning Teacher (and the student themself) can view full report detail via the API.
- Parents access reports **only** via the emailed content — there is no parent-facing web/app view.

---

## 9. Notifications

**Description:** In-app/push/email alerts for platform events (distinct from the parent-report emails in §8).

**User roles:** Teacher, Student (recipients); system-generated.

**Notification rules:**
- Channels: **in-app, mobile push, email** (no SMS).
- Triggering events include (not exhaustive, but all explicitly discussed): new learning material uploaded, video uploaded, AI exam generation completed, exam scheduled, upcoming exam reminders, exam results published, classroom announcements, new chat messages, and other platform events.
- Parent/guardian emails (§8) are a **separate mechanism** from this general notification system — different trigger logic, different recipient (non-user email vs. authenticated user), different table (`ParentReportLog` vs. `Notification`).

**Edge cases:**
- A user should be able to mark notifications as read individually; there's no discussed requirement for "mark all as read," so this is not assumed.

**Permission rules:**
- A user can only see/mark their own notifications.

---

## 10. Classroom Chat

**Description:** Communication channel between a teacher and their enrolled students.

**User roles:** Teacher, Student (both enrolled in the same classroom).

**Business rules:**
- Each classroom has **one dedicated chat channel** connecting the teacher with all currently enrolled students.
- The chat supports general classroom discussion, student questions, teacher responses, and announcements.
- The chat is **human-to-human** — **not** AI-mediated in any way.
- The chat is **scoped per classroom**, not tied to a specific lesson or exam.

**Permission rules:**
- Only the classroom's teacher and its currently-enrolled students can post/read messages.
- A student removed from a classroom loses access to that classroom's chat.

---

## 11. Subscriptions & Usage Quotas

**Description:** How teachers pay for platform access (distinct from students paying for classroom access, §3).

**User roles:** Teacher (subscriber), SuperAdmin (plan configuration).

**Business rules:**
- Draya uses a **subscription-based SaaS model** for teachers.
- **Subscription plans are fully admin-configurable** — there are **no hard-coded tier names or numeric limits** in the application. A SuperAdmin can create/modify plans (max students, max storage, monthly AI-generated exam quota) without any code or schema change.
- When a teacher reaches a plan limit, they are **notified** and may **upgrade**.
- **Usage quotas are enforced throughout the platform** — e.g., exam generation is blocked once the monthly quota is hit, not just warned about.

**Validation rules:**
- A `SubscriptionPlan`'s `MaxStudents`, `MaxStorageMB`, and `MonthlyExamQuota` must all be `> 0`.

**Permission rules:**
- Only SuperAdmin can create/edit `SubscriptionPlan` rows.
- A Teacher can view their own current plan and usage, not other teachers'.

---

## 12. Audit Logging

**Description:** System-wide traceability for sensitive/administrative actions.

**Audit rules:**
- **Grading overrides** have their own dedicated, append-only audit trail (`GradeOverride` — see §7) because this is a domain-specific, high-frequency concern.
- **All other sensitive/administrative actions** (subscription plan changes, account deactivation, material deletion, etc.) are captured in a **general-purpose `AuditLog`** table, recording entity type, entity ID, action, the performing user (nullable for system-initiated actions), and before/after JSON snapshots.
- **Payment webhook calls are separately logged** in `PaymentWebhookLog`, independent of both audit trails above, because payment disputes require proof of exactly what a third-party gateway sent — including calls that were rejected or ignored as duplicates.

---

## 13. Data Retention & Compliance

**Description:** Scope boundary explicitly agreed for this graduation project.

**Business rules:**
- **Formal legal compliance policies and data retention regulations are explicitly out of scope** for this project. This was a deliberate scoping decision, not an oversight.
- The project follows **standard security practices only**: HTTPS everywhere, JWT authentication, Role-Based Access Control, and PII anonymization before any data reaches a third-party AI service.
- **No consent workflow or formal Data Processing Agreement is required** for this project's scope — anonymous GUID-based substitution (never sending real names/emails to Claude) is considered sufficient.
