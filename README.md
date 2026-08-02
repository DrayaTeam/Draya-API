# Draya Backend

Draya is an AI-powered EdTech web and mobile learning platform for tutoring centers and private educational academies in Egypt. This repository contains the ASP.NET Core backend implemented using **Clean Architecture**.

## Getting Started & Documentation

All technical specifications, business rules, API contracts, architecture guides, and database schemas are maintained as the single source of truth in the [`/docs`](docs/) directory.

* **Project Memory & Context:** Read [`docs/PROJECT_CONTEXT.md`](docs/PROJECT_CONTEXT.md) first.
* **Architecture & Clean Architecture Guide:** [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) and [`docs/CLEAN_ARCHITECTURE_GUIDE.md`](docs/CLEAN_ARCHITECTURE_GUIDE.md).
* **API Contract & OpenAPI Spec:** [`docs/API_CONTRACT.md`](docs/API_CONTRACT.md) and [`docs/draya-api.yaml`](docs/draya-api.yaml).
* **Database Schema:** [`docs/DATABASE.md`](docs/DATABASE.md) and [`docs/draya_schema.dbml`](docs/draya_schema.dbml).
* **Business Rules:** [`docs/BUSINESS_RULES.md`](docs/BUSINESS_RULES.md).
* **Development Roadmap:** [`docs/DEVELOPMENT_ROADMAP.md`](docs/DEVELOPMENT_ROADMAP.md).
* **Architectural Decisions & Assumptions:** [`docs/DECISIONS.md`](docs/DECISIONS.md).

## Backend Solution Structure

```text
src/
  Draya.Domain/          # Core Domain entities, invariants, value objects & repository interfaces (Zero dependencies)
  Draya.Application/     # Use Cases, CQRS Commands/Queries, DTOs, Validators & interfaces (Depends on Domain only)
  Draya.Infrastructure/  # EF Core, Repositories, AI Agents, Paymob client & External integrations (Depends on Application & Domain)
  Draya.Api/             # Presentation Controllers, Middleware & DI Composition Root (Depends on Application & Infrastructure)

tests/
  Draya.Domain.Tests/
  Draya.Application.Tests/
  Draya.Infrastructure.Tests/
  Draya.Api.Tests/

Draya.sln
```
