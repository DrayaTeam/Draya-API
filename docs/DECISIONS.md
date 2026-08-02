# DECISIONS.md

> Format per decision: **Decision** / **Reason** / **Alternatives Considered**. Decisions marked **[CONFIRMED]** were explicitly agreed by the team. Decisions marked **[ASSUMPTION]** were made by the AI assistant to keep the project moving and are explicitly flagged for team sign-off — do not treat these as settled the way confirmed decisions are.

---

## Architecture

### Decision: Clean Architecture (Domain / Application / Infrastructure / Presentation) **[CONFIRMED]**
**Reason:** Keeps business rules (exam generation logic, grading rules, enrollment/payment state transitions) independent of ASP.NET Core, EF Core, Semantic Kernel, or any other framework. Enables genuine unit testing of business logic without a database, HTTP server, or real AI/payment calls. Makes the AI provider, vector store, and payment gateway swappable Infrastructure concerns without touching core logic — valuable given the team may need to change implementation details under graduation-project time pressure.
**Alternatives Considered:** Traditional 3-layer/N-tier architecture (Controllers → Services → Repositories) — simpler to scaffold initially, but couples business rules to EF Core/ASP.NET Core more tightly, making the AI-agent and payment-gateway swappability goals harder to guarantee. Vertical Slice Architecture — considered, but Clean Architecture's clearer layer boundaries were judged easier for a team of 7 to divide work across without stepping on each other, and better suited to explaining "why" a given piece of code lives where it does to a new contributor or evaluator.

### Decision: Single ASP.NET Core Web API project structure, not microservices **[ASSUMPTION]**
**Reason:** Given the project scope (ITI graduation project) and team size, a modular monolith following Clean Architecture gives most of the maintainability benefit of service boundaries without the operational overhead (service discovery, distributed transactions, network latency between services) that would be disproportionate to the project's scale.
**Alternatives Considered:** Microservices per module (Identity service, Exams service, Payments service, etc.) — rejected as premature complexity for this project's scale and timeline; the Clean Architecture module boundaries already provide a clear path to splitting into services later if ever needed, without having designed for it prematurely.

---

## Authentication & Authorization

### Decision: JWT Bearer tokens via ASP.NET Core Identity **[CONFIRMED — implied by "handled securely via JWT (JSON Web Tokens) managed by ASP.NET Core Identity" in original requirements]**
**Reason:** Stateless, works cleanly across both the Angular web client and the Flutter mobile client without server-side session storage, and is the standard, well-supported approach in the ASP.NET Core ecosystem.
**Alternatives Considered:** Cookie-based session auth — rejected because it complicates the mobile (Flutter) client and cross-origin API access. OAuth2/OpenID Connect via a third-party identity provider (e.g., Auth0, Azure AD B2C) — not discussed by the team; would add external dependency cost/complexity not clearly justified for this project's scope.

### Decision: 3 fixed roles (Teacher, Student, SuperAdmin), no dynamic permission engine **[CONFIRMED — derived from explicit rejection of an Academy Admin role and confirmation of exactly these 3 roles]**
**Reason:** With only 3 roles and no indication of needing more, a `Role` field on `AppUser` checked via `[Authorize(Roles=...)]` plus Application-layer ownership checks is sufficient and avoids the complexity of a full Role/Permission/Claim engine that would add development cost without corresponding value at this scale.
**Alternatives Considered:** Full RBAC with a `Permission`/`RolePermission` table — explicitly considered and rejected in the database design phase as over-engineering for 3 fixed roles.

### Decision: Ownership checks return 404, never 403 **[CONFIRMED — established during API contract design]**
**Reason:** Prevents a caller from confirming a resource exists (e.g., a specific classroom ID) purely by observing a 403 vs 404 response, which is a minor but real information-leakage vector.
**Alternatives Considered:** Standard 403 for authenticated-but-forbidden — more conventional REST semantics, but rejected in favor of the stronger privacy guarantee given the platform handles student academic and payment data.

---

## Data & Persistence

### Decision: MSSQL Server / Azure SQL Database **[CONFIRMED]**
**Reason:** Explicitly specified by the team from the original project pitch; aligns with the ASP.NET Core + Azure hosting stack already committed to.
**Alternatives Considered:** PostgreSQL — a common alternative in the .NET ecosystem, but never proposed by the team; MSSQL was a given, not a decision point.

### Decision: Entity Framework Core as the ORM **[ASSUMPTION]**
**Reason:** The team specified ASP.NET Core + MSSQL but did not explicitly name an ORM. EF Core is the default, best-integrated choice for this stack, with first-class support for the Clean Architecture repository pattern used throughout `CLEAN_ARCHITECTURE_GUIDE.md`.
**Alternatives Considered:** Dapper (micro-ORM, more manual SQL control, less "magic") — would work fine with Clean Architecture too, but EF Core's migration tooling is a better fit for a fast-moving graduation project schema that's still evolving (as evidenced by the payments module being added after initial design).

### Decision: Single database, application-enforced multi-tenancy by `TeacherId` **[CONFIRMED — derived from explicit rejection of Academy Admin/per-academy isolation]**
**Reason:** There is no organizational tenant above the Teacher in this system's business model, so the Teacher naturally *is* the tenant boundary; a single shared-schema database with ownership checks in the Application layer is simplest and sufficient.
**Alternatives Considered:** Database-per-tenant or schema-per-tenant — would provide stronger isolation guarantees but adds significant operational complexity (migrations across N databases) that is unjustified given the business model has no per-academy grouping requirement at all.

### Decision: Soft delete for academically/financially/audit-significant entities; hard delete elsewhere **[CONFIRMED — derived from requirement that exams stay linked to historical lesson versions, and that grading/payment history must be preserved]**
**Reason:** Academic records (materials, classrooms, enrollments, questions) and financial records (payments) must never disappear from the system even when a user-facing "delete" action is taken, both for traceability and because other entities (exams, grades) may still reference them.
**Alternatives Considered:** Hard delete everywhere with cascade — rejected outright; would violate the explicit lesson-versioning requirement and grading-audit requirements. Full temporal tables (SQL Server system-versioned tables) for every entity — considered, but judged as more infrastructure than needed given the explicit audit tables (`GradeOverride`, `AuditLog`, `PaymentWebhookLog`) already cover the specific auditability the team asked for.

### Decision: Three separate audit mechanisms (`GradeOverride`, `AuditLog`, `PaymentWebhookLog`) rather than one generic table **[CONFIRMED — derived from explicit grading-audit and payment-traceability requirements]**
**Reason:** Each has a distinct write pattern, retention need, and consumer (a finance reviewer doesn't need grading noise; a teacher reviewing override history doesn't need webhook payloads).
**Alternatives Considered:** One generic `AuditLog` for everything — simpler schema, but would mix high-frequency grading changes with rare admin actions and payment webhook noise, making any single consumer's queries less efficient and less clear.

---

## File Storage

### Decision: Cloud blob storage (Azure Blob Storage) with signed URLs for streaming/download **[ASSUMPTION]**
**Reason:** Given the Azure hosting commitment, Azure Blob Storage is the natural choice for raw file storage (PDF/DOCX/PPTX/Video/Image), and signed URLs keep large file transfer off the API server's compute, per the file-handling flow in `ARCHITECTURE.md` §4.
**Alternatives Considered:** Storing files directly in SQL Server as `varbinary(max)` — rejected; poor fit for large video files and unnecessarily couples file storage to the relational database's backup/scaling characteristics. Exact container/folder naming convention is still an **open item** (see `DEVELOPMENT_ROADMAP.md` Phase 7).

---

## Notifications

### Decision: Three channels — in-app, push, email; no SMS **[CONFIRMED]**
**Reason:** Explicitly stated by the team as sufficient coverage; SMS was explicitly excluded.
**Alternatives Considered:** Adding SMS for exam reminders — discussed implicitly by omission; not pursued given the explicit "no SMS" confirmation.

### Decision: Parent-report emails are a separate pipeline from general in-app/push/email notifications **[CONFIRMED — derived from parents having no account, and the distinct event-driven-per-exam trigger vs. general notification triggers]**
**Reason:** Parent notifications go to a non-authenticated email address with different trigger logic (always fires once per completed exam, no user-configurable preferences) than the general `Notification` entity, which is scoped to authenticated `AppUser` accounts.
**Alternatives Considered:** Unifying both under one `Notification` table with a nullable `UserId` — rejected during database design because it would force an awkward nullable-FK pattern and blur two genuinely different business processes.

### Decision: Push notification provider **[ASSUMPTION — not yet decided]**
**Reason:** Not discussed by the team. Firebase Cloud Messaging is flagged as the natural default given Flutter is the confirmed mobile framework (excellent first-party Flutter support), but this requires explicit team confirmation before Phase 8 implementation.
**Alternatives Considered:** Azure Notification Hubs (better fit if the team wants to stay entirely within Azure) — genuinely viable alternative, not ruled out, just not yet decided.

### Decision: Email provider **[ASSUMPTION — not yet decided]**
**Reason:** Not discussed by the team at all. SendGrid is used as a placeholder example in `CLEAN_ARCHITECTURE_GUIDE.md` §2.7 purely for concreteness — this is not a real recommendation, just illustrative, and must be confirmed before Phase 8.
**Alternatives Considered:** Azure Communication Services Email, SMTP via a generic provider — all viable, none decided.

---

## AI Integration

### Decision: Claude Sonnet 4.6 via Anthropic API, orchestrated by Microsoft Semantic Kernel **[CONFIRMED]**
**Reason:** Explicitly specified by the team; Claude's Arabic-language capability was specifically cited as important given the platform's Arabic-first, Egypt-focused audience. Semantic Kernel was chosen specifically because it integrates natively with ASP.NET Core's C# dependency injection, avoiding the overhead of Python-based orchestration tools that don't fit the chosen backend stack.
**Alternatives Considered:** LangChain (Python-based) — rejected due to poor fit with the ASP.NET Core backend. Direct Anthropic SDK calls without an orchestration framework — would work but lose Semantic Kernel's plugin/prompt-pipeline structure that the team explicitly wanted for the agent-based design (Exam Builder, Grader, Report Generator).

### Decision: Qdrant Cloud (Free Tier) as the vector database, accessed via its official C# SDK **[CONFIRMED]**
**Reason:** Explicitly specified; provides instant setup and an isolated semantic search engine, keeping heavy vector math out of the operational MSSQL database.
**Alternatives Considered:** pgvector (if PostgreSQL had been chosen) — moot, since MSSQL was already the confirmed relational database. Azure AI Search — a viable Azure-native alternative, not pursued since Qdrant was explicitly named by the team.

### Decision: Fixed-size chunking (1,000 characters, 150-character overlap) with metadata pre-filtering **[CONFIRMED]**
**Reason:** Explicitly specified chunking strategy; metadata tags (`SubjectId`, `LessonId`, `DifficultyLevel`) enable pre-filtering inside Qdrant before running vector similarity matching, improving relevance and reducing search cost.
**Alternatives Considered:** Semantic/recursive chunking (splitting on natural content boundaries rather than fixed character counts) — generally produces higher-quality chunks for retrieval, but was not the team's chosen approach; noted here for awareness if retrieval quality issues arise during implementation.

### Decision: Temperature 0.0 + explicit `DATA_UNAVAILABLE` guardrail for grading/exam generation **[CONFIRMED]**
**Reason:** Explicitly specified as the hallucination-prevention mechanism — deterministic output for grading/generation, and an explicit instruction to admit insufficient context rather than inventing content.
**Alternatives Considered:** Higher temperature with post-hoc fact-checking against source chunks — more complex to implement correctly and was not the team's chosen approach.

### Decision: PII anonymization via GUID substitution before any data reaches Claude **[CONFIRMED]**
**Reason:** Explicitly specified as the privacy mechanism — a middleware pipeline replaces student names/emails with internal GUIDs before requests leave for the Anthropic API, and re-maps responses back to real profiles inside the secure database boundary.
**Alternatives Considered:** Not sending student-identifying data to the LLM at all (fully anonymous prompts with no re-identification) — would be simpler but the team's design explicitly requires re-mapping results back to real students (e.g., for grading), so full anonymity without re-identification wasn't viable.

### Decision: Langfuse for AI observability **[CONFIRMED]**
**Reason:** Explicitly specified; integrated at the ASP.NET Core API layer so every exam-generation or grading call is traced for prompt payloads, token usage, latency, and hallucination detection.
**Alternatives Considered:** Building custom logging around AI calls — rejected in favor of a purpose-built observability tool that already solves this problem well.

---

## Grading & Exams

### Decision: Immediate grade release for all question types, teacher override afterward with full audit trail **[CONFIRMED — final answer, resolving an earlier conflict; see below]**
**Reason:** The team's later, more specific clarification confirmed grades release immediately with no approval gate, for both objective and AI-graded subjective questions, prioritizing student/parent visibility speed over a review-before-release model. Every override is captured immutably (`GradeOverride`) so correction remains fully auditable even without a pre-release gate.
**Alternatives Considered:** A pre-release teacher-approval gate for AI-graded subjective answers (Essay/Short Answer) specifically — this was the model implied by the *original* pitch document's `ParentReport.IsApproved` language, but the team's later, more specific answer explicitly ruled out any approval workflow. **This is a genuine conflict between two things the team said at different points, resolved here in favor of the later, more specific statement** — flagged prominently in `PROJECT_CONTEXT.md` and `BUSINESS_RULES.md` §8 for re-confirmation, since it was never explicitly reconciled by the team itself.

### Decision: AI-generated rubric + teacher-added instructions = final rubric (never one alone) **[CONFIRMED]**
**Reason:** Explicitly specified — balances AI efficiency (auto-generating a starting rubric) with teacher control (adding constraints before publishing).
**Alternatives Considered:** Fully AI-owned rubrics with no teacher input — rejected; teacher input was explicitly requested. Fully teacher-authored rubrics with AI only as an optional starting suggestion the teacher must accept/reject wholesale — rejected in favor of the additive combination model the team specified.

### Decision: One attempt per student per exam **[ASSUMPTION]**
**Reason:** No retake policy was ever discussed by the team. A single-attempt model is the simplest default and matches how many timed academic exams work in practice, but this is genuinely unconfirmed.
**Alternatives Considered:** Multiple attempts with an `AttemptNumber` — designed for as an easy extension path (noted explicitly in `DATABASE.md` §9) but not implemented as the default, since retake policy (unlimited? capped? does the best/latest/first attempt count?) was never specified and shouldn't be guessed at in more detail than "flag it."

---

## Anti-Cheating

### Decision: Behavior-monitoring only (tab-switch, copy/paste, app-minimize/focus-loss); no camera/mic/AI visual proctoring; fixed warn-then-flag escalation **[CONFIRMED]**
**Reason:** Explicitly specified by the team, with the explicit exclusion of camera/mic proctoring likely reflecting both privacy sensitivity and implementation complexity/cost tradeoffs appropriate for a graduation project.
**Alternatives Considered:** AI-based webcam proctoring (gaze tracking, environment scanning) — a common approach in commercial exam platforms, explicitly ruled out by the team. Configurable violation thresholds per teacher/exam — considered, but the team explicitly confirmed a fixed, platform-wide threshold instead.

---

## Payments

### Decision: Paymob as the payment gateway **[CONFIRMED]**
**Reason:** Team's explicit choice, well-suited to the Egyptian market (supports cards, Fawry, Vodafone Cash, Meeza) compared to alternatives with weaker or no support for Egyptian cash-based payment methods.
**Alternatives Considered:** Stripe (cleaner API/docs, but weak/no Egyptian cash-payment-method support — a significant gap for a mass-market Egyptian tutoring audience); Fawry directly (viable but narrower than Paymob's multi-method coverage).

### Decision: Draya as merchant of record, with a configurable platform commission **[ASSUMPTION]**
**Reason:** Simpler initial integration (one platform-level Paymob account rather than per-teacher merchant onboarding) and enables Draya to actually monetize via commission, consistent with the project's stated business-goal of generating SaaS + transaction revenue.
**Alternatives Considered:** Teachers connect their own Paymob/merchant accounts, with Draya never touching the money directly — would remove the need for `TeacherPayoutStatement` entirely and shift compliance/onboarding burden to individual teachers; this is a **live alternative** the team should explicitly rule in or out, since it changes real implementation scope.

### Decision: One-time payment per classroom enrollment, not recurring/subscription access **[ASSUMPTION]**
**Reason:** Matches the simplest interpretation of "pay to join a classroom" and avoids building a recurring-billing/access-expiry system without evidence it's needed.
**Alternatives Considered:** Recurring monthly access per classroom (common in tutoring-center billing) — a real possibility given the project's tutoring-center target market, genuinely plausible, and should be explicitly confirmed since it would require a `Subscription`-style entity with renewal dates and access-expiry checks gating material/exam visibility — a materially different schema shape than what's currently built.

### Decision: Server-to-server webhook is the *only* mechanism permitted to confirm payment and create a paid Enrollment; the client is never trusted **[CONFIRMED — standard payment-security practice, applied without needing explicit team sign-off since it's not a business-rule question but a security requirement]**
**Reason:** A client (browser/app) reporting "payment succeeded" is not verifiable proof — it can be faked, dropped, or manipulated. Only a signature-verified message from Paymob's own servers is trustworthy.
**Alternatives Considered:** Trusting a client-side redirect/callback as payment confirmation — this is a well-known anti-pattern that would allow students to fake free access to paid classrooms; not seriously considered.

### Decision: Refund trigger policy — **not yet decided** **[OPEN ITEM, not even an assumption yet]**
**Reason:** The `Refunded` status exists in the schema (`PaymentTransaction.Status`) but no business rule yet defines what specifically causes a refund (capacity race at enrollment time, teacher deleting a classroom post-payment, a formal student dispute process, or some combination). This is flagged as needing explicit team input before Phase 4/9/10 payment implementation and testing, rather than guessed at.
**Alternatives Considered:** N/A — deliberately left open rather than assumed, since a wrong guess here has real financial/legal implications.

---

## Reporting

### Decision: AI-generated weak-topic analysis, automatically emailed to parents per completed exam, no approval gate **[CONFIRMED — see Grading section above for the full conflict-resolution reasoning]**
**Reason:** See the "Immediate grade release" decision above — the same later clarification round that removed the grading approval gate also removed the parent-report approval gate, in favor of full automation.
**Alternatives Considered:** See above.

---

## Background Processing

### Decision: Hangfire (backed by the existing Azure SQL database) for background jobs **[ASSUMPTION]**
**Reason:** The team never discussed background job technology explicitly, despite the system clearly needing asynchronous processing for material parsing, exam generation, and grading (all confirmed to be async operations returning `202 Accepted`). Hangfire requires minimal additional infrastructure (reuses the existing SQL database as its job store) which fits a graduation project's operational simplicity needs.
**Alternatives Considered:** Azure Service Bus + a dedicated worker service — a more scalable, production-grade pattern, and the natural upgrade path if the team later needs true multi-instance queue durability; not chosen as the default given the added infrastructure setup cost relative to project scope. In-process `IHostedService`-based queues — simplest possible option, but offers no durability across app restarts, which is a meaningful risk for something like "grading a submitted exam" where losing a queued job is a real problem.
