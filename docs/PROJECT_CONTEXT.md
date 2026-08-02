# PROJECT_CONTEXT.md

> **Audience:** This document (and the rest of `/docs`) is the **complete, standalone context package** for the Draya project. It is written so an AI assistant with **zero prior conversation history** can pick up development immediately. No information from outside this `/docs` folder should be assumed.

---

## 1. Project Name

**Draya** (also referred to as "Draya Team" project, ITI graduation project, Team 5).

## 2. Purpose

Draya is an AI-powered web and mobile learning platform for the EdTech / AI-in-Education space, built specifically for **tutoring centers and private educational academies** in Egypt. It automates exam creation, grading, and academic performance analysis using AI, replacing manual teacher workflows and passive student studying with AI-generated insights.

## 3. Business Goals

- Save teachers time on exam creation, student tracking, and performance analysis.
- Give students AI-generated summaries, performance insights, and topic-level weakness identification instead of passive studying.
- Give parents visibility into their child's academic progress without requiring them to use the platform directly.
- Differentiate from traditional LMS platforms (e.g., Moodle) which only provide content upload + online exams with no AI-driven analysis.
- Generate recurring SaaS subscription revenue from teachers, plus a commission on paid classroom enrollments (see Section 12 and `DECISIONS.md`).

## 4. Target Users

| Role | Description |
|---|---|
| **Teacher** (Primary) | Works in a tutoring center or private academy. Self-registers, creates classrooms, uploads lesson materials, generates/authors exams, grades and overrides scores, views analytics. |
| **Student** (Secondary) | Self-registers, joins classrooms via enrollment code (free or paid), takes exams, views results and AI summaries, uses classroom chat. |
| **Parent/Guardian** (Secondary) | Has **no platform account**. Receives automated email reports after each exam via a stored guardian email address on the student's profile. |
| **Platform Super Admin** | Anthropic-analogous "platform team" role — manages teacher accounts, subscription plans, platform-wide monitoring, abuse handling, technical support, payout processing. Does **not** manage individual classrooms or student data. |

There is **no Academy Admin role** — this was explicitly considered and rejected. Teachers operate independently; there is no organizational layer between a Teacher and the Platform Super Admin.

## 5. Main Features

1. **AI-generated exams** — teachers select source material, difficulty, and question types; an AI agent generates curriculum-aware questions (and rubrics for subjective types) for teacher review before publishing.
2. **Manual + AI-assisted question authoring** — teachers can fully author/edit/delete questions themselves; AI is an assistant, not a requirement.
3. **Automated grading** — objective questions graded deterministically; subjective (Essay/Short Answer) questions graded by AI against a rubric; teachers can override any score with a fully audited history.
4. **AI-generated performance reports** — topic-level weakness analysis for students, automatically emailed to parents after every completed exam.
5. **Classroom-based structure** — one teacher per classroom, one subject per classroom, many students per classroom (many-to-many overall via enrollment).
6. **Classroom chat** — human-to-human, teacher-to-students, scoped per classroom.
7. **Anti-cheating** — browser/app-behavior monitoring (not camera proctoring), with a fixed warn-then-flag policy.
8. **Notifications** — in-app, push, and email across a defined set of platform events.
9. **Subscription plans** — admin-configurable limits (students, storage, monthly AI exam quota) per teacher.
10. **Paid classroom enrollment** — teachers can charge students to join a classroom, processed via **Paymob**, with Draya as merchant of record taking a platform commission (see Section 12).
11. **Content ingestion & RAG** — PDF/DOCX/PPTX lesson materials are parsed, chunked, and embedded for AI-powered exam generation; videos/images are supported as learning resources but excluded from the RAG/AI pipeline.

## 6. Current Development Stage

**Design/specification stage — no code has been written yet.** Completed so far:
- Business requirements gathering (two full clarification rounds with the team, all preserved in `BUSINESS_RULES.md`)
- Complete relational database design (ERD), now including the payments module (`DATABASE.md`)
- Complete API contract (endpoints, DTOs, auth) using OpenAPI 3.0, now including the payments module (`API_CONTRACT.md`)
- **Architectural decision made:** the backend will be implemented using **Clean Architecture** (`ARCHITECTURE.md`, `CLEAN_ARCHITECTURE_GUIDE.md`)

Not yet started: solution scaffolding, any actual implementation code, testing, deployment.

## 7. High-Level System Overview

```
┌─────────────────┐        ┌─────────────────┐
│  Angular (Web)   │        │  Flutter (Mobile)│
└────────┬─────────┘        └────────┬─────────┘
         │                            │
         └──────────────┬─────────────┘
                         │  HTTPS / JWT
                ┌────────▼─────────┐
                │ ASP.NET Core     │
                │ Web API          │
                │ (Clean Arch.)    │
                └────────┬─────────┘
        ┌────────────────┼─────────────────────┐
        │                │                      │
┌───────▼──────┐  ┌───────▼───────┐   ┌─────────▼────────┐
│ Azure SQL     │  │ Qdrant Cloud  │   │ Anthropic API     │
│ (MSSQL)       │  │ (Vector DB)   │   │ Claude Sonnet 4.6  │
│ Relational data│  │ RAG chunks    │   │ via Semantic Kernel│
└───────────────┘  └───────────────┘   └────────────────────┘
        │
┌───────▼──────────┐   ┌──────────────┐   ┌────────────────┐
│ Langfuse          │   │ Paymob        │   │ Email/Push      │
│ (AI observability)│   │ (Payments)    │   │ (Notifications) │
└───────────────────┘   └──────────────┘   └─────────────────┘
```

Hosted on **Azure App Service**, containerized via **Docker**, deployed via **GitHub Actions → Azure Container Registry → Azure App Service**.

## 8. Key Architectural Decisions

| Decision | Value |
|---|---|
| Backend architecture | **Clean Architecture** (final, approved — see `ARCHITECTURE.md`) |
| Backend framework | ASP.NET Core Web API |
| Frontend (web) | Angular |
| Frontend (mobile) | Flutter |
| Relational database | MSSQL Server (Azure SQL in production) |
| ORM | Entity Framework Core (assumption — see `DECISIONS.md`) |
| AI orchestration | Microsoft Semantic Kernel |
| LLM | Claude Sonnet 4.6, via the Anthropic API |
| Vector database (RAG) | Qdrant Cloud (Free Tier), via official C# SDK |
| Embedding model | Nova Multimodal Embeddings |
| Reranking | Cohere Rerank 3.5 |
| AI observability | Langfuse SDK |
| Payment gateway | Paymob |
| Multi-tenancy model | Single database, tenant isolation enforced at the application/query layer by `TeacherId` (no Academy Admin layer) |
| Authentication | JWT via ASP.NET Core Identity |
| Authorization | Role field on `AppUser` (`Teacher`\|`Student`\|`SuperAdmin`) — no dynamic permission engine (3 fixed roles judged not to need one) |

## 9. Authentication and Authorization Overview

- **JWT Bearer tokens.** Access token TTL 60 minutes, refresh token TTL 14 days (rotated on use).
- **Self-registration** for both Teachers and Students — no invite-only or admin-provisioned account creation.
- **No parent accounts exist at all** — parents are reached only via a stored email address, never authenticate, and have no RBAC permissions.
- **Role-based access control** with exactly 3 roles: `Teacher`, `Student`, `SuperAdmin`. A Teacher can only access their own classrooms/students/materials/exams (enforced by ownership checks, not by a separate permission table). A Student can only access classrooms they're enrolled in and cannot see draft/unpublished exams.
- Full detail: `BUSINESS_RULES.md` (permission rules per feature) and `API_CONTRACT.md` §1 (auth conventions).

## 10. File Handling Overview

- Supported lesson material formats: **PDF, DOCX, PPTX** (parsed for RAG), plus **Video and Image** (stored/streamed but **not** processed by RAG or used for question generation).
- **Lesson versioning:** every re-upload/edit of a `LearningMaterial` creates a new `MaterialVersion`. Exams remain permanently linked to the exact version they were generated from — editing a lesson never retroactively changes past exams.
- File size limits are governed by the teacher's `SubscriptionPlan.MaxStorageMB`.
- Videos use an integrated in-platform player for streaming (not just a download link).

## 11. Notification Overview

Channels: **in-app, mobile push, and email** (no SMS). Full event list is in `BUSINESS_RULES.md` §Notifications. Parent/guardian notifications are a **separate, event-driven mechanism** (not the same table/flow as in-app notifications) — one automatic email per completed exam attempt, no digesting, no approval gate.

## 12. AI Integrations Overview

Three AI agents, all powered by **Claude Sonnet 4.6** via the Anthropic API, orchestrated through **Microsoft Semantic Kernel**:

1. **Agent 1 — Exam Builder**: retrieves lesson content via RAG, generates curriculum-aware questions + distractors (MCQ) + rubrics (Essay/Short Answer), for teacher review before publishing.
2. **Agent 2 — Grader**: evaluates student responses against marking criteria / rubrics, produces scores and confidence values.
3. **Agent 3 — Report Generator**: aggregates performance data, identifies weak topics, generates parent/teacher-facing summaries.

**Hallucination guardrails:** temperature locked to `0.0` for grading and exam generation; a strict system-prompt guardrail instructs the model to answer only from provided context and return `DATA_UNAVAILABLE` rather than invent content — surfaced to teachers as a partial-generation warning (see `BUSINESS_RULES.md`).

**PII protection:** a middleware pipeline anonymizes student names/emails into internal GUIDs before any data reaches Claude, and re-maps responses back to real profiles inside the secure database — real PII never reaches the third-party AI endpoint.

## 13. Multi-Tenancy Overview

Draya is multi-tenant at the **Teacher** level: each teacher's classrooms, students (via enrollment), materials, exams, and question bank are logically isolated. There is no intermediate "Academy" tenant — this was explicitly asked about and explicitly rejected during requirements gathering. Isolation is enforced by the Application layer (ownership checks against `TeacherId`), not by separate databases or schemas per tenant.

## 14. Payments Overview

Teachers may charge students to join a classroom. **Paymob** is the payment gateway (confirmed decision). **Draya is the merchant of record** with a configurable platform commission — this is a documented **Assumption** (not explicitly confirmed by the team) because the alternative (teachers connecting their own merchant accounts) was never ruled out. Payment is **one-time per classroom enrollment**, not a recurring subscription — also an **Assumption**. See `DECISIONS.md` for full rationale and `BUSINESS_RULES.md` for the payment/enrollment rules.

The core rule governing this entire module: **the client (web/mobile) is never trusted to report payment success.** Only a signature-verified, server-to-server webhook from Paymob is permitted to create a paid classroom `Enrollment`.

## 15. The Most Important Constraints Another AI Must Know

- **Do not reintroduce an Academy Admin role or per-academy tenancy** — explicitly rejected.
- **Do not add parent accounts, parent login, or parent RBAC** — explicitly rejected; parents are email-only.
- **Do not add a teacher-approval gate before parent report emails send** — the *final* clarification round confirmed automatic, immediate, event-driven sending with no approval step. (Note: the original pitch document described an `IsApproved` approval gate for parent reports — this was **superseded** by the later, more specific clarification. See `DECISIONS.md` for the explicit resolution of this conflict.)
- **Grade release is immediate for all question types**, including AI-graded Essay/Short-Answer — there is no approval gate before a student/parent sees a grade. Teacher overrides happen *after* release, not before, and are fully audited.
- **One attempt per student per exam** is the current assumption — flagged, not explicitly confirmed. If retakes become a requirement, this is a schema and API change (see `DATABASE.md` §8 and `API_CONTRACT.md` §5.14).
- **Subjects are a shared, platform-wide lookup table**, not private per teacher — flagged assumption.
- **Payment/merchant model is an assumption** (Draya as merchant of record, one-time payment) — not explicitly confirmed by the team; flagged in multiple places.
- **Refund trigger policy is not yet defined** — the `Refunded` status exists in the schema, but no business rule yet specifies what causes a refund to be issued. This must be resolved before implementing the payments module.
- **Clean Architecture is now the final, approved backend architecture** — any previous framing of the backend as a simple layered/N-tier ASP.NET Core Web API should be treated as superseded by `ARCHITECTURE.md` and `CLEAN_ARCHITECTURE_GUIDE.md`.

---

### READ THIS FIRST

If you read nothing else before touching this project, read this:

1. **Draya** is an Arabic-first (with English support) AI EdTech SaaS platform for Egyptian tutoring centers, built on **ASP.NET Core Web API using Clean Architecture**, **Angular** (web) + **Flutter** (mobile), **MSSQL** (relational data), **Qdrant** (vectors/RAG), and **Claude Sonnet 4.6** (via Semantic Kernel) for exam generation, grading, and reporting.
2. **Three roles only:** Teacher, Student, SuperAdmin. No Academy Admin. No parent accounts — parents get automated emails only.
3. **Multi-tenant at the Teacher level** — a Teacher owns Classrooms; Students enroll in Classrooms (many-to-many via `Enrollment`); Subjects are a shared lookup.
4. **Everything AI-related is guarded**: temperature 0.0, `DATA_UNAVAILABLE` fallback instead of hallucination, PII anonymized via GUID before reaching Claude, all AI grading/generation is teacher-reviewable and overridable.
5. **Grading is immediate, not gated** — but every override is permanently audit-logged (`GradeOverride`), never destructively overwritten.
6. **Payments (Paymob) are new** as of this document and layered on top of the existing `Enrollment` flow: free classrooms keep instant enrollment; paid classrooms require a checkout session, and **only a verified webhook — never the client — creates the Enrollment**.
7. **Several explicit assumptions remain open** (one-attempt exams, global Subjects, merchant-of-record payment model, refund policy) — see Section 15 above and `DECISIONS.md`. Do not silently resolve these differently without flagging it, since the human team has not signed off on them yet.
8. **The database (`DATABASE.md`) and API contract (`API_CONTRACT.md`) in this package are the latest, final versions** — they already include the payments module. Do not consult any earlier/partial version of either.
9. Full traceability of every business rule the team agreed to is preserved in `BUSINESS_RULES.md` — treat it as the source of truth for "what the system must do," and `DATABASE.md`/`API_CONTRACT.md` as the source of truth for "how it's currently modeled to do it."

---

## COMPLETE PROJECT MEMORY

Draya is a multi-tenant, Arabic-first AI EdTech SaaS platform built for Egyptian tutoring centers and private academies, aimed at automating exam creation, grading, and performance analytics for teachers while giving students AI-driven insights and giving parents passive email visibility into their child's progress — without parents ever needing an account.

The system has three roles: **Teacher** (self-registers, owns Classrooms, uploads versioned lesson materials in PDF/DOCX/PPTX/Video/Image, authors or AI-generates Exams built from a reusable per-teacher Question Bank), **Student** (self-registers, joins Classrooms — free instantly via code, or paid via a Paymob checkout flow — takes Exam Attempts, receives AI-graded results immediately, with anti-cheating behavior monitoring that warns then flags), and **SuperAdmin** (manages platform-wide concerns: teacher accounts, configurable SubscriptionPlans with per-teacher usage quotas, payout processing — never touches classroom-level data).

Three Claude-Sonnet-4.6-powered agents, orchestrated via Microsoft Semantic Kernel, do the heavy lifting: an Exam Builder (RAG-grounded, using Qdrant + Nova embeddings + Cohere reranking, chunking lesson text at 1000 chars/150-char overlap), a Grader (deterministic for objective questions, rubric-based AI grading for Essay/Short-Answer, teacher-overridable with full audit trail), and a Report Generator (topic-level weakness analysis, auto-emailed to parents after every exam, no approval gate). All AI calls are guarded against hallucination (temperature 0.0, explicit `DATA_UNAVAILABLE` fallback) and PII-anonymized via a GUID-remapping middleware before reaching the Anthropic API, with Langfuse providing observability.

The relational schema (MSSQL/Azure SQL, 31 entities) is anchored around an `AppUser`-plus-role-extension identity model, a `Classroom` as the central teaching unit (one Teacher, one Subject, many Students via `Enrollment`), versioned `LearningMaterial`/`MaterialVersion` feeding a RAG pipeline pointed at Qdrant via `MaterialChunk`, a decoupled `Question`/`ExamQuestion` bank-and-reuse model, an `ExamAttempt`/`StudentAnswer`/`GradeOverride` grading chain with full auditability, and — newly added — a payments module (`ClassroomPricing`, `PaymentTransaction`, `PaymentWebhookLog`, `TeacherPayoutStatement`) built around the non-negotiable rule that only a signature-verified Paymob webhook, never the client, can create a paid `Enrollment`.

The backend will be implemented using **Clean Architecture** (Domain / Application / Infrastructure / Presentation layers, dependencies pointing inward, framework-independent Domain and Application layers) — this is the final, approved decision and supersedes any earlier informal architecture framing. The API contract (55 endpoints across Auth, Classrooms, Materials, Exams, Grading, Reports, and Payments) is fully specified as OpenAPI 3.0 and is considered frozen for parallel frontend/mobile/backend development, pending sign-off on the explicitly flagged open assumptions (one-attempt exams, global Subjects lookup, merchant-of-record payment model, undefined refund trigger policy).
