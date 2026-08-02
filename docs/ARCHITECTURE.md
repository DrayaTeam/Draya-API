# ARCHITECTURE.md

> **Final, approved decision:** Draya's backend is implemented using **Clean Architecture**. This supersedes any earlier, less-specific framing of the backend as a conventional layered ASP.NET Core Web API. This document explains the architecture at a system level; `CLEAN_ARCHITECTURE_GUIDE.md` goes one level deeper into per-feature implementation guidance.

---

## 1. Why Clean Architecture (brief — full rationale in `DECISIONS.md`)

- Keeps business rules (exam generation logic, grading rules, enrollment rules, payment state transitions) independent of ASP.NET Core, Entity Framework, Semantic Kernel, or any other framework — those are all *details*, plugged in at the edges.
- Makes the AI layer (Claude/Semantic Kernel), the vector store (Qdrant), and the payment gateway (Paymob) all swappable **infrastructure** concerns, without touching core business logic — important given this is a graduation project where implementation details may need to change under time pressure without destabilizing the domain.
- Enables genuine unit testing of business rules without spinning up a database, an HTTP server, or a real AI call.

---

## 2. The Four Layers

```
┌──────────────────────────────────────────────────────────┐
│                  Presentation / API Layer                 │
│   Controllers, Request/Response models, Middleware,        │
│   Filters, Swagger, DI composition root                    │
└───────────────────────────┬────────────────────────────────┘
                             │ depends on
┌───────────────────────────▼────────────────────────────────┐
│                     Application Layer                       │
│   Use Cases (Commands/Queries), DTOs, Validators,            │
│   Mapping profiles, external-service INTERFACES              │
└───────────────────────────┬────────────────────────────────┘
                             │ depends on
┌───────────────────────────▼────────────────────────────────┐
│                       Domain Layer                           │
│   Entities, Value Objects, Enums, Domain Events,              │
│   Domain Services, Repository INTERFACES                      │
│   (No dependencies on any other layer — the core)             │
└──────────────────────────────────────────────────────────────┘
                             ▲
                             │ implements interfaces from
┌───────────────────────────┴────────────────────────────────┐
│                    Infrastructure Layer                      │
│   EF Core DbContext, Repository implementations,              │
│   Qdrant client, Anthropic/Semantic Kernel client,             │
│   Paymob client, Email/Push senders, Langfuse integration      │
└──────────────────────────────────────────────────────────────┘
```

**Dependency Rule:** dependencies only ever point **inward**, toward the Domain. The Domain layer has **zero** dependencies on any other layer or any external package (no EF Core, no ASP.NET Core, no Semantic Kernel references inside Domain). Infrastructure and Presentation both depend on Application/Domain — never the reverse. This is what "framework-independent core" means in practice, detailed further in `CLEAN_ARCHITECTURE_GUIDE.md` §Dependency Direction.

---

## 3. Layer Responsibilities Per Module

The system is organized into these modules (matching `DATABASE.md` clusters and `API_CONTRACT.md` modules): **Identity**, **Classrooms & Enrollment**, **Payments**, **Materials (RAG)**, **Exams & Question Bank**, **Grading**, **Reports**, **Notifications**, **Chat**, **Subscriptions**, **Audit**.

### Identity Module
- **Domain:** `AppUser`, `Teacher`, `Student`, `PlatformAdmin` entities; `Role` value object/enum.
- **Application:** `RegisterTeacherCommand`, `RegisterStudentCommand`, `LoginQuery`, `RefreshTokenCommand`; `IPasswordHasher`, `ITokenService` interfaces.
- **Infrastructure:** ASP.NET Core Identity integration, JWT generation/validation, EF Core repository implementations.
- **Presentation:** `AuthController` mapping directly to `API_CONTRACT.md` §2.

### Classrooms & Enrollment Module
- **Domain:** `Classroom`, `Subject`, `Enrollment` entities; business rule "a classroom cannot exceed the teacher's plan's `MaxStudents`" lives as a Domain Service or Domain invariant, not scattered in a controller.
- **Application:** `CreateClassroomCommand`, `EnrollFreeClassroomCommand`, `RegenerateEnrollmentCodeCommand`; enforces ownership (a Teacher can only act on their own classrooms) at this layer, before infrastructure is touched.
- **Infrastructure:** EF Core repositories for Classroom/Enrollment.
- **Presentation:** `ClassroomsController`.

### Payments Module
- **Domain:** `ClassroomPricing`, `PaymentTransaction`, `PaymentWebhookLog`, `TeacherPayoutStatement` entities; `PaymentStatus` value object/enum with valid-transition rules (e.g., `Pending → Paid` is valid, `Paid → Pending` is not) enforced as a Domain invariant.
- **Application:** `InitiateCheckoutCommand`, `HandlePaymobWebhookCommand` (the **only** command permitted to create a paid `Enrollment` — this rule is enforced in the Application layer's use case, not just documented), `GeneratePayoutStatementCommand`; `IPaymentGatewayClient` interface (abstracts Paymob so a second gateway could be added later without changing any Use Case).
- **Infrastructure:** `PaymobGatewayClient` (implements `IPaymentGatewayClient`), webhook signature verification, EF Core repositories.
- **Presentation:** `PaymentsController` (student-facing checkout/status endpoints) + a dedicated, minimally-middleware-wrapped `PaymobWebhookController` (see §4 Authentication Flow — this endpoint is intentionally *not* behind standard JWT auth).

### Materials (RAG) Module
- **Domain:** `LearningMaterial`, `MaterialVersion`, `MaterialChunk`, `VideoDetail` entities.
- **Application:** `UploadMaterialCommand`, `CreateNewVersionCommand`; `IDocumentParser`, `IEmbeddingService`, `IVectorStore` interfaces — none of these mention Qdrant, PDF-parsing libraries, or embedding model names; that's Infrastructure's job.
- **Infrastructure:** `QdrantVectorStore` (implements `IVectorStore`), PDF/DOCX/PPTX parsers, Nova embedding client, Cohere reranking client, Azure Blob Storage (or equivalent) for raw files.
- **Presentation:** `MaterialsController`.

### Exams & Question Bank Module
- **Domain:** `Exam`, `Question`, `QuestionOption`, `QuestionRubric`, `ExamQuestion` entities; invariant "exactly one correct option for MCQ/TrueFalse" enforced here; invariant "cannot publish with zero questions or a missing rubric" enforced here.
- **Application:** `GenerateExamCommand` (orchestrates the AI Exam Builder agent), `AddQuestionToExamCommand`, `PublishExamCommand`; `IExamGenerationAgent` interface.
- **Infrastructure:** `SemanticKernelExamBuilderAgent` (implements `IExamGenerationAgent`, wraps Claude Sonnet 4.6 calls via Semantic Kernel), EF Core repositories.
- **Presentation:** `ExamsController`, `QuestionBankController`.

### Grading Module
- **Domain:** `ExamAttempt`, `StudentAnswer`, `GradeOverride`, `AntiCheatingEvent` entities; invariant "FinalScore cannot exceed the question's Points" enforced here; the warn-then-flag anti-cheating escalation rule lives here as a Domain Service (`AntiCheatingEvaluator`), not in a controller.
- **Application:** `StartAttemptCommand`, `SubmitAnswerCommand`, `SubmitAttemptCommand` (triggers async grading), `OverrideGradeCommand`; `IGradingAgent` interface.
- **Infrastructure:** `SemanticKernelGraderAgent` (implements `IGradingAgent`), background job trigger for async grading (see §4 Background Job Flow).
- **Presentation:** `AttemptsController`, `GradingController`.

### Reports Module
- **Domain:** report/weak-topic value objects.
- **Application:** `GenerateReportCommand` (invoked automatically after grading completes), `SendParentReportEmailCommand`; `IReportGenerationAgent`, `IEmailSender` interfaces.
- **Infrastructure:** `SemanticKernelReportAgent`, SMTP/SendGrid (or equivalent) email sender implementation.
- **Presentation:** `ReportsController`.

### Notifications, Chat, Subscriptions, Audit modules
Follow the identical pattern: Domain holds the entity + any invariants, Application holds the use cases + interfaces to external senders (push/email providers), Infrastructure implements those interfaces, Presentation exposes the controllers listed in `API_CONTRACT.md`.

---

## 4. Cross-Cutting Flows

### Authentication Flow
1. Client calls `POST /auth/login` → Presentation → Application `LoginQuery` → Infrastructure validates credentials via ASP.NET Core Identity → `ITokenService` (Infrastructure) issues a JWT access token + refresh token.
2. Every subsequent request carries `Authorization: Bearer <token>`. A Presentation-layer **authentication middleware** validates the JWT signature/expiry before the request reaches any controller action.
3. **Exception carved out deliberately:** the Paymob webhook endpoint (`POST /webhooks/payments/paymob`) is **not** behind this JWT middleware — it is authenticated instead by HMAC signature verification performed inside its own Application-layer use case (`HandlePaymobWebhookCommand`). This is documented explicitly because it's the one endpoint in the whole system that intentionally breaks the "everything requires a Bearer token" pattern.

### Authorization Flow
1. After authentication, a Presentation-layer **authorization filter/attribute** checks the JWT's `role` claim against the endpoint's declared allowed role(s) (e.g., `[Authorize(Roles = "Teacher")]`).
2. **Ownership checks go one level deeper than role checks** and live in the **Application layer**, not Presentation — e.g., "is this Teacher the owner of this specific Classroom?" is answered inside the Use Case (by comparing the authenticated `TeacherId` against the entity's `TeacherId`), not by a generic attribute. This keeps the ownership business rule testable independent of ASP.NET Core.
3. A failed ownership check returns `404`, not `403` — consistent with `API_CONTRACT.md` §1.1's stated policy of never confirming a resource's existence to a caller who doesn't own it.

### File Upload/Download Flow
1. Teacher uploads a material via `multipart/form-data` → Presentation reads the stream → Application `UploadMaterialCommand` → Infrastructure stores the raw file (Azure Blob Storage or equivalent) and creates a `MaterialVersion` row with `ParseStatus: Pending`.
2. The response is `202 Accepted` immediately — parsing/chunking/embedding is asynchronous (see Background Job Flow below).
3. A background worker picks up `Pending` versions, calls `IDocumentParser` → extracts text → chunks it (1,000 chars / 150 overlap) → calls `IEmbeddingService` (Nova) → calls `IVectorStore` (Qdrant) to store vectors → writes `MaterialChunk` rows with the returned Qdrant point IDs → updates `ParseStatus` to `Parsed` or `Failed`.
4. Download/stream (video) requests go through `GET /materials/{id}/stream`, which returns a **short-lived signed URL** rather than proxying the file through the API server directly — this keeps large file transfer off the API's compute.

### Background Job Flow
Three operations are asynchronous by design (all return `202 Accepted` from their triggering endpoint, per `API_CONTRACT.md`):
- **Material parsing/chunking/embedding** (triggered by upload)
- **Exam generation** (triggered by `POST /classrooms/{id}/exams/generate`)
- **Exam grading** (triggered by `POST /attempts/{id}/submit`)

**Assumption (flagged):** the specific background job technology (Hangfire, Azure Service Bus + a worker service, or in-process `IHostedService` queues) has **not** been decided by the team. Given the ASP.NET Core + Azure stack already committed to, **Hangfire backed by the same Azure SQL database** is a reasonable, low-infrastructure-overhead default for a graduation project, with Azure Service Bus as the natural upgrade path if the team later needs true multi-instance queue durability. This must be confirmed before implementation — see `DECISIONS.md`.

Client-side, all three async operations are polled via a status endpoint (or, longer-term, could move to push notifications instead — flagged as a future improvement in `API_CONTRACT.md`'s review checklist).

### Email/Notification Flow
1. A domain event (e.g., `ExamGradedEvent`, `EnrollmentConfirmedEvent`) is raised inside the Domain/Application layer when the relevant state change happens.
2. An Application-layer event handler translates that into calls to `INotificationSender` (in-app + push) and, separately, `IEmailSender` (for the distinct parent-report email flow — see `BUSINESS_RULES.md` §8, which is **not** the same pipeline as general in-app notifications).
3. Infrastructure implements both interfaces against real providers (push notification service, SMTP/SendGrid).

### AI Processing Flow
1. A Use Case (e.g., `GenerateExamCommand`) calls an Application-layer interface (`IExamGenerationAgent`), never Semantic Kernel or the Anthropic SDK directly.
2. The Infrastructure implementation (`SemanticKernelExamBuilderAgent`) does the actual orchestration: retrieves relevant chunks from `IVectorStore` (RAG), builds the prompt with the hallucination guardrail (temperature `0.0`, explicit "answer only from context, else `DATA_UNAVAILABLE`" system instruction — see `BUSINESS_RULES.md` §5), calls Claude Sonnet 4.6, and parses the structured JSON response back into Domain entities.
3. **PII anonymization middleware** sits inside this Infrastructure implementation: before any request leaves for the Anthropic API, student names/emails are swapped for internal GUIDs; before the response is mapped back to Domain entities, GUIDs are re-mapped to real identities inside the secure database boundary. Real PII never crosses the Infrastructure→external-API boundary.
4. Every AI call is wrapped in a **Langfuse trace** (prompt payload, token usage, latency, and error/hallucination flags) for observability — this instrumentation lives entirely in Infrastructure, invisible to Domain/Application.

### Error Handling Strategy
- Domain layer throws **Domain-specific exceptions** (e.g., `InvalidGradeOverrideException`, `ClassroomCapacityExceededException`) — plain C# exceptions with no framework dependency.
- A single **global exception-handling middleware** in the Presentation layer catches these (and unhandled exceptions) and maps them to the standard `ErrorResponse` envelope defined in `API_CONTRACT.md` §1.4, with the correct HTTP status code per exception type.
- Validation failures (Application-layer, e.g., FluentValidation) map to `400` with per-field `details`.

### Logging Strategy
- Structured logging (e.g., Serilog) at the Infrastructure/Presentation boundary, correlated by `X-Request-Id` (per `API_CONTRACT.md` §1.2).
- AI-specific logging (prompts, tokens, latency) goes through **Langfuse** specifically, not the general application log — this is a deliberate separation so AI observability data doesn't get lost in general request logs.
- Payment webhook calls are logged twice, deliberately: once in the general structured log (for ops visibility) and once, immutably, in `PaymentWebhookLog` (for financial dispute resolution) — see `BUSINESS_RULES.md` §12.

### Caching Strategy
**Assumption (flagged, not explicitly discussed by the team):** no caching requirement was raised during requirements gathering. A reasonable default for a graduation-project scope: in-memory caching (`IMemoryCache`) for read-heavy, rarely-changing lookups (`Subject` list, active `SubscriptionPlan` list) only — no distributed cache (e.g., Redis) is assumed necessary at this stage given expected traffic volume. Revisit if load testing suggests otherwise.

### Queue Strategy
See Background Job Flow above — the three async operations (material parsing, exam generation, grading) are the platform's only queue-worthy operations currently identified. No other queue-based workflow was discussed.

### Deployment Considerations
- **Hosting:** Azure App Service for both the Angular frontend and the ASP.NET Core API, with Azure SQL Database (managed MSSQL).
- **Containerization:** multi-stage Dockerfiles for both the API and the frontend, ensuring dev/prod parity.
- **CI/CD:** GitHub Actions triggers on every push to `main` — runs the xUnit test suite, builds Docker images, pushes to Azure Container Registry, deploys to Azure App Service.
- **Secrets:** Anthropic API keys, Paymob API keys/webhook secrets, and connection strings are never committed — they belong in Azure App Service configuration / Key Vault (**Assumption**: exact secrets-management approach wasn't explicitly discussed beyond "not hard-coded"; Key Vault is the standard Azure-native choice).

### Scalability Considerations
- Clean Architecture's separation means the Infrastructure implementations for AI calls, vector search, and payment processing can each be scaled or swapped independently without touching Domain/Application code.
- The **multi-tenant-by-TeacherId** model (no per-tenant database) means horizontal scaling of the API tier is straightforward — no tenant-routing logic needed at the infrastructure level.
- Async background jobs (material parsing, exam generation, grading) are the natural first candidates for independent horizontal scaling if load increases, since they're already decoupled from the synchronous request/response cycle.

---

## 5. Summary Diagram — Request Lifecycle Example

`POST /exams/{examId}/publish` (see `CLEAN_ARCHITECTURE_GUIDE.md` for the full walkthrough of this exact example):

```
Angular/Flutter Client
   │  Authorization: Bearer <JWT>
   ▼
Presentation: ExamsController.Publish(examId)
   │  authenticates JWT, checks role = Teacher
   ▼
Application: PublishExamCommandHandler
   │  loads Exam via IExamRepository (interface)
   │  checks ownership (TeacherId match) → 404 if not owner
   │  checks Domain invariants (has questions? rubrics complete?)
   ▼
Domain: Exam.Publish() — enforces its own invariants, raises ExamPublishedEvent
   ▼
Infrastructure: EfExamRepository.SaveChanges() — persists to Azure SQL
   ▼
Application: event handler → INotificationSender → students notified
   ▼
Presentation: maps updated Exam to ExamDto → 200 OK
```
