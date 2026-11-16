# In-process validation for create commands (#1824)

Follow-up from #1804 decision **(c)**: `CreateDatedWorkOrdersCommand`, `CreateVendorCommand`, and
`CreateTeamCommand` keep FluentValidation **off** MediatR `ValidationBehavior`. This document records
the deliberate in-process design.

## Decision

| Path | Mechanism | Failure signal |
| --- | --- | --- |
| **In-process** (Blazor Server / MCP / Worker via `IBus`) | Each command's `FindRuleViolations` (handlers map the list to a Failed result) | `Create*Result` with `Success`/`Succeeded` false and a message — **not** `ValidationException` |
| **HTTP** (single API / remoting envelope) | FluentValidation types listed in `HttpEnvelopeValidationDeferral` | HTTP **400** with the same `errors` payload as before #1824 |

`ValidationBehavior` stays pure: it runs every DI-registered `IValidator<T>`. The three create
validators are **not** registered as MediatR validators; `HttpEnvelopeValidationDeferral` is the
single catalog the host registries and HTTP resolver share.

## What `FindRuleViolations` runs

| Command | In-process rules | Same messages as HTTP shape? |
| --- | --- | --- |
| `CreateDatedWorkOrdersCommand` | DueDates empty / over `MaximumBatchSize` (10) with master's Failed messages; then other shape rules via `InProcessCreateCommandShapeValidation` → `CreateDatedWorkOrdersCommandValidator` | DueDates batch messages stay master's in-process wording; username/title/description match the envelope validator |
| `CreateVendorCommand` | Shape via `CreateVendorCommandValidator`, then duplicate name ignoring case | Yes for shape (validator error messages) |
| `CreateTeamCommand` | Shape via `CreateTeamCommandValidator`, then duplicate name and unknown members | Yes for shape (validator error messages) |

`InProcessCreateCommandShapeValidation` is the **named exception** to the #1804 rule that production
code must not construct FluentValidation validators outside `ValidationBehavior`. It builds the
deferred validator with `Activator.CreateInstance` (not MediatR DI). The matching HTTP site is
`HttpEnvelopeValidatorSet` (`ActivatorUtilities`). `ValidationPipelineArchitectureTests` allow-lists
only those two files for Activator construction and still forbids `new AbstractValidator` elsewhere.

Handlers call `FindRuleViolations` before writing. MCP tools branch on `Succeeded` / `Success`.

## What must not change

- HTTP 400 responses and FluentValidation property/error payloads for the three commands
- `ValidationBehavior` (no type exception list, no opt-out marker)
- Registration of the three validators only through `HttpEnvelopeValidatorSet` / envelope resolution
  (not as MediatR `IValidator<T>`)
- No additional production Activator sites for validators beyond the two allow-listed files

## Tests

- Unit: `InProcessCreateCommandValidationTests`, `InProcessCreateCommandShapeValidationTests`
- Integration (real handlers): blank-name Failed results on `VendorQueryHandler` / `TeamQueryHandler`;
  DueDates empty / over-ten on `CreateDatedWorkOrdersHandler` via `IBus`
