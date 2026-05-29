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

# E2E tests (Playwright + Chromium) — ALWAYS run after completing any task
# Step 1: Build (auto-downloads Playwright Chromium via MSBuild AfterTargets)
dotnet build src/SecurityRule.E2E.Tests/
# Step 2: Install Playwright system dependencies (required once per environment)
pwsh src/SecurityRule.E2E.Tests/bin/Debug/net10.0/playwright.ps1 install chromium --with-deps
# Step 3: Run E2E tests
dotnet test src/SecurityRule.E2E.Tests/ --no-build
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

### General Rules — MANDATORY

> ⛔ **A task without new or updated tests is INCOMPLETE — do not consider it done.**

- Tests **must** be written or updated for **every** task, no exceptions
- Any UI change (new button, new page, new form field, new route) **requires** at minimum one new E2E `.feature` scenario and the corresponding step definitions
- Always run `dotnet test src/SecurityRule.Tests/` after completing any task — 0 failures required
- **Always run E2E tests after completing any task** — build, install deps, and run: `dotnet build src/SecurityRule.E2E.Tests/ && pwsh src/SecurityRule.E2E.Tests/bin/Debug/net10.0/playwright.ps1 install chromium --with-deps && dotnet test src/SecurityRule.E2E.Tests/ --no-build` — 0 failures required
- E2E tests CAN and MUST run in the agent sandbox — `pwsh` and Chromium are available; Playwright is auto-installed during build
- The PR description **must** list exactly which test files were added or modified

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

1. **Write or update tests FIRST** — they must fail (or be non-existent) before implementation; confirm this before proceeding
2. Implement the functionality
3. Run `dotnet test src/SecurityRule.Tests/` — ensure ALL tests pass
4. Run E2E tests: `dotnet build src/SecurityRule.E2E.Tests/ && pwsh src/SecurityRule.E2E.Tests/bin/Debug/net10.0/playwright.ps1 install chromium --with-deps && dotnet test src/SecurityRule.E2E.Tests/ --no-build` — ensure ALL E2E tests pass
5. Push the PR and verify CI passes — **both** the unit/integration job and the E2E job must be green
6. Refactor safely (tests must remain green)

> **For any Blazor UI change** (new page, new button, new route, new form field):
> write the `.feature` scenario + step definitions FIRST, then implement the UI.
> Run E2E tests locally (in the agent sandbox) to confirm they pass before pushing.
>
> ⛔ It is **never acceptable** to commit a UI change without the corresponding E2E scenario.

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
- Run E2E tests: `dotnet build src/SecurityRule.E2E.Tests/ && pwsh src/SecurityRule.E2E.Tests/bin/Debug/net10.0/playwright.ps1 install chromium --with-deps && dotnet test src/SecurityRule.E2E.Tests/ --no-build` and verify all pass

### Step 4 — Verify coverage

Before closing the task, confirm:
- Every new public method has at least one test
- Every deleted method's tests are removed
- No tests reference code that no longer exists
- All unit/integration tests pass (`dotnet test src/SecurityRule.Tests/` — 0 failures)
- All E2E tests pass (`dotnet test src/SecurityRule.E2E.Tests/ --no-build` — 0 failures)
- CI is green for **both** jobs: unit/integration tests **and** E2E tests (`.github/workflows/ci.yml`)

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
- [ ] **At least one test file was modified or created** (exception: purely internal refactoring with no behaviour change)
- [ ] `dotnet test src/SecurityRule.Tests/` passes with 0 failures
- [ ] For any UI change: at least one new E2E `.feature` scenario was added with the corresponding step definitions
- [ ] **CI is green** — both the unit/integration job and the E2E job in `.github/workflows/ci.yml` pass with 0 failures
- [ ] The PR description lists every test file that was added or modified
- [ ] Code follows Clean Architecture layer rules
- [ ] No build errors or critical warnings remain
- [ ] All new public functionality is covered by tests
- [ ] No orphaned tests reference deleted code

---

## MudBlazor & Razor Pages

### File structure

All Blazor pages live under `src/SecurityRule.Web/Components/Pages/<EntityName>/`.  
Each entity section contains the same set of files:

| File | Purpose |
|---|---|
| `Index.razor` | List page — loads data, shows `<Table>` component |
| `Table.razor` | Reusable `MudTable` component (no `@page` directive) |
| `Create.razor` | Create form page |
| `Edit.razor` | Edit / Delete form page |
| `Details.razor` | Read-only detail view; supports `Embedded="true"` for inline rendering inside Table |

Shared reusable components (inputs, chips) go in `Components/Shared/`.  
Modal dialogs go in `Components/Dialogs/` and inherit `MudDialog`.

### Required directives (every page)

```razor
@page "/entity-name"
@rendermode InteractiveServer
@inject IEntityRepository EntityRepository
@inject NavigationManager Navigation
```

`@rendermode InteractiveServer` is **mandatory** on every `@page` component — without it, event handlers will not fire.

Global usings (MudBlazor, Domain models, services, etc.) are declared once in `Components/_Imports.razor` — do **not** repeat them in individual files.

### Index page pattern

```razor
@page "/entities"
@rendermode InteractiveServer
@inject IEntityRepository EntityRepository
@inject NavigationManager Navigation

<PageTitle>Сущности</PageTitle>

<MudStack Row="true" Spacing="1">
    <MudText Typo="Typo.h4" GutterBottom="true">Сущности</MudText>
    <MudIconButton Icon="@Icons.Material.Filled.Add" Size="Size.Small" Color="Color.Default"
                   Title="Добавить"
                   OnClick="@(() => Navigation.NavigateTo("/entities/create"))" />
</MudStack>

@if (_loading)
{
    <MudProgressLinear Color="Color.Primary" Indeterminate="true" />
}
else
{
    <Table Items="_entities" />
}

@code {
    private IEnumerable<Entity> _entities = [];
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        _entities = await EntityRepository.GetAllAsync();
        _loading = false;
    }
}
```

### Table component pattern

- No `@page` directive
- `[Parameter] public IEnumerable<Entity> Items { get; set; } = [];`
- `[Parameter] public int Elevation { get; set; } = 1;`
- Use `MudTable` with `Hover="true" Striped="true" Dense="true"`
- Row click expands inline `<Details>` with `Embedded="true"` — Ctrl+click / middle-click opens in new tab
- Action buttons (`MudIconButton`) in the last column use `@onclick:stopPropagation="true"` to prevent row expansion
- Always include `data-testid` attributes on interactive elements for Playwright E2E tests

```razor
<MudTable T="Entity" Items="@Items" Hover="true" Striped="true" Dense="true"
          Elevation="@Elevation" OnRowClick="@ToggleRow">
    <HeaderContent>
        <MudTh>Название</MudTh>
        <MudTh></MudTh>
    </HeaderContent>
    <RowTemplate>
        <MudTd DataLabel="Название">@context.Name</MudTd>
        <MudTd Style="text-align:right">
            <span @onclick:stopPropagation="true">
                <MudIconButton Icon="@Icons.Material.Filled.Edit" Size="Size.Small"
                               OnClick="@(() => Navigation.NavigateTo($"/entities/edit/{context.Id}"))" />
            </span>
        </MudTd>
    </RowTemplate>
    <ChildRowContent Context="entity">
        @if (_expandedRows.Contains(entity.Id))
        {
            <MudTr>
                <td colspan="2" style="padding:0; background:var(--mud-palette-background-grey);">
                    <div style="border-left:3px solid var(--mud-palette-primary); margin:8px 16px 12px 36px; padding-left:16px;">
                        <Details Server="entity" Embedded="true" />
                    </div>
                </td>
            </MudTr>
        }
    </ChildRowContent>
</MudTable>
```

### Create / Edit form pattern

- Wrap fields in `<MudCard><MudCardContent>` / `<MudCardActions>`
- Add `Class="mb-3"` to every input for consistent spacing
- Show `ISnackbar` notifications after save/delete: `Snackbar.Add("Сохранено", Severity.Success)`
- Catch exceptions on Delete and show `Severity.Error`
- Navigate back with `NavigationManager` after successful save

```razor
@inject ISnackbar Snackbar

<MudCard>
    <MudCardContent>
        <MudTextField @bind-Value="_entity.Name" Label="Название" Required="true" Class="mb-3" />
    </MudCardContent>
    <MudCardActions>
        <MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="@Save">Сохранить</MudButton>
        <MudButton Variant="Variant.Text" OnClick="@(() => Navigation.NavigateTo("/entities"))">Отмена</MudButton>
        @* Edit only — Delete button *@
        <MudSpacer />
        <MudButton Variant="Variant.Filled" Color="Color.Error"
                   StartIcon="@Icons.Material.Filled.Delete" OnClick="@Delete">Удалить</MudButton>
    </MudCardActions>
</MudCard>
```

### Details page / component pattern

- Supports dual mode: standalone page (`@page`) and embedded in a table row (`Embedded="true"`)
- Use `MudBreadcrumbs` on standalone pages
- Use `MudGrid` + `MudItem` for field layout (xs/sm breakpoints)
- Label: `Typo.caption Color="Color.Secondary"`, value: `Typo.body1`

### Common MudBlazor components used in this project

| Component | Usage |
|---|---|
| `MudTextField` | Text, IP, description inputs |
| `MudAutocomplete` | OS selection, tag input (`CoerceText="true"`) |
| `MudSelect` + `MudSelectItem` | Multi-select of related entities (`MultiSelection="true"`) |
| `MudTable` | All data lists (`Hover`, `Striped`, `Dense`) |
| `MudCard` / `MudCardContent` / `MudCardActions` | Form containers |
| `MudGrid` / `MudItem` | Responsive detail layouts |
| `MudStack` | Horizontal/vertical flex layouts (`Row="true"`) |
| `MudChip` / `MudChipSet` | Tag display |
| `MudIconButton` | Icon actions in tables |
| `MudProgressLinear` | Loading indicator (`Color.Primary Indeterminate`) |
| `MudDialog` | Modal dialogs — use `[CascadingParameter] IMudDialogInstance MudDialog` |
| `MudBreadcrumbs` | Page navigation trail |
| `MudSnackbar` / `ISnackbar` | Toast notifications (Success / Error / Warning) |
| `MudNavLink` | Navigation menu items |
| `MudDivider` | Horizontal separator in nav / dialogs |
| `MudTooltip` | Hover hints on icon buttons |
| `MudSpacer` | Flex spacer in MudStack / MudAppBar |

### Icons

Always use `Icons.Material.Filled.*` — never use string literals.  
Common icons: `Dns` (server), `MiscellaneousServices` (service), `CompareArrows` (connections), `Security` (certificates), `AccountCircle` (user), `Group` (group), `Edit`, `Delete`, `Add`, `ContentCopy`, `OpenInNew`.

### Navigation

- Routes follow the pattern `/entity-name`, `/entity-name/create`, `/entity-name/edit/{Id:int}`, `/entity-name/{Id:int}`
- Route parameters use `[Parameter] public int Id { get; set; }`
- Query parameters use `[SupplyParameterFromQuery] public int? ParamName { get; set; }`
- After successful save → navigate back to the list or detail page

### NavMenu

Add new pages to `Components/Layout/NavMenu.razor` under the appropriate section (`ИНФРАСТРУКТУРА`, `БЕЗОПАСНОСТЬ`, `УЧЁТНЫЕ ЗАПИСИ`) using `<MudNavLink Href="..." Icon="...">`.

### Dialog pattern

```razor
@inject IDialogService DialogService

// Open a dialog:
var parameters = new DialogParameters<MyDialog> { { p => p.EntityId, id } };
await DialogService.ShowAsync<MyDialog>("Заголовок", parameters,
    new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true });
```

In the dialog component:
```razor
[CascadingParameter] IMudDialogInstance MudDialog { get; set; } = null!;
void Close() => MudDialog.Close();
```

---

## Additional Guidance

- Test names must describe the scenario, not the method name:
  - ✅ `Returns_Empty_List_When_No_Servers_Exist`
  - ❌ `Test_GetAll_001`
- Prefer simplicity — avoid over-engineering or premature abstractions
- Do not create files outside the standard project structure without a clear reason

---

_End of instructions_
