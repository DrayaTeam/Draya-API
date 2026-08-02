# DEVELOPMENT_ROADMAP.md

---

## Completed

The following design/specification artifacts are **finished and approved**, and form the source-of-truth basis for all implementation work:

- ✅ **Requirements gathering** — two full clarification rounds with the team, covering multi-tenancy, exam/grading rules, content ingestion, enrollment, parent communication, anti-cheating, billing, notifications, chat, and data-retention scope. Fully preserved in `BUSINESS_RULES.md`.
- ✅ **Complete relational database design (ERD)** — 31 entities, including the payments module, with full attribute/constraint/index/relationship specification and a DBML script ready for dbdiagram.io. See `DATABASE.md`.
- ✅ **Complete API contract** — 55 endpoints across 7 modules (Auth, Classrooms, Materials, Exams, Grading, Reports, Payments), specified as both human-readable documentation and a machine-readable OpenAPI 3.0 file. See `API_CONTRACT.md`.
- ✅ **Architectural decision finalized:** Clean Architecture, with full layer-by-layer and module-by-module implementation guidance. See `ARCHITECTURE.md` and `CLEAN_ARCHITECTURE_GUIDE.md`.
- ✅ **Payment gateway selected:** Paymob, with the full checkout/webhook/payout flow designed.

**Not yet started:** any actual solution scaffolding, source code, tests, or deployment configuration.

---

## Remaining Work — Implementation Phases

### Phase 1: Solution Setup
- Create the .sln with 4 class library/project structure: `Draya.Domain`, `Draya.Application`, `Draya.Infrastructure`, `Draya.Api` (Presentation), matching `CLEAN_ARCHITECTURE_GUIDE.md` §1.
- Set up project references enforcing the Dependency Rule (Domain has zero references; Application references Domain only; Infrastructure references Application + Domain; Api references Application + Infrastructure for DI registration only).
- Add core NuGet packages: EF Core (SQL Server provider), MediatR (or equivalent for Command/Query dispatch), FluentValidation, AutoMapper/Mapster, Serilog.
- Set up `appsettings.json` structure (and Azure Key Vault / App Service configuration plan) for connection strings, JWT secrets, Anthropic API key, Paymob API keys/webhook secret, Qdrant connection details.
- Initialize the Angular and Flutter client project shells (can proceed in parallel against `API_CONTRACT.md`/`draya-api.yaml` once this phase is done, without waiting for backend implementation).

**Recommended order:** solution/project scaffolding → package installation → configuration plumbing → empty EF Core `DbContext` wired to Azure SQL (connectivity smoke test) → CI pipeline skeleton (even before there's much to build/test, get the GitHub Actions pipeline running end-to-end on a trivial build).

### Phase 2: Domain Layer Implementation
- Implement every Entity from `DATABASE.md` as a plain C# class in `Draya.Domain`, with the invariants specified in `CLEAN_ARCHITECTURE_GUIDE.md` §2 (e.g., `Exam.Publish()`, the `AntiCheatingEvaluator` domain service, `PaymentStatus` valid-transition rules).
- Implement Value Objects (`Money`, `Email`, `Role`, `PaymentStatus`, `WeakTopic`).
- Define all Repository interfaces (`I*Repository`) — no implementations yet, that's Phase 4.
- Define Domain Events (`ExamPublishedEvent`, `PaymentConfirmedEvent`, `StudentEnrolledEvent`, etc.).
- **Unit test Domain thoroughly here** — this layer requires no database, no HTTP, no mocks beyond plain objects, and is the cheapest place in the whole system to catch business-rule bugs early.

**Recommended order:** Identity entities → Classroom/Enrollment entities → Materials entities → Exam/Question entities → Grading/Attempt entities → Payment entities (deliberately last among Domain work, since it has the most subtle invariants — build confidence with the simpler modules first) → Reports/Notification/Chat/Audit entities.

### Phase 3: Application Layer Implementation
- Implement Commands/Queries and their handlers per module, per `CLEAN_ARCHITECTURE_GUIDE.md` §2.
- Implement DTOs and mapping profiles (Domain entity → DTO, matching `API_CONTRACT.md` schemas exactly).
- Implement FluentValidation validators for every Command per the validation rules in `BUSINESS_RULES.md`.
- Define the remaining external-service interfaces not yet defined in Phase 2 (`IExamGenerationAgent`, `IGradingAgent`, `IReportGenerationAgent`, `IPaymentGatewayClient`, `IVectorStore`, `IEmailSender`, `INotificationSender`).
- **Unit test with mocked repository/service interfaces** — Application logic (ownership checks, orchestration order) should be fully testable without a real database or real AI calls.

**Recommended order:** mirror Phase 2's module order, since each module's Application layer depends on that module's Domain entities being done first.

### Phase 4: Infrastructure Layer Implementation
- Implement EF Core `DbContext` with full entity configuration (Fluent API mapping every constraint/index/cascade rule from `DATABASE.md` §7.1) and initial migration.
- Implement all Repository interfaces against EF Core.
- Implement `AzureBlobFileStorage`, document parsers (PDF/DOCX/PPTX).
- Implement `QdrantVectorStore`, `NovaEmbeddingService` client.
- Implement `SemanticKernelExamBuilderAgent`, `SemanticKernelGraderAgent`, `SemanticKernelReportAgent` — including the PII-anonymization middleware and Langfuse tracing described in `ARCHITECTURE.md` §4.
- Implement `PaymobGatewayClient` (payment intent creation + HMAC webhook verification).
- Implement `JwtTokenService`, `AspNetIdentityPasswordHasher`.
- Implement `IEmailSender`/`INotificationSender` (provider choice per `DECISIONS.md`).
- Set up the background job mechanism (Hangfire, per the assumption in `ARCHITECTURE.md` §4 — confirm before building) for material parsing, exam generation, and grading.

**Recommended order:** DbContext + migrations first (everything else in this phase needs it) → repositories → file storage/parsers → AI agents (start with the Exam Builder since it's the most novel integration, learn from it before building the Grader/Report agents) → payment gateway client → auth infrastructure → background jobs last (it composes several of the above).

### Phase 5: API Layer Implementation
- Implement Controllers per `API_CONTRACT.md`, module by module, wiring each action to its corresponding Command/Query.
- Implement Request/Response models matching the DTOs exactly.
- Implement global exception-handling middleware (per `ARCHITECTURE.md` §4 Error Handling Strategy), mapping Domain/Application exceptions to the `ErrorResponse` envelope.
- Wire up Swagger/Swashbuckle to auto-generate the live OpenAPI document.
- Set up the DI composition root (`Program.cs`) registering every Infrastructure implementation against its Application interface.

**Recommended order:** Auth module first (nothing else works without it) → Classrooms → Materials → Exams/Question Bank → Grading/Attempts → Reports → Payments last among the core 7 (it has the most external dependency — real Paymob sandbox access — and benefits from the rest of the API already being stable to enroll against).

### Phase 6: Authentication & Authorization
- Wire ASP.NET Core Identity + JWT middleware into the pipeline (this can start in parallel with late Phase 4/early Phase 5, since Identity module implementation naturally comes first).
- Implement role-based `[Authorize]` attributes on every controller action per `API_CONTRACT.md`'s stated auth requirements.
- Implement the ownership-check pattern (404-not-403) consistently across every Application-layer handler that touches teacher-owned or student-owned resources.
- **Explicitly test the Paymob webhook's HMAC-only auth path** to confirm it correctly bypasses standard JWT middleware without becoming an unintended open endpoint for anything else.

### Phase 7: File Storage
- Finalize Azure Blob Storage container structure/naming (**not yet decided by the team** — flagged as an open item, see `DECISIONS.md`) and signed-URL expiry policy for both raw materials and video streaming.
- Implement upload size validation against the uploading teacher's plan (`MaxStorageMB`).
- Implement/verify the async parse pipeline end-to-end for all three text formats (PDF/DOCX/PPTX) plus video/image pass-through storage.

### Phase 8: Notifications
- Implement the in-app/push/email notification pipeline for the full event list in `BUSINESS_RULES.md` §9.
- Implement the **separate** parent-report email pipeline (§8) — do not conflate the two, per the explicit architectural note in `ARCHITECTURE.md` §4.
- Confirm push notification provider (**not yet decided by the team** — Firebase Cloud Messaging is the natural default given Flutter is the mobile framework, but this needs explicit confirmation, see `DECISIONS.md`).

### Phase 9: AI Features
- Implement and tune the Exam Builder agent prompt (RAG retrieval + guardrails), validating the `DATA_UNAVAILABLE` partial-generation behavior end-to-end.
- Implement and tune the Grader agent (rubric-based scoring + confidence output).
- Implement and tune the Report Generator agent (weak-topic aggregation + narrative summary).
- Validate the PII-anonymization pipeline with real test data — this is a **security-critical** phase step and should not be treated as "just another integration."
- Validate Langfuse tracing is capturing prompt/token/latency data correctly for all three agents.

### Phase 10: Testing
- Domain layer: pure unit tests (no mocks needed beyond plain objects) — highest priority, cheapest to write, per Phase 2.
- Application layer: unit tests with mocked repository/external-service interfaces.
- Infrastructure layer: integration tests against a real (test) Azure SQL instance and, where feasible, a Qdrant test instance; Paymob sandbox environment for payment flow tests, with particular attention to the **webhook idempotency** scenario flagged repeatedly in `DATABASE.md` §9 and `BUSINESS_RULES.md` §3.
- API layer: end-to-end tests per controller, validating both happy paths and the full `ErrorResponse` taxonomy from `API_CONTRACT.md` §1.4.
- xUnit is the confirmed test framework (per the original architecture decision).

### Phase 11: Deployment
- Finalize multi-stage Dockerfiles for API and Angular frontend.
- Set up Azure Container Registry and Azure App Service environments (staging + production, matching `draya-api.yaml`'s two declared server URLs).
- Complete the GitHub Actions pipeline: build → xUnit test run → Docker build → push to ACR → deploy to Azure App Service, gated on the test phase passing.
- Configure Azure Key Vault (or App Service configuration) for all production secrets — never commit them.
- Run a full smoke test of the payment flow against Paymob's **live** (not sandbox) webhook endpoint before considering payments production-ready, given the idempotency/security sensitivity already flagged multiple times in this package.

---

## Open Items Blocking Full Confidence in This Roadmap

These do not block *starting* implementation, but should be resolved before the phases that depend on them (cross-referenced from `DECISIONS.md`):

1. Background job technology (Hangfire assumed, not confirmed) — affects Phase 4/9.
2. Push notification provider (Firebase Cloud Messaging assumed, not confirmed) — affects Phase 8.
3. Email provider (not assumed/decided at all) — affects Phase 8.
4. Blob storage container/folder structure — affects Phase 7.
5. Refund trigger policy for payments — affects Phase 4/9 payment work and Phase 10 payment testing.
6. Retake policy for exam attempts (currently one-attempt-only assumption) — affects Phase 2/3 Grading module scope.
