# Copilot Instructions

## Project Overview

This project is built using **.NET (C#)** — a Blazor Server web application with Clean Architecture.

**Solution structure:**
- `src/SecurityRule.Domain` — entities, interfaces, domain logic
- `src/SecurityRule.Application` — use cases, application services
- `src/SecurityRule.Infrastructure` — EF Core, repositories, FakeAdService, ActiveDirectoryService
- `src/SecurityRule.Web` — Blazor Server UI (MudBlazor), Razor components
- `src/SecurityRule.Tests` — unit and integration tests (NUnit, InMemory DB)
- `src/SecurityRule.E2E.Tests` — end-to-end tests (NUnit + SpecFlow + Playwright)

**Key commands:**
```bash
# Build
dotnet build src/SecurityRule.sln

# Unit + Integration tests (81 tests, no browser required)
dotnet test src/SecurityRule.Tests/

# E2E tests (require Playwright browser — run in CI only)
dotnet test src/SecurityRule.E2E.Tests/
```

All code must be production-ready, maintainable, and aligned with modern engineering practices.

---

## Development Principles

Always follow these methodologies:

- **TDD (Test-Driven Development)** — write tests before implementation
- **BDD (Behavior-Driven Development)** — focus on behavior and business scenarios
- **DDD (Domain-Driven Design)** — model the domain explicitly
- **FDD (Feature-Driven Development)** — implement features incrementally
- **MDD (Model-Driven Development)** — rely on domain models as the foundation

---

## General Rules

- Follow clean architecture principles
- Prefer small, modular functions
- Write readable and maintainable code
- Always include error handling
- Add logging where appropriate

---

## Architecture & Design

- Follow **Clean Architecture** with strict layer separation:
  - **Domain** — entities, interfaces, no external dependencies
  - **Application** — use cases, orchestration
  - **Infrastructure** — EF Core (`AppDbContext`), repositories, `FakeAdService`
  - **Presentation** — Blazor Server pages and components

- Apply SOLID principles
- Use dependency injection
- Avoid tight coupling
- Keep business logic inside the Domain layer
- Do not mix infrastructure concerns with business logic

---

## Testing Requirements

### Framework

- Use **NUnit** for all tests (both unit/integration and E2E)
- E2E tests use **SpecFlow** (Gherkin `.feature` files) + **Playwright**
- Use `InMemory` EF Core database for `AppDbContext` in tests
- Use `FakeAdService` singleton (not real AD) in all tests

### General Rules

- Tests are mandatory for all changes
- Always run `dotnet test src/SecurityRule.Tests/` after completing any task
- A task is NOT complete if any test fails

---

## Test Strategy

### Web-related functionality (Blazor pages/components, routes, UI behavior)

If functionality involves UI, pages, or routes:

- MUST create or update **E2E tests** (`.feature` file + step definitions in `SecurityRule.E2E.Tests`)
- Cover real user scenarios in Gherkin (Given/When/Then in Russian)
- Validate full page rendering and interaction via Playwright
- Step definitions go in `StepDefinitions/` folder, grouped by domain

### Non-Web functionality (domain logic, repositories, services)

If changes affect business logic, repositories, or infrastructure:

- MUST create or update:
  - **Unit tests** — isolated logic with mocked dependencies
  - **Integration tests** — using `InMemoryDbContext` and real repository implementations

---

## Strict TDD Workflow

1. Write or update tests FIRST
2. Run tests (they MUST fail at this point)
3. Implement the functionality
4. Run tests again
5. Ensure ALL tests pass
6. Refactor safely (tests must remain green)

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
- All 81+ unit/integration tests are green

---

## Code Quality Rules

- Prefer `async`/`await` for all I/O operations
- Avoid blocking calls (no `.Result` or `.Wait()`)
- Use meaningful naming — classes, methods, variables, test names
- Avoid overengineering and unnecessary abstractions
- Keep methods short and focused (single responsibility)

---

## Error Handling & Logging

- Always handle exceptions properly
- Never swallow exceptions silently
- Use structured logging (`ILogger<T>`)
- Log:
  - Errors and exceptions
  - Important business events
  - External service interactions (AD, DB)

---

## Dependencies

- Only use **open-source NuGet packages**
- Prefer well-maintained, widely adopted libraries
- Avoid adding dependencies without a clear need
- Check for known vulnerabilities before adding new packages

---

## Workflow Rules

- Analyze existing code before making changes
- Follow existing architecture and patterns
- Do not introduce conflicting approaches
- If something is unclear — ASK before implementing

---

## Definition of Done

A task is complete ONLY if ALL of the following are true:

- [ ] Tests are written/updated/deleted as required by the Post-Task Test Analysis
- [ ] `dotnet test src/SecurityRule.Tests/` passes with 0 failures
- [ ] E2E `.feature` files and step definitions are updated (if UI changed)
- [ ] Code follows Clean Architecture rules
- [ ] No critical warnings or errors remain
- [ ] All new public functionality is covered by tests
- [ ] No orphaned tests reference deleted code

---

## Additional Guidance

- Prefer simplicity over cleverness
- Write self-documenting code
- Add comments only for non-obvious or complex logic
- If unsure — ask instead of guessing
- Test names should describe the scenario, not the implementation:
  - ✅ `Returns_Empty_List_When_No_Servers_Exist`
  - ❌ `Test_GetAll_001`

---

_End of instructions_
