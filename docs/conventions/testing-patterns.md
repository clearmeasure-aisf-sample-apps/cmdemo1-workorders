# Testing patterns: bUnit, Blazor pages, StubBus, EF Core, acceptance and LLM tests

Read when writing or fixing bUnit component/page tests, adding an `[Inject]` or `IBus.Send` to a page, writing EF
Core handlers or their tests, writing acceptance or live-LLM tests. Moved from `AGENTS.md`, `CLAUDE.md` and
`.github/copilot-instructions.md`. The always-on test rules stay in [AGENTS.md](../../AGENTS.md); see also
[testing-conventions.md](../testing-conventions.md) and [testing-cheat-sheet.md](testing-cheat-sheet.md).

## Frameworks

- NUnit 4.x + Shouldly assertions (NOT FluentAssertions, NOT `Assert.That`); avoid mocking libraries.
- Test data generation: AutoBogus. UI component tests: bUnit. Acceptance tests: Playwright.
- Test categories: unit (single unit in isolation, fast, no infrastructure); integration (component integration, may
  use the real database, run in CI); UI (bUnit for Blazor components).
- Examples of names: `GetWorkOrder_WithValidId_ReturnsWorkOrder`,
  `SaveChurchBulletin_WithMissingTitle_ThrowsValidationException`.
- New or updated behavior needs unit tests, integration tests, and acceptance tests (Playwright; may be skipped when
  the UX is unchanged).

## Live-LLM tests

Mark with `[LlmTest]` (from `IntegrationTests/TestSupport`) instead of `[Retry(n)]`. The test gets 3 attempts; if none
pass it is reported as a **warning**, not a build failure. Rate-limit `Assert.Ignore` and `Inconclusive` results
pass through unchanged.

## Acceptance tests

Use helpers from `AcceptanceTestBase`: `LoginAsCurrentUser()`, `CreateAndSaveNewWorkOrder()`, `Click(testId)`,
`Input(testId, value)`, `Select(testId, value)`, `Expect(locator)`.

```csharp
[Test]
public async Task TestNameFromIssue()
{
    await LoginAsCurrentUser();
    await Click("...");
}
```

Run: `pwsh src/AcceptanceTests/bin/Debug/net10.0/playwright.ps1 install` (first time), then
`dotnet test src/AcceptanceTests --configuration Debug --filter "FullyQualifiedName~TestClassName"`. CI runs 6 shards;
see [running-acceptance-shards.md](../running-acceptance-shards.md).

## bUnit patterns (WorkOrderSearch and any `AppComponentBase` component)

- Adding an `[Inject]` property means **every** bUnit test of that component must register the service, or it
  throws at render time. For `AuthenticationStateProvider`, use `CustomAuthenticationStateProvider` with a
  `StubUserSessionStore`:

  ```csharp
  var store = new StubUserSessionStore { Username = "someuser" };
  var authProvider = new CustomAuthenticationStateProvider(store);
  authProvider.Login("someuser").GetAwaiter().GetResult();
  ctx.Services.AddSingleton<AuthenticationStateProvider>(authProvider);
  ```

- Prefer a `CreateContext()` factory helper over repeating registrations; pass an `IBus` override when needed:

  ```csharp
  private static BunitContext CreateContext(IBus? bus = null, string loggedInAs = "defaultuser")
  {
      var ctx = new BunitContext();
      ctx.Services.AddSingleton(bus ?? new StubBus());
      ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
      ctx.Services.AddSingleton(TimeProvider.System);
      // ... add AuthenticationStateProvider
      return ctx;
  }
  ```

- Checkbox state: assert with `element.HasAttribute("checked")`; `IElement` has no `IsChecked()`.
- `@bind-Value:after="HandlerMethod"` on an `<InputCheckbox>` runs async side-effects after binding; the handler sees
  the new value.
- `AuthorizeView` needs `IAuthorizationPolicyProvider` and `Task<AuthenticationState>`: use `ctx.AddAuthorization()`
  (bUnit helper), not `ctx.Services.AddAuthorization()`.

## Blazor page code-behind

When adding logic to a `.razor` page that has only an `@code` block:

- Create a `.razor.cs` partial class alongside it (e.g. `Index.razor.cs`) declaring
  `public partial class PageName : AppComponentBase, IListener<Event>`.
- Do NOT add `@inherits AppComponentBase` to the `.razor` file: `src/UI.Shared/_Imports.razor` sets it globally.
- Do NOT add `[Route(...)]` to the `.razor.cs` when `@page` already defines the route.
- Keep complex component logic in code-behind; minimize JavaScript interop.

## StubBus maintenance

`src/UnitTests/UI.Shared/Pages/StubBus.cs` is the shared stub for all bUnit page tests. Whenever a page sends a new
`IRequest<TResponse>` (for example in `OnInitializedAsync`), add a matching `if (request is NewQueryType) { ... }`
branch before the `throw new NotImplementedException()`, or existing page tests fail.

## EF Core handler gotchas

- `GroupBy` on value-converter properties (like `wo.Status.Key`) cannot be translated to SQL: `.Select(wo => wo.Status).ToListAsync()` and group in memory.
- SQLite in-memory tests: `Data Source=:memory:` is a new database per connection; the `EnsureCreated()` schema is
  lost when the context is disposed. Use a temp-file database (`Path.GetTempPath() + Guid.NewGuid() + ".db"`) in
  `[SetUp]` and delete it in `[TearDown]`.
- `IDatabaseConfiguration` has two members, `GetConnectionString()` and `ResetConnectionPool()`: implement both in
  any stub.
- `WorkOrderStatusConverter` stores `Code` (e.g. `DRT`), not `Key` (e.g. `Draft`); `WorkOrderStatus.FromCode(code)`
  converts back.
- Create a mapping file for every entity in `src/DataAccess/Mappings/`.
