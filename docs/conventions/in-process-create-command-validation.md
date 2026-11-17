# In-process validation for create commands (#1824)

Follow-up from #1804 decision **(c)**: `CreateDatedWorkOrdersCommand`, `CreateVendorCommand`, and
`CreateTeamCommand` keep FluentValidation **off** MediatR `ValidationBehavior`. This document records
the deliberate in-process design (owner choice **(b)**: dependency injection into the handlers).

## Decision

| Path | Mechanism | Failure signal |
| --- | --- | --- |
| **In-process** (Blazor Server / MCP / Worker via `IBus`) | Handler injects a create-command **shape-rules** type (not `IValidator<T>`), runs shape rules, then `FindRuleViolations` for stored-data checks; maps messages to a Failed result | `Create*Result` with `Success`/`Succeeded` false and a message — **not** `ValidationException` |
| **HTTP** (single API / remoting envelope) | FluentValidation types listed in `HttpEnvelopeValidationDeferral`, resolved by `HttpEnvelopeValidatorSet` | HTTP **400** with the same `errors` payload as before #1824 |

`ValidationBehavior` stays pure: it runs every DI-registered `IValidator<T>`. The three create
FluentValidation classes are **not** registered as DI services. Lamar treats a registered
`AbstractValidator<T>` as `IValidator<T>`, which would make `ValidationBehavior` throw instead of
returning Failed results. Instead, UI and MCP registries build each deferred validator with
`ActivatorUtilities` (same as `HttpEnvelopeValidatorSet`) and register
`CreateVendorCommandShapeRules`, `CreateTeamCommandShapeRules`, and
`CreateDatedWorkOrdersCommandShapeRules` — plain types handlers inject. `HttpEnvelopeValidationDeferral`
remains the catalog for the deferral filter and HTTP envelope set.

## What runs in-process

| Command | Shape rules | Stored-data / batch rules (`FindRuleViolations`) | Same messages as HTTP shape? |
| --- | --- | --- | --- |
| `CreateDatedWorkOrdersCommand` | Injected `CreateDatedWorkOrdersCommandShapeRules` | DueDates empty / over `MaximumBatchSize` (10) with master's Failed messages | DueDates batch messages stay master's in-process wording; username/title/description match the envelope validator |
| `CreateVendorCommand` | Injected `CreateVendorCommandShapeRules` | Duplicate name ignoring case | Yes for shape (validator error messages) |
| `CreateTeamCommand` | Injected `CreateTeamCommandShapeRules` | Duplicate name and unknown members | Yes for shape (validator error messages) |

Handlers run shape validation before writing. MCP tools branch on `Succeeded` / `Success`.
`ShouldNotNewAbstractValidatorsOutsideValidationBehavior` stays exactly as on master: production
code does not `new` FluentValidation validators outside `ValidationBehavior`. HTTP construction
remains on `HttpEnvelopeValidatorSet` (`ActivatorUtilities`). There is no
`InProcessCreateCommandShapeValidation` type and no architecture-gate Activator allow-list.

## What must not change

- HTTP 400 responses and FluentValidation property/error payloads for the three commands
- `ValidationBehavior` (no type exception list, no opt-out marker)
- Deferred FluentValidation types stay off MediatR `IValidator<T>` registration
- The no-`new`-validators architecture gate (no exemptions for #1824)

## Tests

- Unit: `InProcessCreateCommandValidationTests` (pure `FindRuleViolations_*` for stored-data / batch);
  `Create*CommandShapeRulesTests`
- Integration (real handlers / `IBus`): blank name, over-long fields, bad email/phone, blank title;
  Failed results and nothing saved
