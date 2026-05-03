# Copilot Instructions

## Project Overview

This project is built using **.NET (C#)** — a Blazor Server web application with Clean Architecture.

**Solution structure (`src/`):**
- `SecurityRule.Domain` — entities (`Models/`), repository interfaces (`Interfaces/`), no external dependencies
- `SecurityRule.Infrastructure` — EF Core (`AppDbContext`), repository implementations (`Repositories/`), services (`Services/`: `FakeAdService`, `ActiveDirectoryService`)
- `SecurityRule.Web` — Blazor Server UI (MudBlazor components), Razor pages (`Components/Pages/`), application services (`Services/`)
- `SecurityRule.Tests` — unit + integration tests (NUnit, EF Core InMemory DB)
- `SecurityRule.E2E.Tests` — end-to-end tests (NUnit + SpecFlow + Playwright)

> **Note:** There is NO `SecurityRule.Application` project. Business logic lives in `SecurityRule.Domain`; orchestration lives in `SecurityRule.Infrastructure/Services/` or `SecurityRule.Web/Services/`.

**Key commands:**
```bash
# Build
dotnet build src/SecurityRule.slnx

# Unit + Integration tests (~129 tests, no browser required) — always run after changes
dotnet test src/SecurityRule.Tests/

# E2E tests (require Playwright browser — run in CI only, NOT in agent sandbox)
dotnet test src/SecurityRule.E2E.Tests/
```

---

## Architecture & Design

- **Clean Architecture** layer rules:
  - **Domain** — entities, interfaces. Zero external dependencies. Never reference Infrastructure or Web.
  - **Infrastructure** — implements Domain interfaces. Contains `AppDbContext` (EF Core), repositories, `FakeAdService`, `ActiveDirectoryService`.
  - **Web** — Blazor Server pages/components (MudBlazor), page-scoped services (e.g. `GraphMapElementsBuilder`). References Domain and Infrastructure.
- Apply SOLID principles; use dependency injection everywhere.
- Keep business logic in Domain. Do not put it in repositories or Blazor components.
- Do not mix infrastructure concerns (DB, AD) with business logic.

---

## Testing Requirements

### Framework

- Use **NUnit** for all tests (both unit/integration and E2E)
- E2E tests use **SpecFlow** (Gherkin `.feature` files) + **Playwright**
  - `.feature` files are written **in Russian** (Given/When/Then на русском)
  - Step definitions go in `StepDefinitions/` folder, grouped by domain
- Use `InMemory` EF Core database for `AppDbContext` in tests — never use a real SQL Server connection
- Use `FakeAdService` singleton (not real AD) in all tests; real `ActiveDirectoryService` is Windows-only

### General Rules

- Tests are **mandatory** for all changes
- Always run `dotnet test src/SecurityRule.Tests/` after completing any task
- A task is NOT complete if any test fails
- **E2E tests cannot run in the agent sandbox** — write correct `.feature` + step definitions; they run in CI

---

## Test Strategy

### When to write what type of test

| Changed area | Required test type |
|---|---|
| Domain entity, interface | Unit test |
| Repository method | Integration test (InMemory DB) |
| Infrastructure service (FakeAdService, etc.) | Unit test with mocks or integration test |
| Blazor page, route, UI behavior | E2E `.feature` scenario + step definitions |

### Concrete rules

- Unit tests: mock all external dependencies; test a single class in isolation
- Integration tests: use real repository classes with `InMemory` `AppDbContext`; no mocks for the DB
- E2E tests: cover full user flows; use `data-testid` attributes for locators (already placed in components)

---

## Strict TDD Workflow

1. Write or update tests FIRST (they must fail before implementation — confirm this before proceeding)
2. Implement the functionality
3. Run `dotnet test src/SecurityRule.Tests/` — ensure ALL tests pass
4. Refactor safely (tests must remain green)

> For E2E-only changes (new Blazor page), write the `.feature` + step definitions first, then implement the page. E2E execution happens in CI.

---

## Post-Task Test Analysis (Professional QA Workflow)

**After completing every task**, perform the following analysis as a professional tester:

### Step 1 — Identify what changed

Determine which layers and components were affected:
- Domain models or interfaces → unit tests
- Repositories or services → integration tests
- Blazor pages, routes, or UI behavior → E2E tests
- Removed or renamed methods/properties → check for obsolete tests

### Step 2 — Decision matrix

| Change type | Action |
|---|---|
| New method / class with business logic | ✅ Write new unit tests |
| New repository method | ✅ Write new integration tests |
| New Blazor page or route | ✅ Write new E2E `.feature` scenario |
| Changed method signature or behavior | 🔄 Update existing tests to match new behavior |
| Changed UI element (label, button, selector) | 🔄 Update E2E step definitions / locators |
| Deleted method or class | 🗑️ Delete corresponding tests that no longer apply |
| Renamed entity field or property | 🔄 Update all tests referencing that field |
| Refactoring with no behavior change | ✔️ Verify existing tests pass — no changes needed |
| Bug fix | ✅ Write a regression test that fails before the fix |

### Step 3 — Execute

- Write/update/delete tests as determined in Step 2
- Run `dotnet test src/SecurityRule.Tests/` and verify all pass
- For E2E changes: write correct `.feature` + step definitions (run in CI)

### Step 4 — Verify coverage

Before closing the task, confirm:
- Every new public method has at least one test
- Every deleted method's tests are removed
- No tests reference code that no longer exists
- All unit/integration tests pass (`dotnet test src/SecurityRule.Tests/` — 0 failures)

---

## Code Quality Rules

- Prefer `async`/`await` for all I/O operations; never use `.Result` or `.Wait()`
- Use meaningful naming for classes, methods, variables, and tests
- Keep methods short and focused (single responsibility)
- Add comments only for non-obvious or complex logic; code should be self-documenting otherwise

---

## Error Handling & Logging

- Always handle exceptions explicitly; never swallow them silently (no empty `catch {}`)
- Use structured logging: `ILogger<T>` injected via DI
- Log: errors/exceptions, important business events, external service calls (AD, DB)

---

## Dependencies

- Only use **open-source NuGet packages**
- Avoid adding packages without a clear need; prefer libraries already used in the project
- Before adding a new package, check for known vulnerabilities

---

## Workflow Rules

- Always read the relevant existing code before making changes
- Follow existing patterns; do not introduce inconsistent approaches
- When something is ambiguous, make the **safest, minimal-scope assumption** and implement accordingly — do not guess wildly or add unused abstractions

---

## Definition of Done

A task is complete ONLY if ALL of the following are true:

- [ ] Tests are written/updated/deleted as required by the Post-Task Test Analysis
- [ ] `dotnet test src/SecurityRule.Tests/` passes with 0 failures
- [ ] E2E `.feature` files and step definitions are added/updated (if UI changed)
- [ ] Code follows Clean Architecture layer rules
- [ ] No build errors or critical warnings remain
- [ ] All new public functionality is covered by tests
- [ ] No orphaned tests reference deleted code

---

## Additional Guidance

- Test names must describe the scenario, not the method name:
  - ✅ `Returns_Empty_List_When_No_Servers_Exist`
  - ❌ `Test_GetAll_001`
- Prefer simplicity — avoid over-engineering or premature abstractions
- Do not create files outside the standard project structure without a clear reason

---

_End of instructions_
