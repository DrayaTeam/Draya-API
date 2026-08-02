# CLEAN_ARCHITECTURE_GUIDE.md

> This is the most implementation-critical document in the package. Read `ARCHITECTURE.md` first for the system-level view; this document specifies, per module, exactly what goes in each Clean Architecture layer, and closes with a full request-to-response walkthrough.

---

## 1. Dependency Direction (the one rule that matters most)

```
Presentation → Application → Domain
Infrastructure → Application → Domain
```

- **Domain** references nothing else in the solution. It is a plain class library with no NuGet packages beyond the base class library.
- **Application** references only **Domain**. It defines *interfaces* for everything it needs from the outside world (repositories, AI agents, payment gateway, email sender) but implements none of them.
- **Infrastructure** references **Application** (to implement its interfaces) and **Domain** (to persist/query its entities). It contains all framework/package-specific code: EF Core, Semantic Kernel, the Qdrant SDK, the Paymob HTTP client, SMTP/push clients.
- **Presentation** (the ASP.NET Core Web API project) references **Application** (to invoke Use Cases) and is the **composition root** — the one place where Infrastructure implementations are registered against Application interfaces via Dependency Injection (`Program.cs`/`Startup.cs`).

**Presentation never references Infrastructure types directly** — it only ever talks to Application-layer interfaces/Use Cases, and DI wires the concrete Infrastructure implementation behind the scenes. This is what makes the system framework-independent: swap EF Core for Dapper, or Paymob for Fawry, or Claude for another LLM, and only Infrastructure changes — Domain, Application, and Presentation code is untouched.

---

## 2. Per-Module Breakdown

For each module below: Domain → Application → Infrastructure → Presentation, in that dependency order.

### 2.1 Identity Module

**Domain Layer**
- Entities: `AppUser`, `Teacher`, `Student`, `PlatformAdmin`
- Value Objects: `Email` (validates format on construction), `Role` (enum: `Teacher`, `Student`, `SuperAdmin`)
- Domain Events: `TeacherRegisteredEvent`, `StudentRegisteredEvent`
- Repository Interfaces: `IAppUserRepository`, `ITeacherRepository`, `IStudentRepository`

**Application Layer**
- Commands: `RegisterTeacherCommand`, `RegisterStudentCommand`, `RefreshTokenCommand`, `LogoutCommand`
- Queries: `LoginQuery`, `GetMyProfileQuery`
- DTOs: `AuthResponseDto`, `UserSummaryDto`, `TeacherProfileDto`, `StudentProfileDto` (map 1:1 to the `API_CONTRACT.md` §2 schemas)
- Validators: `RegisterTeacherCommandValidator`, `RegisterStudentCommandValidator` (FluentValidation — enforces the password/email rules from `BUSINESS_RULES.md` §1)
- Interfaces for external services: `IPasswordHasher`, `ITokenService`

**Infrastructure Layer**
- `AppUserRepository`, `TeacherRepository`, `StudentRepository` (EF Core implementations)
- `AspNetIdentityPasswordHasher` (implements `IPasswordHasher`)
- `JwtTokenService` (implements `ITokenService`)

**Presentation Layer**
- `AuthController` — maps directly to `API_CONTRACT.md` §2 endpoints
- Request models: `RegisterTeacherRequest`, `RegisterStudentRequest`, `LoginRequest`
- `JwtAuthenticationMiddleware`, `[Authorize(Roles = "...")]` attributes

---

### 2.2 Classrooms & Enrollment Module

**Domain Layer**
- Entities: `Classroom`, `Subject`, `Enrollment`
- Domain Services: `ClassroomCapacityChecker` (encapsulates "is this classroom's teacher at their `MaxStudents` limit" — pulls in `SubscriptionPlan`/`UsageCounter` data via repository interfaces, not directly)
- Domain Events: `StudentEnrolledEvent`
- Repository Interfaces: `IClassroomRepository`, `IEnrollmentRepository`, `ISubjectRepository`

**Application Layer**
- Commands: `CreateClassroomCommand`, `UpdateClassroomCommand`, `RegenerateEnrollmentCodeCommand`, `EnrollFreeClassroomCommand`, `RemoveStudentCommand`, `SetClassroomPricingCommand`
- Queries: `GetClassroomsQuery`, `GetClassroomDetailQuery`, `GetRosterQuery`
- DTOs: `ClassroomDto`, `StudentRosterItemDto`, `ClassroomPricingDto`
- Ownership enforcement (Teacher owns Classroom) happens inside each Command/Query handler, before any Infrastructure call.

**Infrastructure Layer**
- `ClassroomRepository`, `EnrollmentRepository`, `SubjectRepository` (EF Core)

**Presentation Layer**
- `ClassroomsController` — `API_CONTRACT.md` §3

---

### 2.3 Payments Module

**Domain Layer**
- Entities: `ClassroomPricing`, `PaymentTransaction`, `PaymentWebhookLog`, `TeacherPayoutStatement`
- Value Objects: `Money` (amount + currency, with EGP-only validation baked in per `BUSINESS_RULES.md` §3), `PaymentStatus` (enum with valid-transition rules as a Domain invariant: `Pending → Paid`, `Pending → Failed`, `Paid → Refunded` are the only legal transitions)
- Domain Events: `PaymentConfirmedEvent` (raised only after a successful webhook, never on checkout initiation)
- Repository Interfaces: `IPaymentTransactionRepository`, `IClassroomPricingRepository`, `ITeacherPayoutRepository`

**Application Layer**
- Commands: `SetClassroomPricingCommand` (shared with §2.2), `InitiateCheckoutCommand`, `HandlePaymobWebhookCommand`, `GeneratePayoutStatementCommand`, `MarkPayoutPaidCommand`
- Queries: `GetCheckoutStatusQuery`, `GetTeacherPayoutsQuery`
- DTOs: `CheckoutSessionDto`, `CheckoutStatusDto`, `TeacherPayoutStatementDto`
- Interfaces for external services: `IPaymentGatewayClient` (methods: `CreatePaymentIntentAsync`, `VerifyWebhookSignatureAsync`) — **this interface mentions nothing about Paymob specifically**, so a second gateway could be added by writing a new Infrastructure implementation only.
- **`HandlePaymobWebhookCommandHandler` is the single, exclusive place in the entire codebase permitted to create an `Enrollment` for a paid classroom.** This is enforced by code structure, not just convention: no other Command has write access to create `Enrollment` when a non-zero `ClassroomPricing.Price` exists for the target classroom.

**Infrastructure Layer**
- `PaymobGatewayClient` (implements `IPaymentGatewayClient`, wraps Paymob's HTTP API and HMAC verification)
- `PaymentTransactionRepository`, `ClassroomPricingRepository`, `TeacherPayoutRepository` (EF Core)

**Presentation Layer**
- `PaymentsController` (student/teacher-facing: checkout, status, payouts) — `API_CONTRACT.md` §3A.1, 3A.2, 3A.4, 3A.5
- `PaymobWebhookController` (kept **separate** from `PaymentsController` deliberately, so it can be excluded from the standard JWT auth middleware pipeline without affecting any other payment endpoint) — `API_CONTRACT.md` §3A.3
- `AdminPayoutsController` (SuperAdmin-only) — `API_CONTRACT.md` §3A.6, 3A.7

---

### 2.4 Materials (RAG) Module

**Domain Layer**
- Entities: `LearningMaterial`, `MaterialVersion`, `MaterialChunk`, `VideoDetail`
- Domain invariant: a `LearningMaterial` can have at most one `MaterialVersion` with `IsCurrent = true` at any time (enforced when a new version is added — the previous current version is flipped to `false` in the same transaction)
- Repository Interfaces: `ILearningMaterialRepository`, `IMaterialVersionRepository`

**Application Layer**
- Commands: `UploadMaterialCommand`, `CreateNewVersionCommand`, `DeleteMaterialCommand`
- Queries: `GetMaterialsQuery`, `GetMaterialDetailQuery`, `GetParseStatusQuery`, `GetStreamUrlQuery`
- DTOs: `MaterialDto`, `MaterialVersionDto`
- Interfaces for external services: `IFileStorage` (upload/get signed URL), `IDocumentParser`, `IEmbeddingService`, `IVectorStore`

**Infrastructure Layer**
- `AzureBlobFileStorage` (implements `IFileStorage`)
- `PdfDocumentParser`, `DocxDocumentParser`, `PptxDocumentParser` (implement `IDocumentParser`, selected by `MaterialType`)
- `NovaEmbeddingService` (implements `IEmbeddingService`)
- `QdrantVectorStore` (implements `IVectorStore`)
- `MaterialParsingBackgroundJob` (the worker that runs the async parse→chunk→embed→store pipeline described in `ARCHITECTURE.md` §4)

**Presentation Layer**
- `MaterialsController` — `API_CONTRACT.md` §4

---

### 2.5 Exams & Question Bank Module

**Domain Layer**
- Entities: `Exam`, `Question`, `QuestionOption`, `QuestionRubric`, `ExamQuestion`
- Domain invariants: exactly one `QuestionOption.IsCorrect = true` for MCQ/TrueFalse; `Exam.Publish()` throws a Domain exception if `ExamQuestion` is empty or any Essay/ShortAnswer question is missing a `QuestionRubric`
- Repository Interfaces: `IExamRepository`, `IQuestionRepository`

**Application Layer**
- Commands: `GenerateExamCommand`, `CreateExamCommand`, `UpdateExamCommand`, `AddQuestionToExamCommand`, `RemoveQuestionFromExamCommand`, `ReorderExamQuestionsCommand`, `PublishExamCommand`, `CreateQuestionCommand`, `UpdateQuestionCommand`, `DeactivateQuestionCommand`
- Queries: `GetExamsQuery`, `GetExamDetailQuery`, `GetQuestionBankQuery`, `GetExamGenerationJobStatusQuery`
- DTOs: `ExamDto`, `QuestionDto`, `ExamQuestionDto`, `ExamGenerationJobDto`
- Interfaces for external services: `IExamGenerationAgent` (method: `GenerateQuestionsAsync(materialVersionId, difficulty, types, count)` → returns generated questions + `DATA_UNAVAILABLE` count, never throws on partial results)

**Infrastructure Layer**
- `SemanticKernelExamBuilderAgent` (implements `IExamGenerationAgent`; internally: retrieves chunks via `IVectorStore`, builds the temperature-0.0 guarded prompt, calls Claude Sonnet 4.6 via Semantic Kernel, applies the PII-anonymization step described in `ARCHITECTURE.md` §4, wraps the call in a Langfuse trace)
- `ExamGenerationBackgroundJob`
- `ExamRepository`, `QuestionRepository` (EF Core)

**Presentation Layer**
- `ExamsController`, `QuestionBankController` — `API_CONTRACT.md` §5

---

### 2.6 Grading Module (includes Exam Attempts & Anti-Cheating)

**Domain Layer**
- Entities: `ExamAttempt`, `StudentAnswer`, `GradeOverride`, `AntiCheatingEvent`
- Domain Services: `AntiCheatingEvaluator` (encapsulates the fixed warn-then-flag rule from `BUSINESS_RULES.md` §6 — takes the current `SequenceNumber` and returns `Warning` or `Flagged`, with no configuration input, matching the "platform-wide fixed" business rule exactly)
- Domain invariant: `StudentAnswer.FinalScore` can never exceed the associated `ExamQuestion.Points`
- Repository Interfaces: `IExamAttemptRepository`, `IStudentAnswerRepository`, `IGradeOverrideRepository`

**Application Layer**
- Commands: `StartAttemptCommand`, `SubmitAnswerCommand`, `SubmitAttemptCommand`, `RecordAntiCheatingEventCommand`, `OverrideGradeCommand`
- Queries: `GetAttemptResultsQuery`, `GetExamAttemptsQuery`, `GetAnswerDetailQuery`, `GetOverrideHistoryQuery`
- DTOs: `ExamAttemptDto`, `ExamAttemptQuestionDto`, `StudentAnswerDto`, `ExamAttemptResultDto`, `GradeOverrideDto`
- Interfaces for external services: `IGradingAgent` (method: `GradeAnswerAsync(question, rubric, studentAnswerText)` → score + confidence, used only for Essay/ShortAnswer; objective grading is pure Domain logic, no external call needed)

**Infrastructure Layer**
- `SemanticKernelGraderAgent` (implements `IGradingAgent`)
- `GradingBackgroundJob` (triggered on `SubmitAttemptCommand`)
- `ExamAttemptRepository`, `StudentAnswerRepository`, `GradeOverrideRepository` (EF Core)

**Presentation Layer**
- `AttemptsController`, `GradingController` — `API_CONTRACT.md` §5 (attempt/anti-cheating endpoints) and §6 (grading endpoints)

---

### 2.7 Reports Module

**Domain Layer**
- Value Objects: `WeakTopic` (topic, proficiency score, recommendation)
- Repository Interfaces: `IPerformanceReportRepository`, `IParentReportLogRepository`

**Application Layer**
- Commands: `GenerateReportCommand` (invoked automatically by an event handler after `SubmitAttemptCommand`'s grading completes — not directly callable by a client), `SendParentReportEmailCommand`
- Queries: `GetStudentReportsQuery`, `GetReportDetailQuery`, `GetClassroomReportOverviewQuery`, `GetParentReportLogsQuery`
- DTOs: `PerformanceReportSummaryDto`, `PerformanceReportDto`, `ClassroomReportOverviewDto`, `ParentReportLogDto`
- Interfaces for external services: `IReportGenerationAgent`, `IEmailSender`

**Infrastructure Layer**
- `SemanticKernelReportAgent` (implements `IReportGenerationAgent`)
- `SendGridEmailSender` (implements `IEmailSender`, or equivalent — **Assumption**, exact provider not decided by the team, see `DECISIONS.md`)

**Presentation Layer**
- `ReportsController` — `API_CONTRACT.md` §7

---

### 2.8 Notifications, Chat, Subscriptions, Admin Modules

Follow the identical layering pattern. Summarized (full DTO-level detail intentionally deferred — see `API_CONTRACT.md` §8, which flags these as a follow-up documentation pass):

| Module | Domain | Application | Infrastructure | Presentation |
|---|---|---|---|---|
| Notifications | `Notification` | `MarkReadCommand`, `GetNotificationsQuery` | `PushNotificationSender`, `NotificationRepository` | `NotificationsController` |
| Chat | `ChatMessage` | `PostMessageCommand`, `GetMessagesQuery` | `ChatMessageRepository` | `ChatController` |
| Subscriptions | `SubscriptionPlan`, `TeacherSubscription`, `UsageCounter` | `GetCurrentPlanQuery`, `GetUsageQuery` | `SubscriptionRepository`, `UsageCounterRepository` | `SubscriptionController` |
| Admin | (spans multiple entities) | `CreatePlanCommand`, `DeactivateTeacherCommand` | (reuses above repositories) | `AdminController` |

---

## 3. SOLID Principles Applied

- **Single Responsibility:** each Use Case (Command/Query handler) does exactly one thing — e.g., `PublishExamCommandHandler` only publishes; it does not also generate notifications directly (that's a separate event handler reacting to `ExamPublishedEvent`).
- **Open/Closed:** adding a second payment gateway (e.g., Fawry) means writing a new `FawryGatewayClient` implementing `IPaymentGatewayClient` — zero changes to `InitiateCheckoutCommand` or any Domain code.
- **Liskov Substitution:** any `IExamGenerationAgent` implementation (Semantic Kernel today, potentially a different orchestration approach later) must honor the same contract — including the `DATA_UNAVAILABLE`/partial-result behavior — so Application-layer code never needs to special-case which implementation is active.
- **Interface Segregation:** interfaces are narrow and purpose-specific (`IEmailSender` vs `INotificationSender` are separate, per `ARCHITECTURE.md` §4's explicit note that parent emails and in-app notifications are different pipelines) rather than one giant `IExternalServices` god-interface.
- **Dependency Inversion:** Application and Domain depend only on interfaces they themselves define; Infrastructure depends on (and implements) those interfaces. This is the mechanism that makes the Dependency Rule in §1 actually enforceable, not just aspirational.

## 4. How the Project Remains Framework-Independent

- Domain has zero package references beyond .NET base class library — it could theoretically be extracted and reused in a completely different host (a console app, a different web framework) with no changes.
- Application references only Domain plus lightweight, swappable cross-cutting packages (e.g., FluentValidation, AutoMapper/Mapster) — never EF Core, never ASP.NET Core, never the Anthropic/Semantic Kernel SDK, never the Paymob SDK.
- All framework/vendor-specific code is quarantined in Infrastructure, registered via DI in Presentation's composition root. If the team needed to migrate off Azure, off MSSQL, off Semantic Kernel, or off Paymob, the blast radius is Infrastructure-only.

## 5. How to Add a New Feature Following the Same Architecture

1. **Domain first:** does this feature need a new Entity, Value Object, or invariant? Add it here, with no dependencies on anything else.
2. **Application second:** define the Command/Query, its DTO, its Validator, and any new external-service interface it needs (but not the interface's implementation).
3. **Infrastructure third:** implement the repository/external-service interfaces the Application layer just declared.
4. **Presentation last:** add the Controller action, Request/Response models, and register any new DI bindings in the composition root.
5. **Update `DATABASE.md` and `API_CONTRACT.md`** in the same change — this package is meant to stay in sync with the code, not drift from it.

---

## 6. Full Feature Walkthrough: `POST /exams/{examId}/publish`

This is the same example introduced at the end of `ARCHITECTURE.md` §5, expanded to full implementation detail.

**Step 1 — Presentation Layer (`ExamsController.cs`)**
```csharp
[HttpPost("{examId}/publish")]
[Authorize(Roles = "Teacher")]
public async Task<ActionResult<ExamDto>> Publish(Guid examId)
{
    var teacherId = User.GetUserId(); // extracted from JWT claims
    var result = await _mediator.Send(new PublishExamCommand(examId, teacherId));
    return Ok(result); // AutoMapper/Mapster maps the Domain-returned Exam to ExamDto
}
```
Presentation's only jobs: authenticate, extract the caller's identity from the JWT, dispatch to Application via the Command, map the result to a DTO, choose the HTTP status code. No business logic lives here.

**Step 2 — Application Layer (`PublishExamCommandHandler.cs`)**
```csharp
public async Task<Exam> Handle(PublishExamCommand cmd)
{
    var exam = await _examRepository.GetByIdAsync(cmd.ExamId)
        ?? throw new NotFoundException(); // maps to 404

    if (exam.TeacherId != cmd.TeacherId)
        throw new NotFoundException(); // ownership failure also maps to 404, never 403 — see API_CONTRACT.md §1.1

    exam.Publish(); // Domain method — see Step 3

    await _examRepository.SaveChangesAsync();
    return exam;
}
```
Application's job: orchestrate — load via repository interface, enforce ownership, delegate the actual business rule to Domain, persist, return.

**Step 3 — Domain Layer (`Exam.cs`)**
```csharp
public void Publish()
{
    if (!Questions.Any())
        throw new CannotPublishEmptyExamException();

    foreach (var q in Questions.Where(q => q.Question.QuestionType is QuestionType.Essay or QuestionType.ShortAnswer))
    {
        if (q.Question.Rubric is null)
            throw new MissingRubricException(q.QuestionId);
    }

    Status = ExamStatus.Published;
    PublishedAt = DateTime.UtcNow;
    AddDomainEvent(new ExamPublishedEvent(Id, ClassroomId));
}
```
Domain's job: enforce the actual business invariants (from `BUSINESS_RULES.md` §5) with zero knowledge of HTTP, databases, or DI — this method is fully unit-testable with an in-memory `Exam` object and no mocks beyond plain C#.

**Step 4 — Infrastructure Layer (`ExamRepository.cs`)**
```csharp
public async Task SaveChangesAsync() => await _dbContext.SaveChangesAsync();
```
Plus, separately, an event handler (also Infrastructure-registered, Application-defined interface) reacts to `ExamPublishedEvent` to trigger student notifications — this is what connects back to `ARCHITECTURE.md` §4's Email/Notification Flow.

**Step 5 — Response**
Presentation maps the returned `Exam` Domain entity to `ExamDto` (per the schema in `API_CONTRACT.md` §5.9) and returns `200 OK`.

This walkthrough is the pattern every other endpoint in `API_CONTRACT.md` should follow — the layers and their responsibilities never change, only the specific Entities/Commands/Repositories involved.
