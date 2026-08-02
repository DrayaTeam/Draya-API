# DATABASE.md

> This is the **latest, final, approved ERD** for the Draya platform (31 entities, including the Payments module). It supersedes any earlier/partial version. Written for an AI assistant continuing development with no prior conversation history — every rationale, assumption, and constraint is stated explicitly.

**Prepared as:** Senior Database Architect / Solution Architect deliverable
**Scope:** Complete relational schema (SQL Server / Azure SQL) for the Draya EdTech platform, to be accessed through EF Core repository implementations in the Clean Architecture Infrastructure layer (see `ARCHITECTURE.md`, `CLEAN_ARCHITECTURE_GUIDE.md`)
**Status:** Design finalized — no backend implementation yet

---

## 1. Database Overview

The Draya database is a **multi-tenant relational schema** (single database, tenant isolation enforced at the application/query layer by `TeacherId`) built for SQL Server / EF Core.

Architectural approach:

- **Identity is split**: a single `AppUser` table holds authentication concerns (email, password hash, role), while `Teacher`, `Student`, and `PlatformAdmin` are 1:1 extension tables holding role-specific attributes. This avoids a wide, mostly-null user table and keeps RBAC simple — a fixed 3-role system does not need a full dynamic Role/Permission engine.
- **Content is versioned**: `LearningMaterial` (the logical lesson) is separated from `MaterialVersion` (each upload/edit), so exams can stay permanently pinned to the version they were generated from — matching the agreed requirement.
- **Question Bank is decoupled from Exam**: `Question` is owned by a `Teacher` and reusable across multiple exams via the `ExamQuestion` junction table. This supports question pools, reuse, and manual authoring alongside AI generation.
- **Grading is fully auditable**: `StudentAnswer` stores the AI-produced score; any teacher change is captured immutably in `GradeOverride`, never overwritten in place.
- **RAG storage boundary is respected**: vector embeddings live in Qdrant (external), but `MaterialChunk` keeps a lightweight relational pointer (`VectorId`) for traceability between a chunk, its source version, and the vector store — without duplicating vector math inside SQL Server.
- **Cross-cutting concerns** (notifications, chat, audit, anti-cheating events, parent email log) are modeled as first-class, independently queryable entities rather than bolted onto business tables.

---

## 2. Entity List

| # | Entity | One-line description |
|---|--------|----------------------|
| 1 | AppUser | Authentication record shared by all account types |
| 2 | Teacher | Teacher-specific profile (1:1 with AppUser) |
| 3 | Student | Student-specific profile (1:1 with AppUser) |
| 4 | PlatformAdmin | Super Admin profile (1:1 with AppUser) |
| 5 | SubscriptionPlan | Admin-configurable plan limits (students, storage, exam quota) |
| 6 | TeacherSubscription | A teacher's subscription history/current plan |
| 7 | UsageCounter | Per-teacher, per-billing-period usage tracking for quota enforcement |
| 8 | Subject | Global lookup of academic subjects |
| 9 | Classroom | One teacher's class for one subject |
| 10 | Enrollment | Student's membership in a classroom |
| 11 | LearningMaterial | A logical lesson/resource owned by a classroom |
| 12 | MaterialVersion | A specific uploaded version of a LearningMaterial |
| 13 | MaterialChunk | RAG chunk metadata pointing to a Qdrant vector |
| 14 | VideoDetail | Video-specific playback metadata (extends LearningMaterial) |
| 15 | Exam | An exam definition within a classroom |
| 16 | Question | A reusable question owned by a teacher (question bank) |
| 17 | QuestionOption | An answer option for MCQ/True-False questions |
| 18 | QuestionRubric | AI-generated + teacher-augmented grading rubric for essay/short-answer questions |
| 19 | ExamQuestion | Junction: which questions belong to which exam, in what order, worth how many points |
| 20 | ExamAttempt | A student's attempt at an exam |
| 21 | StudentAnswer | A student's answer to one question within an attempt |
| 22 | GradeOverride | Immutable audit record of a teacher changing an AI-assigned score |
| 23 | AntiCheatingEvent | A detected suspicious-behavior event during an attempt |
| 24 | Notification | In-app/push/email notification sent to a user |
| 25 | ParentReportLog | Log of automated parent/guardian emails sent after exams |
| 26 | ChatMessage | A message inside a classroom's chat channel |
| 27 | AuditLog | Generic system-wide audit trail for administrative/sensitive actions |
| 28 | ClassroomPricing | Current price a teacher charges for a classroom (nullable = free) |
| 29 | PaymentTransaction | One payment attempt/record for a student joining a paid classroom |
| 30 | PaymentWebhookLog | Raw log of every inbound Paymob webhook, for replay-safety and disputes |
| 31 | TeacherPayoutStatement | Periodic payout record: what a teacher is owed after Draya's platform fee |

> **Assumption stated explicitly (payments, added this revision):** Draya acts as the **merchant of record** — a single platform-level Paymob integration, with a configurable platform commission percentage deducted before payout to teachers. Payment is **one-time per classroom enrollment**, not a recurring subscription. If either assumption is wrong (e.g., teachers should connect their own merchant accounts, or access should be time-boxed/renewable), `TeacherPayoutStatement` and `PaymentTransaction` are the two tables that would need to change — everything else in the schema is unaffected either way.

---

## 3. Entity Specifications

> Convention: all primary keys are `uniqueidentifier` (`UUID`) generated application-side (`NEWID()` default), all timestamps are `datetime2`, all monetary/score values are `decimal`. `nvarchar` is used throughout for Arabic/English text support.

### 3.1 AppUser
**Purpose:** Single authentication source of truth for every account type.

| Attribute | Type | Nullable | Default | Constraints |
|---|---|---|---|---|
| Id | uniqueidentifier | No | NEWID() | PK |
| Email | nvarchar(256) | No | — | UNIQUE |
| PasswordHash | nvarchar(512) | No | — | — |
| Role | nvarchar(20) | No | — | CHECK IN ('Teacher','Student','SuperAdmin') |
| IsActive | bit | No | 1 | — |
| CreatedAt | datetime2 | No | SYSUTCDATETIME() | — |
| LastLoginAt | datetime2 | Yes | NULL | — |

**Indexes:** UNIQUE(Email); IX_AppUser_Role

---

### 3.2 Teacher
**Purpose:** Teacher-specific profile, 1:1 extension of AppUser.

| Attribute | Type | Nullable | Default | Constraints |
|---|---|---|---|---|
| UserId | uniqueidentifier | No | — | PK, FK → AppUser.Id |
| FullName | nvarchar(200) | No | — | — |
| Phone | nvarchar(30) | Yes | NULL | — |
| CreatedAt | datetime2 | No | SYSUTCDATETIME() | — |

**Indexes:** PK(UserId)

---

### 3.3 Student
**Purpose:** Student-specific profile, 1:1 extension of AppUser.

| Attribute | Type | Nullable | Default | Constraints |
|---|---|---|---|---|
| UserId | uniqueidentifier | No | — | PK, FK → AppUser.Id |
| FullName | nvarchar(200) | No | — | — |
| ParentGuardianEmail | nvarchar(256) | No | — | one guardian email per student (agreed rule) |
| DateOfBirth | date | Yes | NULL | — |
| CreatedAt | datetime2 | No | SYSUTCDATETIME() | — |

**Indexes:** PK(UserId)

---

### 3.4 PlatformAdmin
**Purpose:** Super Admin profile, 1:1 extension of AppUser.

| Attribute | Type | Nullable | Default | Constraints |
|---|---|---|---|---|
| UserId | uniqueidentifier | No | — | PK, FK → AppUser.Id |
| FullName | nvarchar(200) | No | — | — |
| CreatedAt | datetime2 | No | SYSUTCDATETIME() | — |

---

### 3.5 SubscriptionPlan
**Purpose:** Admin-configurable plan definitions (no hard-coded tiers, per requirement).

| Attribute | Type | Nullable | Default | Constraints |
|---|---|---|---|---|
| Id | uniqueidentifier | No | NEWID() | PK |
| Name | nvarchar(100) | No | — | UNIQUE |
| MaxStudents | int | No | — | CHECK (MaxStudents > 0) |
| MaxStorageMB | int | No | — | CHECK (MaxStorageMB > 0) |
| MonthlyExamQuota | int | No | — | CHECK (MonthlyExamQuota > 0) |
| PriceMonthly | decimal(10,2) | No | 0 | CHECK (PriceMonthly >= 0) |
| IsActive | bit | No | 1 | — |
| CreatedAt | datetime2 | No | SYSUTCDATETIME() | — |

---

### 3.6 TeacherSubscription
**Purpose:** History of plan assignments per teacher; current plan = latest row with Status = 'Active'.

| Attribute | Type | Nullable | Default | Constraints |
|---|---|---|---|---|
| Id | uniqueidentifier | No | NEWID() | PK |
| TeacherId | uniqueidentifier | No | — | FK → Teacher.UserId |
| PlanId | uniqueidentifier | No | — | FK → SubscriptionPlan.Id |
| StartDate | datetime2 | No | SYSUTCDATETIME() | — |
| EndDate | datetime2 | Yes | NULL | — |
| Status | nvarchar(20) | No | 'Active' | CHECK IN ('Active','Expired','Cancelled') |
| CreatedAt | datetime2 | No | SYSUTCDATETIME() | — |

**Indexes:** IX_TeacherSubscription_TeacherId_Status (TeacherId, Status)

---

### 3.7 UsageCounter
**Purpose:** Enforces monthly quotas (exam generation, storage) without recomputing aggregates on every request.

| Attribute | Type | Nullable | Default | Constraints |
|---|---|---|---|---|
| Id | uniqueidentifier | No | NEWID() | PK |
| TeacherId | uniqueidentifier | No | — | FK → Teacher.UserId |
| PeriodMonth | date | No | — | first day of the billing month |
| ExamsGeneratedCount | int | No | 0 | CHECK (>= 0) |
| StorageUsedMB | decimal(10,2) | No | 0 | CHECK (>= 0) |

**Indexes:** UNIQUE(TeacherId, PeriodMonth)

---

### 3.8 Subject
**Purpose:** Global lookup of academic subjects (Math, Physics, Arabic, etc.).
> **Assumption stated explicitly:** Subjects are treated as a shared platform-wide taxonomy rather than teacher-private free text, so reporting/analytics can group by subject consistently. If this is wrong, `Subject` can be trivially scoped per-teacher instead.

| Attribute | Type | Nullable | Default | Constraints |
|---|---|---|---|---|
| Id | uniqueidentifier | No | NEWID() | PK |
| Name | nvarchar(100) | No | — | UNIQUE |

---

### 3.9 Classroom
**Purpose:** One teacher's class for one subject; the unit students enroll into.

| Attribute | Type | Nullable | Default | Constraints |
|---|---|---|---|---|
| Id | uniqueidentifier | No | NEWID() | PK |
| TeacherId | uniqueidentifier | No | — | FK → Teacher.UserId |
| SubjectId | uniqueidentifier | No | — | FK → Subject.Id |
| Name | nvarchar(200) | No | — | — |
| EnrollmentCode | nvarchar(20) | No | — | UNIQUE |
| IsActive | bit | No | 1 | — |
| CreatedAt | datetime2 | No | SYSUTCDATETIME() | — |

**Indexes:** UNIQUE(EnrollmentCode); IX_Classroom_TeacherId

---

### 3.10 Enrollment
**Purpose:** Junction recording a student's membership in a classroom (instant, no approval state — per agreed rule).

| Attribute | Type | Nullable | Default | Constraints |
|---|---|---|---|---|
| Id | uniqueidentifier | No | NEWID() | PK |
| StudentId | uniqueidentifier | No | — | FK → Student.UserId |
| ClassroomId | uniqueidentifier | No | — | FK → Classroom.Id |
| PaymentTransactionId | uniqueidentifier | Yes | NULL | FK → PaymentTransaction.Id — NULL for free classrooms; set only after a webhook confirms payment |
| EnrolledAt | datetime2 | No | SYSUTCDATETIME() | — |
| Status | nvarchar(20) | No | 'Active' | CHECK IN ('Active','Removed') |

**Indexes:** UNIQUE(StudentId, ClassroomId); IX_Enrollment_ClassroomId

> **Payment note:** for a paid classroom, this row is created only *after* `PaymentTransaction.Status` becomes `Paid` (webhook-driven — see 3.28–3.31). No Enrollment row is ever created speculatively before payment confirmation.

---

### 3.11 LearningMaterial
**Purpose:** Logical lesson/resource container owned by a classroom (parent of versions).

| Attribute | Type | Nullable | Default | Constraints |
|---|---|---|---|---|
| Id | uniqueidentifier | No | NEWID() | PK |
| ClassroomId | uniqueidentifier | No | — | FK → Classroom.Id |
| TeacherId | uniqueidentifier | No | — | FK → Teacher.UserId |
| Title | nvarchar(300) | No | — | — |
| MaterialType | nvarchar(20) | No | — | CHECK IN ('PDF','DOCX','PPTX','Video','Image') |
| IsDeleted | bit | No | 0 | soft delete |
| CreatedAt | datetime2 | No | SYSUTCDATETIME() | — |

**Indexes:** IX_LearningMaterial_ClassroomId

---

### 3.12 MaterialVersion
**Purpose:** Each upload/edit of a LearningMaterial; exams stay permanently pinned to the version used at generation time.

| Attribute | Type | Nullable | Default | Constraints |
|---|---|---|---|---|
| Id | uniqueidentifier | No | NEWID() | PK |
| MaterialId | uniqueidentifier | No | — | FK → LearningMaterial.Id |
| VersionNumber | int | No | — | CHECK (> 0) |
| FileUrl | nvarchar(500) | No | — | — |
| FileSizeMB | decimal(10,2) | No | — | CHECK (>= 0) |
| UploadedAt | datetime2 | No | SYSUTCDATETIME() | — |
| ParseStatus | nvarchar(20) | No | 'Pending' | CHECK IN ('Pending','Parsed','Failed') |
| IsCurrent | bit | No | 1 | only one TRUE per MaterialId (enforced app-side / filtered index) |

**Indexes:** UNIQUE(MaterialId, VersionNumber); IX_MaterialVersion_MaterialId_IsCurrent

---

### 3.13 MaterialChunk
**Purpose:** Relational pointer from a parsed text chunk to its Qdrant vector, for traceability (not for storing vectors themselves).

| Attribute | Type | Nullable | Default | Constraints |
|---|---|---|---|---|
| Id | uniqueidentifier | No | NEWID() | PK |
| MaterialVersionId | uniqueidentifier | No | — | FK → MaterialVersion.Id |
| ChunkIndex | int | No | — | CHECK (>= 0) |
| VectorId | nvarchar(100) | No | — | Qdrant point ID, UNIQUE |
| CharStart | int | No | — | — |
| CharEnd | int | No | — | CHECK (CharEnd > CharStart) |

**Indexes:** UNIQUE(VectorId); IX_MaterialChunk_MaterialVersionId

---

### 3.14 VideoDetail
**Purpose:** Extends LearningMaterial (1:1) with playback metadata; only exists when MaterialType = 'Video'.

| Attribute | Type | Nullable | Default | Constraints |
|---|---|---|---|---|
| MaterialId | uniqueidentifier | No | — | PK, FK → LearningMaterial.Id |
| DurationSeconds | int | No | — | CHECK (> 0) |
| StreamUrl | nvarchar(500) | No | — | — |
| ThumbnailUrl | nvarchar(500) | Yes | NULL | — |

---

### 3.15 Exam
**Purpose:** An exam definition within a classroom.

| Attribute | Type | Nullable | Default | Constraints |
|---|---|---|---|---|
| Id | uniqueidentifier | No | NEWID() | PK |
| ClassroomId | uniqueidentifier | No | — | FK → Classroom.Id |
| TeacherId | uniqueidentifier | No | — | FK → Teacher.UserId |
| SourceMaterialVersionId | uniqueidentifier | Yes | NULL | FK → MaterialVersion.Id (null if fully manual exam) |
| Title | nvarchar(300) | No | — | — |
| DifficultyLevel | nvarchar(20) | No | — | CHECK IN ('Easy','Medium','Hard') |
| Status | nvarchar(20) | No | 'Draft' | CHECK IN ('Draft','Published','Archived') |
| TimeLimitMinutes | int | Yes | NULL | CHECK (> 0) |
| RandomizeQuestionOrder | bit | No | 1 | — |
| RandomizeOptionOrder | bit | No | 1 | — |
| CreatedAt | datetime2 | No | SYSUTCDATETIME() | — |
| PublishedAt | datetime2 | Yes | NULL | — |

**Indexes:** IX_Exam_ClassroomId_Status

---

### 3.16 Question
**Purpose:** A reusable question owned by a teacher (question bank), AI-generated or manually authored.

| Attribute | Type | Nullable | Default | Constraints |
|---|---|---|---|---|
| Id | uniqueidentifier | No | NEWID() | PK |
| TeacherId | uniqueidentifier | No | — | FK → Teacher.UserId |
| SubjectId | uniqueidentifier | Yes | NULL | FK → Subject.Id |
| SourceMaterialVersionId | uniqueidentifier | Yes | NULL | FK → MaterialVersion.Id |
| QuestionType | nvarchar(20) | No | — | CHECK IN ('MCQ','TrueFalse','FillBlank','ShortAnswer','Essay') |
| QuestionText | nvarchar(max) | No | — | — |
| DifficultyLevel | nvarchar(20) | No | — | CHECK IN ('Easy','Medium','Hard') |
| Source | nvarchar(20) | No | — | CHECK IN ('AIGenerated','Manual','AIAssistedEdited') |
| IsActive | bit | No | 1 | — |
| CreatedAt | datetime2 | No | SYSUTCDATETIME() | — |

**Indexes:** IX_Question_TeacherId_QuestionType; IX_Question_SubjectId

---

### 3.17 QuestionOption
**Purpose:** Answer option for MCQ / True-False questions.

| Attribute | Type | Nullable | Default | Constraints |
|---|---|---|---|---|
| Id | uniqueidentifier | No | NEWID() | PK |
| QuestionId | uniqueidentifier | No | — | FK → Question.Id |
| OptionText | nvarchar(500) | No | — | — |
| IsCorrect | bit | No | 0 | — |
| OrderIndex | int | No | — | CHECK (>= 0) |

**Indexes:** IX_QuestionOption_QuestionId

---

### 3.18 QuestionRubric
**Purpose:** AI-generated rubric + teacher instructions, used to grade Essay/ShortAnswer questions (1:1 with Question).

| Attribute | Type | Nullable | Default | Constraints |
|---|---|---|---|---|
| QuestionId | uniqueidentifier | No | — | PK, FK → Question.Id |
| AIGeneratedCriteria | nvarchar(max) | No | — | JSON-structured rubric criteria |
| TeacherInstructions | nvarchar(max) | Yes | NULL | additional constraints added at review time |
| UpdatedAt | datetime2 | No | SYSUTCDATETIME() | — |

---

### 3.19 ExamQuestion
**Purpose:** Junction: which questions compose an exam, their order and point value. Enables question reuse across exams.

| Attribute | Type | Nullable | Default | Constraints |
|---|---|---|---|---|
| Id | uniqueidentifier | No | NEWID() | PK |
| ExamId | uniqueidentifier | No | — | FK → Exam.Id |
| QuestionId | uniqueidentifier | No | — | FK → Question.Id |
| OrderIndex | int | No | — | CHECK (>= 0) |
| Points | decimal(5,2) | No | — | CHECK (> 0) |

**Indexes:** UNIQUE(ExamId, QuestionId); IX_ExamQuestion_QuestionId

---

### 3.20 ExamAttempt
**Purpose:** A student's attempt at an exam.
> **Assumption stated explicitly:** one attempt per student per exam (no retake policy was specified). `AttemptNumber` can be introduced later without breaking the schema if retakes become a requirement.

| Attribute | Type | Nullable | Default | Constraints |
|---|---|---|---|---|
| Id | uniqueidentifier | No | NEWID() | PK |
| ExamId | uniqueidentifier | No | — | FK → Exam.Id |
| StudentId | uniqueidentifier | No | — | FK → Student.UserId |
| StartedAt | datetime2 | No | SYSUTCDATETIME() | — |
| SubmittedAt | datetime2 | Yes | NULL | — |
| Status | nvarchar(20) | No | 'InProgress' | CHECK IN ('InProgress','Submitted','Flagged','Expired') |
| TotalScore | decimal(6,2) | Yes | NULL | populated after grading |
| MaxScore | decimal(6,2) | No | — | CHECK (> 0) |

**Indexes:** UNIQUE(ExamId, StudentId); IX_ExamAttempt_StudentId

---

### 3.21 StudentAnswer
**Purpose:** A student's answer to one question within an attempt, plus the AI-assigned score.

| Attribute | Type | Nullable | Default | Constraints |
|---|---|---|---|---|
| Id | uniqueidentifier | No | NEWID() | PK |
| AttemptId | uniqueidentifier | No | — | FK → ExamAttempt.Id |
| QuestionId | uniqueidentifier | No | — | FK → Question.Id |
| SelectedOptionId | uniqueidentifier | Yes | NULL | FK → QuestionOption.Id (MCQ/TrueFalse only) |
| AnswerText | nvarchar(max) | Yes | NULL | FillBlank/ShortAnswer/Essay |
| AIScore | decimal(5,2) | Yes | NULL | CHECK (>= 0) |
| FinalScore | decimal(5,2) | Yes | NULL | CHECK (>= 0); equals AIScore unless overridden |
| ConfidenceScore | decimal(4,3) | Yes | NULL | CHECK (BETWEEN 0 AND 1) |
| GradedAt | datetime2 | Yes | NULL | — |

**Indexes:** UNIQUE(AttemptId, QuestionId); IX_StudentAnswer_QuestionId

---

### 3.22 GradeOverride
**Purpose:** Immutable log of a teacher changing an AI-assigned score. `StudentAnswer.FinalScore` is updated, but the change is never destructive — full history preserved here.

| Attribute | Type | Nullable | Default | Constraints |
|---|---|---|---|---|
| Id | uniqueidentifier | No | NEWID() | PK |
| StudentAnswerId | uniqueidentifier | No | — | FK → StudentAnswer.Id |
| TeacherId | uniqueidentifier | No | — | FK → Teacher.UserId |
| OriginalScore | decimal(5,2) | No | — | — |
| NewScore | decimal(5,2) | No | — | — |
| Reason | nvarchar(500) | Yes | NULL | — |
| OverriddenAt | datetime2 | No | SYSUTCDATETIME() | — |

**Indexes:** IX_GradeOverride_StudentAnswerId

---

### 3.23 AntiCheatingEvent
**Purpose:** A detected suspicious-behavior event during an attempt; 1st = warning, 2nd = flag (platform-wide, fixed rule).

| Attribute | Type | Nullable | Default | Constraints |
|---|---|---|---|---|
| Id | uniqueidentifier | No | NEWID() | PK |
| AttemptId | uniqueidentifier | No | — | FK → ExamAttempt.Id |
| EventType | nvarchar(30) | No | — | CHECK IN ('TabSwitch','CopyPaste','AppMinimized','FocusLoss') |
| OccurredAt | datetime2 | No | SYSUTCDATETIME() | — |
| SequenceNumber | int | No | — | CHECK (> 0); 1 = warning, 2+ = flag trigger |

**Indexes:** IX_AntiCheatingEvent_AttemptId

---

### 3.24 Notification
**Purpose:** In-app / push / email notification delivered to a user.

| Attribute | Type | Nullable | Default | Constraints |
|---|---|---|---|---|
| Id | uniqueidentifier | No | NEWID() | PK |
| UserId | uniqueidentifier | No | — | FK → AppUser.Id |
| Type | nvarchar(50) | No | — | e.g. 'ExamGenerated','MaterialUploaded','ResultsPublished' |
| Title | nvarchar(200) | No | — | — |
| Message | nvarchar(1000) | No | — | — |
| Channel | nvarchar(20) | No | — | CHECK IN ('InApp','Push','Email') |
| IsRead | bit | No | 0 | — |
| CreatedAt | datetime2 | No | SYSUTCDATETIME() | — |

**Indexes:** IX_Notification_UserId_IsRead

---

### 3.25 ParentReportLog
**Purpose:** Log of automated parent/guardian emails sent after each completed exam (event-driven, per agreed rule).

| Attribute | Type | Nullable | Default | Constraints |
|---|---|---|---|---|
| Id | uniqueidentifier | No | NEWID() | PK |
| StudentId | uniqueidentifier | No | — | FK → Student.UserId |
| ExamAttemptId | uniqueidentifier | No | — | FK → ExamAttempt.Id |
| RecipientEmail | nvarchar(256) | No | — | snapshot at send-time |
| SentAt | datetime2 | No | SYSUTCDATETIME() | — |
| Status | nvarchar(20) | No | 'Sent' | CHECK IN ('Sent','Failed') |

**Indexes:** UNIQUE(ExamAttemptId); IX_ParentReportLog_StudentId

---

### 3.26 ChatMessage
**Purpose:** A message inside a classroom's chat channel (human-to-human, not AI-mediated).

| Attribute | Type | Nullable | Default | Constraints |
|---|---|---|---|---|
| Id | uniqueidentifier | No | NEWID() | PK |
| ClassroomId | uniqueidentifier | No | — | FK → Classroom.Id |
| SenderUserId | uniqueidentifier | No | — | FK → AppUser.Id |
| MessageText | nvarchar(max) | No | — | — |
| IsAnnouncement | bit | No | 0 | — |
| SentAt | datetime2 | No | SYSUTCDATETIME() | — |

**Indexes:** IX_ChatMessage_ClassroomId_SentAt

---

### 3.27 AuditLog
**Purpose:** Generic, system-wide audit trail for sensitive/administrative actions beyond grading (e.g., plan changes, account deactivation, material deletion).

| Attribute | Type | Nullable | Default | Constraints |
|---|---|---|---|---|
| Id | uniqueidentifier | No | NEWID() | PK |
| EntityType | nvarchar(100) | No | — | e.g. 'SubscriptionPlan', 'Classroom' |
| EntityId | uniqueidentifier | No | — | — |
| Action | nvarchar(50) | No | — | e.g. 'Create','Update','Delete' |
| PerformedByUserId | uniqueidentifier | Yes | NULL | FK → AppUser.Id (null = system) |
| OldValue | nvarchar(max) | Yes | NULL | JSON snapshot |
| NewValue | nvarchar(max) | Yes | NULL | JSON snapshot |
| Timestamp | datetime2 | No | SYSUTCDATETIME() | — |

**Indexes:** IX_AuditLog_EntityType_EntityId; IX_AuditLog_Timestamp

---

### 3.28 ClassroomPricing
**Purpose:** Current price a teacher charges to join a classroom. Kept as its own table (not a column on Classroom) so price history isn't lost when a teacher changes it — past `PaymentTransaction` rows keep the price actually paid regardless of later changes here.

| Attribute | Type | Nullable | Default | Constraints |
|---|---|---|---|---|
| ClassroomId | uniqueidentifier | No | — | PK, FK → Classroom.Id (1:1) |
| Price | decimal(10,2) | No | 0 | CHECK (Price >= 0) |
| Currency | nvarchar(3) | No | 'EGP' | CHECK IN ('EGP') for now — single-currency by design |
| IsFree | bit | No | 1 | computed/maintained app-side as `Price = 0` |
| UpdatedAt | datetime2 | No | SYSUTCDATETIME() | — |

---

### 3.29 PaymentTransaction
**Purpose:** One payment attempt for a student joining a specific paid classroom. Created in `Pending` state the moment checkout is initiated; only a verified Paymob webhook moves it to `Paid`.

| Attribute | Type | Nullable | Default | Constraints |
|---|---|---|---|---|
| Id | uniqueidentifier | No | NEWID() | PK |
| StudentId | uniqueidentifier | No | — | FK → Student.UserId |
| ClassroomId | uniqueidentifier | No | — | FK → Classroom.Id |
| GatewayProvider | nvarchar(20) | No | 'Paymob' | CHECK IN ('Paymob') — kept as an enum-like field so a second gateway can be added later without a schema change |
| GatewayTransactionId | nvarchar(100) | Yes | NULL | UNIQUE (nullable until Paymob assigns one at intent creation); used for webhook idempotency lookups |
| Amount | decimal(10,2) | No | — | CHECK (Amount >= 0); snapshot of `ClassroomPricing.Price` at checkout time |
| Currency | nvarchar(3) | No | 'EGP' | — |
| Status | nvarchar(20) | No | 'Pending' | CHECK IN ('Pending','Paid','Failed','Refunded') |
| CreatedAt | datetime2 | No | SYSUTCDATETIME() | — |
| PaidAt | datetime2 | Yes | NULL | set only by the webhook handler |

**Indexes:** UNIQUE(GatewayTransactionId) WHERE NOT NULL; IX_PaymentTransaction_StudentId_Status; IX_PaymentTransaction_ClassroomId

---

### 3.30 PaymentWebhookLog
**Purpose:** Immutable log of every inbound Paymob webhook call — signed payload, verification result, and timestamp. Exists independently of `PaymentTransaction` because gateways retry webhooks, and you need to prove (for disputes/support) exactly what was received and when, even for calls that were rejected or ignored as duplicates.

| Attribute | Type | Nullable | Default | Constraints |
|---|---|---|---|---|
| Id | uniqueidentifier | No | NEWID() | PK |
| GatewayTransactionId | nvarchar(100) | Yes | NULL | not FK-constrained — logged even if it doesn't match a known transaction |
| RawPayload | nvarchar(max) | No | — | full JSON body as received |
| SignatureValid | bit | No | — | result of HMAC verification |
| ProcessingResult | nvarchar(30) | No | — | CHECK IN ('Processed','DuplicateIgnored','SignatureInvalid','TransactionNotFound') |
| ReceivedAt | datetime2 | No | SYSUTCDATETIME() | — |

**Indexes:** IX_PaymentWebhookLog_GatewayTransactionId; IX_PaymentWebhookLog_ReceivedAt

---

### 3.31 TeacherPayoutStatement
**Purpose:** Periodic (e.g., monthly) record of what a teacher is owed after Draya's platform commission is deducted, since Draya is the merchant of record and collects payments on the teacher's behalf.

| Attribute | Type | Nullable | Default | Constraints |
|---|---|---|---|---|
| Id | uniqueidentifier | No | NEWID() | PK |
| TeacherId | uniqueidentifier | No | — | FK → Teacher.UserId |
| PeriodStart | date | No | — | — |
| PeriodEnd | date | No | — | CHECK (PeriodEnd > PeriodStart) |
| GrossAmount | decimal(12,2) | No | — | sum of `PaymentTransaction.Amount` for this teacher's classrooms in the period, Status = Paid |
| PlatformFeePercent | decimal(5,2) | No | — | commission rate applied, snapshotted for historical accuracy even if the platform rate later changes |
| PlatformFeeAmount | decimal(12,2) | No | — | GrossAmount × PlatformFeePercent |
| NetAmount | decimal(12,2) | No | — | GrossAmount − PlatformFeeAmount |
| Status | nvarchar(20) | No | 'Pending' | CHECK IN ('Pending','Paid','Failed') |
| PaidAt | datetime2 | Yes | NULL | when Draya actually transferred funds to the teacher |

**Indexes:** UNIQUE(TeacherId, PeriodStart, PeriodEnd); IX_TeacherPayoutStatement_Status

---

## 4. Relationship Matrix

| Relationship | Cardinality | Reason |
|---|---|---|
| AppUser (1) — (1) Teacher | 1:1 | role-specific extension |
| AppUser (1) — (1) Student | 1:1 | role-specific extension |
| AppUser (1) — (1) PlatformAdmin | 1:1 | role-specific extension |
| SubscriptionPlan (1) — (N) TeacherSubscription | 1:N | one plan is used by many teachers over time |
| Teacher (1) — (N) TeacherSubscription | 1:N | subscription history per teacher |
| Teacher (1) — (N) UsageCounter | 1:N | one usage row per billing month |
| Teacher (1) — (N) Classroom | 1:N | a teacher owns many classrooms |
| Subject (1) — (N) Classroom | 1:N | a subject is taught in many classrooms |
| Student (N) — (N) Classroom via Enrollment | N:N | students join multiple classrooms; classrooms hold multiple students |
| Classroom (1) — (N) LearningMaterial | 1:N | lessons belong to one classroom |
| LearningMaterial (1) — (N) MaterialVersion | 1:N | every edit/upload creates a version |
| MaterialVersion (1) — (N) MaterialChunk | 1:N | a version is split into many RAG chunks |
| LearningMaterial (1) — (1) VideoDetail | 1:1 | only applies when MaterialType = Video |
| Classroom (1) — (N) Exam | 1:N | exams belong to one classroom |
| MaterialVersion (1) — (N) Exam | 1:N | one lesson version can source multiple exams |
| Teacher (1) — (N) Question | 1:N | question bank owned per teacher |
| Question (1) — (N) QuestionOption | 1:N | MCQ/TrueFalse options |
| Question (1) — (1) QuestionRubric | 1:1 | only for Essay/ShortAnswer |
| Exam (N) — (N) Question via ExamQuestion | N:N | a question can appear on multiple exams; an exam has many questions |
| Exam (1) — (N) ExamAttempt | 1:N | many students attempt one exam |
| Student (1) — (N) ExamAttempt | 1:N | a student has attempts across many exams |
| ExamAttempt (1) — (N) StudentAnswer | 1:N | one answer row per question in the attempt |
| Question (1) — (N) StudentAnswer | 1:N | a question is answered across many attempts |
| StudentAnswer (1) — (N) GradeOverride | 1:N | full override history retained (usually 0 or 1, modeled as N for auditability) |
| Teacher (1) — (N) GradeOverride | 1:N | a teacher can override many scores |
| ExamAttempt (1) — (N) AntiCheatingEvent | 1:N | multiple violation events per attempt |
| AppUser (1) — (N) Notification | 1:N | a user receives many notifications |
| Student (1) — (N) ParentReportLog | 1:N | one log row per completed exam |
| ExamAttempt (1) — (1) ParentReportLog | 1:1 | one report email per attempt |
| Classroom (1) — (N) ChatMessage | 1:N | classroom-scoped chat |
| AppUser (1) — (N) ChatMessage | 1:N | a user (teacher or student) sends many messages |
| AppUser (1) — (N) AuditLog | 1:N | actions performed by a user (nullable for system actions) |
| Classroom (1) — (1) ClassroomPricing | 1:1 | one active price per classroom, versioned via UpdatedAt |
| Student (1) — (N) PaymentTransaction | 1:N | a student can attempt/complete payments for many classrooms over time |
| Classroom (1) — (N) PaymentTransaction | 1:N | a classroom can be paid for by many students |
| PaymentTransaction (1) — (0..1) Enrollment | 1:1 (optional) | a successful payment produces exactly one Enrollment; free classrooms have Enrollment with no PaymentTransaction |
| Teacher (1) — (N) TeacherPayoutStatement | 1:N | one payout statement per teacher per billing period |

---

## 5. Normalization Review

**1NF:** All attributes are atomic — no comma-separated lists or repeating groups (e.g., QuestionOption and AntiCheatingEvent are separate rows, not JSON arrays inside a single column, except where JSON is *intentionally* used for genuinely unstructured AI output — see below).

**2NF:** Every non-key attribute depends on the whole primary key. Junction tables (Enrollment, ExamQuestion) carry only relationship-specific attributes (EnrolledAt/Status, OrderIndex/Points) — no partial dependency on a single side of the composite relationship.

**3NF:** No transitive dependencies. For example, `ExamAttempt.MaxScore` is stored (not derived) because it's a snapshot of the exam's total points *at attempt time* — if the teacher edits the exam's question points later, past attempts must not retroactively change. This is intentional denormalization, documented below, not a normalization violation.

**Intentional denormalization / JSON use:**
- `QuestionRubric.AIGeneratedCriteria` and `AuditLog.OldValue/NewValue` use `nvarchar(max)` JSON rather than fully normalized sub-tables. Rubric criteria and audit snapshots are variably-structured, write-once-read-rarely payloads generated by an LLM or captured as point-in-time snapshots — normalizing them would add join overhead with no query benefit, since they are never filtered/searched at the column level.
- `ExamAttempt.MaxScore` and `StudentAnswer.FinalScore` duplicate information derivable from `ExamQuestion.Points`, intentionally, to preserve historical accuracy against future edits (a standard "snapshot" pattern in transactional systems).

---

## 6. Design Decisions

- **Why `AppUser` + role extension tables instead of one wide table:** Keeps NOT NULL constraints meaningful per role (e.g., `ParentGuardianEmail` is required for students, meaningless for teachers) and avoids a table full of role-conditional nullable columns. RBAC itself is just `AppUser.Role` — a dedicated Role/Permission engine would be over-engineering for a fixed 3-role system.
- **Why `Enrollment` exists as its own table instead of a direct FK:** Students belong to *many* classrooms and classrooms hold *many* students — this is inherently many-to-many, and `Enrollment` also carries relationship metadata (`EnrolledAt`, `Status`) that doesn't belong on either side.
- **Why `MaterialVersion` is separate from `LearningMaterial`:** The agreed requirement is that exams stay permanently linked to the lesson version used at generation time. Without versioning, editing a lesson would silently invalidate the traceability of every exam built from it.
- **Why `Question` is decoupled from `Exam` via `ExamQuestion`:** Supports the question bank / reuse / question pool requirements — the same question can be pulled into multiple exams without duplication, and `ExamQuestion` lets the same question carry different point values on different exams.
- **Why `GradeOverride` is a separate append-only log rather than an `UpdatedScore` column on `StudentAnswer`:** The requirement explicitly demands full audit history (original score, new score, teacher, timestamp, reason). A single mutable column would destroy history on a second override.
- **Why `AuditLog` exists in addition to `GradeOverride`:** `GradeOverride` is a domain-specific audit trail purpose-built for grading. `AuditLog` is a generic, system-wide trail (plan changes, deletions, admin actions) — separating them keeps grading queries fast and un-cluttered by unrelated admin events.
- **Why `SubscriptionPlan` is a separate table rather than hard-coded limits on `Teacher`:** The Super Admin must be able to configure/modify plan limits without a schema or code change — this is a direct requirement.
- **Why `UsageCounter` exists in addition to `TeacherSubscription`:** Quota enforcement (exams generated this month, storage used) needs a fast, incrementable counter scoped to a billing period; recomputing this by aggregating `Exam`/`MaterialVersion` on every request would not scale.
- **Why `Notification` and `ParentReportLog` are separate:** Parent emails are a distinct compliance/traceability concern (proof of what was sent to which guardian email, once per attempt) from general in-app/push notifications to platform users, which have different read/delivery semantics.
- **Why `Enrollment` is never created before payment confirmation:** The only trustworthy signal that money actually moved is a signed server-to-server webhook from Paymob — never the client redirect. Creating `Enrollment` eagerly (e.g., right after the student clicks "Pay") would let a student join a paid classroom without ever completing payment, if they simply navigate away or the client lies about success.
- **Why `PaymentTransaction` is separate from `Enrollment`:** A transaction can fail, get abandoned, or be retried — none of that should ever produce a classroom membership row. Keeping them separate also lets a student have multiple failed attempts logged for one eventual successful Enrollment, without polluting the enrollment table with failed rows.
- **Why `PaymentWebhookLog` exists in addition to `PaymentTransaction.Status`:** Payment gateways retry webhook delivery and can send events for transactions your system doesn't recognize (retries, out-of-order delivery, replay attempts). Logging every raw inbound call — including rejected/duplicate ones — is what lets support/finance actually resolve a payment dispute six months later; `PaymentTransaction` alone only shows you the current state, not the full delivery history.
- **Why `ClassroomPricing` is its own table instead of a `Price` column on `Classroom`:** Keeps classroom identity (name, subject, teacher) decoupled from its commercial terms, and matches the same reasoning as `SubscriptionPlan` being separate from `Teacher` — pricing is a distinct concern that changes on its own schedule (and `PaymentTransaction.Amount` snapshots the price actually paid, so repricing never retroactively changes what a past payment "should" have been).
- **Why `TeacherPayoutStatement` snapshots `PlatformFeePercent` per statement instead of reading a global config value:** If Draya's commission rate changes in the future, past payout statements must still reflect the rate that was actually in effect — otherwise historical financial records would silently rewrite themselves.
- **Why `MaterialChunk` exists even though vectors live in Qdrant:** Keeps a relational, foreign-keyed link between a chunk's source (`MaterialVersion`), its character span, and its Qdrant `VectorId` — needed for debugging, re-indexing, and citation traceability without pulling vector math into SQL Server.

---

## 7. Cross-Cutting Strategies

### 7.1 Cascade Rules

| Parent | Child | On Parent Delete | Rationale |
|---|---|---|---|
| AppUser | Teacher / Student / PlatformAdmin | **Restrict** (soft-deactivate `AppUser.IsActive` instead) | Identity rows are never hard-deleted; see §7.2 |
| LearningMaterial | MaterialVersion | **Restrict** (soft delete `LearningMaterial.IsDeleted` instead) | Preserves version history even if the parent is "deleted" from the UI |
| MaterialVersion | MaterialChunk | **Cascade** | Chunks are meaningless without their parent version; safe to cascade since chunks are regenerable from the source file |
| Classroom | Enrollment | **Restrict** (soft-deactivate `Classroom.IsActive`; `Enrollment.Status = Removed` individually) | A classroom's enrollment history must survive deactivation, for both academic records and payment/audit traceability |
| Exam | ExamQuestion | **Cascade** | Junction rows are meaningless without the parent exam |
| Exam | ExamAttempt | **Restrict** | Student attempt history must never be deletable via an exam-level cascade — this is graded academic record |
| ExamAttempt | StudentAnswer | **Cascade** | Answers only make sense within their attempt; deleting an attempt (an operation that should itself be rare/admin-only) reasonably takes its answers with it |
| StudentAnswer | GradeOverride | **Restrict** | Override history is an audit trail — it must never disappear even if an answer record is otherwise touched |
| Question | QuestionOption | **Cascade** | Options are meaningless without their parent question |
| Question | QuestionRubric | **Cascade** | 1:1 dependent record |
| ExamAttempt | AntiCheatingEvent | **Cascade** | Events are meaningless without their parent attempt |
| PaymentTransaction | Enrollment | **Restrict** (FK is nullable on Enrollment; never cascades) | Enrollment must survive independently of transaction record lifecycle changes |

**General rule:** any table representing a **financial, academic, or audit record** (payments, grades, overrides, audit logs) uses `Restrict` — nothing here is ever cascade-deleted. Only genuinely dependent, regenerable, or purely-structural child rows (chunks, options, junction rows) use `Cascade`.

### 7.2 Soft Delete Strategy

Draya uses **soft deletes exclusively** for anything with academic, financial, or audit significance — hard deletes are reserved for genuinely transient/regenerable data.

| Entity | Soft-delete mechanism |
|---|---|
| AppUser | `IsActive = 0` (deactivation, not deletion) |
| Classroom | `IsActive = 0` |
| LearningMaterial | `IsDeleted = 1` |
| Question | `IsActive = 0` |
| Enrollment | `Status = 'Removed'` (not a physical delete) |
| SubscriptionPlan | `IsActive = 0` (old plans stay referenceable by historical `TeacherSubscription` rows) |

**Not soft-deleted (hard-delete acceptable):** `MaterialChunk` (regenerable from source file + re-embedding), `Notification` (ephemeral, no audit requirement discussed), `QuestionOption`/`ExamQuestion` (junction/dependent rows, cascade-deleted with their parent).

### 7.3 Multi-Tenancy Strategy

**Single database, shared schema, application-enforced tenant isolation by `TeacherId`.** There is no per-tenant database or schema, and no `TenantId` column separate from `TeacherId` — the Teacher **is** the tenant boundary (see `PROJECT_CONTEXT.md` §13 for the business rationale: no Academy Admin layer exists above teachers).

Every tenant-scoped entity (Classroom, LearningMaterial, Exam, Question, etc.) carries a direct or indirect `TeacherId`. Isolation is enforced in the **Application layer** (see `ARCHITECTURE.md` §4 Authorization Flow) — ownership is checked in each Use Case, not via row-level security or per-tenant connection strings. This was a deliberate simplicity choice appropriate to the project's scale; if true data isolation guarantees became a hard requirement (e.g., enterprise compliance), row-level security policies in SQL Server would be the natural upgrade path — flagged as a future consideration, not a current requirement.

### 7.4 Audit Fields

Every table includes `CreatedAt` (immutable, `SYSUTCDATETIME()` default). Tables representing mutable state additionally include relevant timestamps (`UpdatedAt`, `PaidAt`, `GradedAt`, `PublishedAt`, etc. — see individual entity specs above for the exact field per table). Beyond per-row timestamps, three dedicated audit mechanisms exist, each scoped to a different concern (see `BUSINESS_RULES.md` §12 for the business rationale):

1. **`GradeOverride`** — domain-specific, append-only, for every teacher score change.
2. **`AuditLog`** — general-purpose, system-wide, for administrative/sensitive actions (plan changes, deletions, admin actions), storing before/after JSON snapshots.
3. **`PaymentWebhookLog`** — payment-specific, append-only, logging every inbound Paymob call (including rejected/duplicate ones) for financial dispute resolution.

None of these three audit mechanisms are merged into one generic table — they have different write patterns, different retention needs, and different consumers (a finance team reviewing `PaymentWebhookLog` doesn't need grading noise, and vice versa).

### 7.5 Versioning Strategy

Only one entity family uses explicit content versioning: **`LearningMaterial` → `MaterialVersion`**. Every upload or edit creates a new `MaterialVersion` row (never overwrites); `IsCurrent` marks the active one; `Exam.SourceMaterialVersionId` and `Question.SourceMaterialVersionId` point at a **specific, immutable version**, guaranteeing that editing a lesson never silently changes what an already-generated exam or question was based on (see `BUSINESS_RULES.md` §4). No other entity in the schema uses this pattern — grading history uses the append-only `GradeOverride` log instead (a different technique for a different problem: correcting a mistake vs. tracking evolving source content).

### 7.6 Example Data Flow Between Entities

**Scenario: a student pays to join a classroom, then takes and is graded on an exam.**

```
1. Teacher sets price          → ClassroomPricing (Price: 150 EGP)
2. Student initiates checkout  → PaymentTransaction (Status: Pending, Amount: 150 snapshotted)
3. Paymob confirms via webhook → PaymentWebhookLog (raw payload logged)
                                → PaymentTransaction.Status → 'Paid', PaidAt set
                                → Enrollment created (PaymentTransactionId FK set, Status: 'Active')
4. Teacher generates an exam   → Exam created, linked to a specific MaterialVersion
                                → Question rows created (AIGenerated), linked to that Exam via ExamQuestion
5. Student starts the exam     → ExamAttempt created (MaxScore snapshotted from current ExamQuestion.Points sum)
6. Student answers, submits    → StudentAnswer rows created per question
                                → AI grades each → AIScore + FinalScore populated, ExamAttempt.Status → 'Submitted'
7. Teacher reviews, overrides one score
                                → StudentAnswer.FinalScore updated
                                → GradeOverride row created (OriginalScore, NewScore, TeacherId, Reason)
8. Report generated            → PerformanceReport-equivalent data aggregated (see Application-layer DTOs in API_CONTRACT.md)
                                → ParentReportLog row created, email sent to Student.ParentGuardianEmail
```

This single scenario touches 9 of the 31 entities and illustrates why the schema separates "attempt" from "payment" from "grading" from "audit" — each step above is independently retryable/idempotent and independently auditable, which would not be possible if these concerns were collapsed into fewer, wider tables.

---

## 8. dbdiagram.io DBML

> The DBML script below is also maintained as a standalone, directly-importable file: `draya_schema.dbml` (delivered alongside this document — see the repository's `/docs` folder or the original deliverable set).

```dbml
Table AppUser {
  Id uuid [pk, default: `newid()`]
  Email nvarchar(256) [not null, unique]
  PasswordHash nvarchar(512) [not null]
  Role varchar(20) [not null, note: 'Teacher | Student | SuperAdmin']
  IsActive bit [not null, default: 1]
  CreatedAt datetime2 [not null, default: `sysutcdatetime()`]
  LastLoginAt datetime2
}

Table Teacher {
  UserId uuid [pk]
  FullName nvarchar(200) [not null]
  Phone nvarchar(30)
  CreatedAt datetime2 [not null, default: `sysutcdatetime()`]
}

Table Student {
  UserId uuid [pk]
  FullName nvarchar(200) [not null]
  ParentGuardianEmail nvarchar(256) [not null]
  DateOfBirth date
  CreatedAt datetime2 [not null, default: `sysutcdatetime()`]
}

Table PlatformAdmin {
  UserId uuid [pk]
  FullName nvarchar(200) [not null]
  CreatedAt datetime2 [not null, default: `sysutcdatetime()`]
}

Table SubscriptionPlan {
  Id uuid [pk, default: `newid()`]
  Name nvarchar(100) [not null, unique]
  MaxStudents int [not null]
  MaxStorageMB int [not null]
  MonthlyExamQuota int [not null]
  PriceMonthly decimal(10,2) [not null, default: 0]
  IsActive bit [not null, default: 1]
  CreatedAt datetime2 [not null, default: `sysutcdatetime()`]
}

Table TeacherSubscription {
  Id uuid [pk, default: `newid()`]
  TeacherId uuid [not null]
  PlanId uuid [not null]
  StartDate datetime2 [not null, default: `sysutcdatetime()`]
  EndDate datetime2
  Status varchar(20) [not null, default: 'Active', note: 'Active | Expired | Cancelled']
  CreatedAt datetime2 [not null, default: `sysutcdatetime()`]

  indexes {
    (TeacherId, Status)
  }
}

Table UsageCounter {
  Id uuid [pk, default: `newid()`]
  TeacherId uuid [not null]
  PeriodMonth date [not null]
  ExamsGeneratedCount int [not null, default: 0]
  StorageUsedMB decimal(10,2) [not null, default: 0]

  indexes {
    (TeacherId, PeriodMonth) [unique]
  }
}

Table Subject {
  Id uuid [pk, default: `newid()`]
  Name nvarchar(100) [not null, unique]
}

Table Classroom {
  Id uuid [pk, default: `newid()`]
  TeacherId uuid [not null]
  SubjectId uuid [not null]
  Name nvarchar(200) [not null]
  EnrollmentCode nvarchar(20) [not null, unique]
  IsActive bit [not null, default: 1]
  CreatedAt datetime2 [not null, default: `sysutcdatetime()`]

  indexes {
    TeacherId
  }
}

Table Enrollment {
  Id uuid [pk, default: `newid()`]
  StudentId uuid [not null]
  ClassroomId uuid [not null]
  EnrolledAt datetime2 [not null, default: `sysutcdatetime()`]
  Status varchar(20) [not null, default: 'Active', note: 'Active | Removed']

  indexes {
    (StudentId, ClassroomId) [unique]
    ClassroomId
  }
}

Table LearningMaterial {
  Id uuid [pk, default: `newid()`]
  ClassroomId uuid [not null]
  TeacherId uuid [not null]
  Title nvarchar(300) [not null]
  MaterialType varchar(20) [not null, note: 'PDF | DOCX | PPTX | Video | Image']
  IsDeleted bit [not null, default: 0]
  CreatedAt datetime2 [not null, default: `sysutcdatetime()`]

  indexes {
    ClassroomId
  }
}

Table MaterialVersion {
  Id uuid [pk, default: `newid()`]
  MaterialId uuid [not null]
  VersionNumber int [not null]
  FileUrl nvarchar(500) [not null]
  FileSizeMB decimal(10,2) [not null]
  UploadedAt datetime2 [not null, default: `sysutcdatetime()`]
  ParseStatus varchar(20) [not null, default: 'Pending', note: 'Pending | Parsed | Failed']
  IsCurrent bit [not null, default: 1]

  indexes {
    (MaterialId, VersionNumber) [unique]
    (MaterialId, IsCurrent)
  }
}

Table MaterialChunk {
  Id uuid [pk, default: `newid()`]
  MaterialVersionId uuid [not null]
  ChunkIndex int [not null]
  VectorId nvarchar(100) [not null, unique]
  CharStart int [not null]
  CharEnd int [not null]

  indexes {
    MaterialVersionId
  }
}

Table VideoDetail {
  MaterialId uuid [pk]
  DurationSeconds int [not null]
  StreamUrl nvarchar(500) [not null]
  ThumbnailUrl nvarchar(500)
}

Table Exam {
  Id uuid [pk, default: `newid()`]
  ClassroomId uuid [not null]
  TeacherId uuid [not null]
  SourceMaterialVersionId uuid
  Title nvarchar(300) [not null]
  DifficultyLevel varchar(20) [not null, note: 'Easy | Medium | Hard']
  Status varchar(20) [not null, default: 'Draft', note: 'Draft | Published | Archived']
  TimeLimitMinutes int
  RandomizeQuestionOrder bit [not null, default: 1]
  RandomizeOptionOrder bit [not null, default: 1]
  CreatedAt datetime2 [not null, default: `sysutcdatetime()`]
  PublishedAt datetime2

  indexes {
    (ClassroomId, Status)
  }
}

Table Question {
  Id uuid [pk, default: `newid()`]
  TeacherId uuid [not null]
  SubjectId uuid
  SourceMaterialVersionId uuid
  QuestionType varchar(20) [not null, note: 'MCQ | TrueFalse | FillBlank | ShortAnswer | Essay']
  QuestionText nvarchar(max) [not null]
  DifficultyLevel varchar(20) [not null, note: 'Easy | Medium | Hard']
  Source varchar(20) [not null, note: 'AIGenerated | Manual | AIAssistedEdited']
  IsActive bit [not null, default: 1]
  CreatedAt datetime2 [not null, default: `sysutcdatetime()`]

  indexes {
    (TeacherId, QuestionType)
    SubjectId
  }
}

Table QuestionOption {
  Id uuid [pk, default: `newid()`]
  QuestionId uuid [not null]
  OptionText nvarchar(500) [not null]
  IsCorrect bit [not null, default: 0]
  OrderIndex int [not null]

  indexes {
    QuestionId
  }
}

Table QuestionRubric {
  QuestionId uuid [pk]
  AIGeneratedCriteria nvarchar(max) [not null]
  TeacherInstructions nvarchar(max)
  UpdatedAt datetime2 [not null, default: `sysutcdatetime()`]
}

Table ExamQuestion {
  Id uuid [pk, default: `newid()`]
  ExamId uuid [not null]
  QuestionId uuid [not null]
  OrderIndex int [not null]
  Points decimal(5,2) [not null]

  indexes {
    (ExamId, QuestionId) [unique]
    QuestionId
  }
}

Table ExamAttempt {
  Id uuid [pk, default: `newid()`]
  ExamId uuid [not null]
  StudentId uuid [not null]
  StartedAt datetime2 [not null, default: `sysutcdatetime()`]
  SubmittedAt datetime2
  Status varchar(20) [not null, default: 'InProgress', note: 'InProgress | Submitted | Flagged | Expired']
  TotalScore decimal(6,2)
  MaxScore decimal(6,2) [not null]

  indexes {
    (ExamId, StudentId) [unique]
    StudentId
  }
}

Table StudentAnswer {
  Id uuid [pk, default: `newid()`]
  AttemptId uuid [not null]
  QuestionId uuid [not null]
  SelectedOptionId uuid
  AnswerText nvarchar(max)
  AIScore decimal(5,2)
  FinalScore decimal(5,2)
  ConfidenceScore decimal(4,3)
  GradedAt datetime2

  indexes {
    (AttemptId, QuestionId) [unique]
    QuestionId
  }
}

Table GradeOverride {
  Id uuid [pk, default: `newid()`]
  StudentAnswerId uuid [not null]
  TeacherId uuid [not null]
  OriginalScore decimal(5,2) [not null]
  NewScore decimal(5,2) [not null]
  Reason nvarchar(500)
  OverriddenAt datetime2 [not null, default: `sysutcdatetime()`]

  indexes {
    StudentAnswerId
  }
}

Table AntiCheatingEvent {
  Id uuid [pk, default: `newid()`]
  AttemptId uuid [not null]
  EventType varchar(30) [not null, note: 'TabSwitch | CopyPaste | AppMinimized | FocusLoss']
  OccurredAt datetime2 [not null, default: `sysutcdatetime()`]
  SequenceNumber int [not null]

  indexes {
    AttemptId
  }
}

Table Notification {
  Id uuid [pk, default: `newid()`]
  UserId uuid [not null]
  Type nvarchar(50) [not null]
  Title nvarchar(200) [not null]
  Message nvarchar(1000) [not null]
  Channel varchar(20) [not null, note: 'InApp | Push | Email']
  IsRead bit [not null, default: 0]
  CreatedAt datetime2 [not null, default: `sysutcdatetime()`]

  indexes {
    (UserId, IsRead)
  }
}

Table ParentReportLog {
  Id uuid [pk, default: `newid()`]
  StudentId uuid [not null]
  ExamAttemptId uuid [not null, unique]
  RecipientEmail nvarchar(256) [not null]
  SentAt datetime2 [not null, default: `sysutcdatetime()`]
  Status varchar(20) [not null, default: 'Sent', note: 'Sent | Failed']

  indexes {
    StudentId
  }
}

Table ChatMessage {
  Id uuid [pk, default: `newid()`]
  ClassroomId uuid [not null]
  SenderUserId uuid [not null]
  MessageText nvarchar(max) [not null]
  IsAnnouncement bit [not null, default: 0]
  SentAt datetime2 [not null, default: `sysutcdatetime()`]

  indexes {
    (ClassroomId, SentAt)
  }
}

Table AuditLog {
  Id uuid [pk, default: `newid()`]
  EntityType nvarchar(100) [not null]
  EntityId uuid [not null]
  Action nvarchar(50) [not null]
  PerformedByUserId uuid
  OldValue nvarchar(max)
  NewValue nvarchar(max)
  Timestamp datetime2 [not null, default: `sysutcdatetime()`]

  indexes {
    (EntityType, EntityId)
    Timestamp
  }
}

// ---------- Relationships ----------
Ref: Teacher.UserId - AppUser.Id
Ref: Student.UserId - AppUser.Id
Ref: PlatformAdmin.UserId - AppUser.Id

Ref: TeacherSubscription.TeacherId > Teacher.UserId
Ref: TeacherSubscription.PlanId > SubscriptionPlan.Id
Ref: UsageCounter.TeacherId > Teacher.UserId

Ref: Classroom.TeacherId > Teacher.UserId
Ref: Classroom.SubjectId > Subject.Id

Ref: Enrollment.StudentId > Student.UserId
Ref: Enrollment.ClassroomId > Classroom.Id

Ref: LearningMaterial.ClassroomId > Classroom.Id
Ref: LearningMaterial.TeacherId > Teacher.UserId
Ref: MaterialVersion.MaterialId > LearningMaterial.Id
Ref: MaterialChunk.MaterialVersionId > MaterialVersion.Id
Ref: VideoDetail.MaterialId - LearningMaterial.Id

Ref: Exam.ClassroomId > Classroom.Id
Ref: Exam.TeacherId > Teacher.UserId
Ref: Exam.SourceMaterialVersionId > MaterialVersion.Id

Ref: Question.TeacherId > Teacher.UserId
Ref: Question.SubjectId > Subject.Id
Ref: Question.SourceMaterialVersionId > MaterialVersion.Id
Ref: QuestionOption.QuestionId > Question.Id
Ref: QuestionRubric.QuestionId - Question.Id

Ref: ExamQuestion.ExamId > Exam.Id
Ref: ExamQuestion.QuestionId > Question.Id

Ref: ExamAttempt.ExamId > Exam.Id
Ref: ExamAttempt.StudentId > Student.UserId

Ref: StudentAnswer.AttemptId > ExamAttempt.Id
Ref: StudentAnswer.QuestionId > Question.Id
Ref: StudentAnswer.SelectedOptionId > QuestionOption.Id

Ref: GradeOverride.StudentAnswerId > StudentAnswer.Id
Ref: GradeOverride.TeacherId > Teacher.UserId

Ref: AntiCheatingEvent.AttemptId > ExamAttempt.Id

Ref: Notification.UserId > AppUser.Id

Ref: ParentReportLog.StudentId > Student.UserId
Ref: ParentReportLog.ExamAttemptId > ExamAttempt.Id

Ref: ChatMessage.ClassroomId > Classroom.Id
Ref: ChatMessage.SenderUserId > AppUser.Id

Ref: AuditLog.PerformedByUserId > AppUser.Id
```

---

## 9. Final Architecture Review (Backend Team Lead Perspective)

**Weaknesses / things to watch:**
- `ExamAttempt` currently enforces one attempt per student per exam via a unique constraint. If retakes are ever allowed, this constraint must be relaxed to `(ExamId, StudentId, AttemptNumber)` — flagged now so it isn't a breaking migration surprise later.
- `Subject` is modeled as a global shared lookup. If two teachers actually want subject names scoped privately (e.g., a custom subject name only they use), this assumption needs revisiting — currently it forces reuse of the same `Subject` row across teachers.
- `QuestionOption.IsCorrect` allows, at the DB level, zero or multiple options marked correct for a single MCQ — application-layer validation must enforce "exactly one correct option" for MCQ and "exactly one" for True/False; a CHECK constraint can't easily express this cross-row rule in SQL Server without a trigger.

**Missing entities considered and deliberately excluded (with rationale):**
- A separate `Permission`/`RolePermission` table — unnecessary for 3 fixed roles; would add complexity without benefit at this scale.
- A `Country`/`Timezone` reference table — not mentioned as a requirement; can be added later without schema disruption.

**Missing indexes to add once query patterns are known:** composite index on `ExamAttempt(StudentId, Status)` for "my pending exams" dashboards, and `Notification(UserId, CreatedAt DESC)` for paginated notification feeds — omitted for now since they depend on actual read patterns, not just FK columns.

**Performance concerns:**
- `Question.QuestionText` and `StudentAnswer.AnswerText` are `nvarchar(max)` — fine for OLTP row storage in SQL Server (stored off-row automatically past the 8KB threshold), but any full-text search over question banks should use a dedicated Full-Text Index rather than `LIKE '%...%'` scans.
- `AuditLog` and `AntiCheatingEvent` are natural high-write, append-only tables — consider table partitioning by `Timestamp`/`OccurredAt` once volume grows, though this is a future-scale concern, not a v1 requirement.

**Future scalability:**
- The `MaterialChunk` → Qdrant `VectorId` pointer design means the relational schema never needs to change if the vector store is swapped or re-indexed — good separation of concerns.
- `SubscriptionPlan` being fully data-driven means new pricing tiers are a data change, not a deployment.

**Payment-specific risks (added this revision):**
- **Idempotency is the single biggest risk in this module.** Paymob will retry webhook delivery; the handler must look up by `GatewayTransactionId` and short-circuit to a no-op if `Status` is already `Paid`, or the system will double-process a payment or throw a duplicate-key exception that then causes endless gateway retries.
- **Refund handling isn't modeled as its own workflow yet** — `PaymentTransaction.Status = 'Refunded'` exists as a state, but *what triggers* a refund (classroom capacity race, teacher deletes classroom, student dispute) needs an explicit business rule before implementation; right now the schema can represent a refund but doesn't yet enforce when one is required.
- **Capacity race condition:** if `ClassroomPricing` and enrollment capacity checks aren't done inside the same transaction as `Enrollment` creation (post-webhook), two students could pay for the last slot simultaneously. This needs a DB-level constraint or transaction isolation strategy at implementation time, not just application logic.

**Conclusion:** The schema fully satisfies every item in the Phase 5 validation checklist (authentication, RBAC via role field, self-registration, Super Admin, enrollment without approval, multi-classroom students, subjects, lesson versioning across all three file types plus video, AI exam generation with question bank and rubrics, all five question types, automatic + overridable grading with full audit trail, notifications across three channels, event-driven parent emails, classroom chat, configurable subscription plans and quotas, anti-cheating with flagging, and room for future growth). No redesign was required.

---

*End of database design document. Awaiting confirmation before proceeding to any implementation phase (EF Core entity classes, migrations, or API contracts).*
