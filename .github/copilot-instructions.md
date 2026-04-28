# Copilot Instructions

## Project Overview
This project is built using **.NET (C#)**.  
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

- Follow **Clean Architecture**
- Use clear separation of concerns:
  - Domain
  - Application
  - Infrastructure
  - Presentation

- Apply SOLID principles
- Use dependency injection
- Avoid tight coupling
- Keep business logic inside the Domain layer
- Do not mix infrastructure concerns with business logic

---

## Testing Requirements

### General Rules

- Tests are mandatory for all changes
- Always run tests after completing any task
- A task is NOT complete if any test fails

### Framework

- Use **NUnit** for all tests

---

## Test Strategy

### Web-related functionality

If functionality involves Web/API/UI:

- MUST create or update **end-to-end (E2E) tests**
- Cover real user scenarios
- Validate full request/response cycle

---

### Non-Web functionality

If changes affect business logic or other projects:

- MUST create or update:
  - **Unit tests** (isolated logic)
  - **Integration tests** (component interaction)

---

## Strict TDD Workflow

1. Write or update tests FIRST
2. Run tests (they should fail)
3. Implement functionality
4. Run tests again
5. Ensure ALL tests pass
6. Refactor safely

---

## Code Quality Rules

- Prefer async/await for I/O operations
- Avoid blocking calls
- Use meaningful naming
- Avoid overengineering
- Do not introduce unnecessary abstractions
- Keep methods short and focused

---

## Error Handling & Logging

- Always handle exceptions properly
- Never swallow exceptions silently
- Use structured logging
- Log:
  - Errors
  - Important business events
  - External interactions

---

## Dependencies

- Only use **open-source NuGet packages**
- Prefer well-maintained libraries
- Avoid adding dependencies without clear need

---

## Workflow Rules

- Analyze existing code before making changes
- Follow existing architecture and patterns
- Do not introduce conflicting approaches
- If something is unclear — ASK before implementing

---

## Testing Enforcement

- If code is added or modified:
  → corresponding tests MUST be updated

- If Web functionality changes:
  → E2E tests MUST be updated

- If other parts change:
  → Unit/Integration tests MUST be updated

- After any change:
  → ALL tests MUST be executed

---

## Definition of Done

A task is complete ONLY if:

- Tests are written/updated
- All tests pass
- Code follows architecture rules
- No critical warnings or errors remain
- Functionality is fully covered by tests

---

## Additional Guidance

- Prefer simplicity over cleverness
- Write self-documenting code
- Add comments only for complex logic
- If unsure — ask instead of guessing

---

_End of instructions_
