# In-process validation for create commands (#1824)

Follow-up from #1804 decision **(c)**: `CreateDatedWorkOrdersCommand`, `CreateVendorCommand`, and
`CreateTeamCommand` keep FluentValidation on the HTTP envelope only. This document records the
deliberate in-process design so students do not confuse the two paths.

## Decision

| Path | Mechanism | Failure signal |
| --- | --- | --- |
| **In-process** (Blazor Server / MCP / Worker via `IBus`) | Each command's `FindRuleViolations` (handler maps the list to a Failed result) | `Create*Result` with `Success`/`Succeeded` false and a message — **not** `ValidationException` |
| **HTTP** (single API / remoting envelope) | FluentValidation types listed in `HttpEnvelopeValidationDeferral` | HTTP **400** with the same `errors` payload as before #1824 |

`ValidationBehavior` stays pure: it runs every DI-registered `IValidator<T>`. The three create
validators are **not** registered as MediatR validators; `HttpEnvelopeValidationDeferral` is the
single catalog the host registries and HTTP resolver share.

## Command seams

| Command | In-process rules on the command | HTTP shape rules (envelope only) |
| --- | --- | --- |
| `CreateDatedWorkOrdersCommand` | `FindRuleViolations()` — empty DueDates; more than `MaximumBatchSize` (10) | `CreateDatedWorkOrdersCommandValidator` (usernames, title, description, DueDates) |
| `CreateVendorCommand` | `FindRuleViolations(existingVendorNames)` — duplicate name ignoring case | `CreateVendorCommandValidator` (required name, lengths, email, phone) |
| `CreateTeamCommand` | `FindRuleViolations(existingTeamNames, knownUserNames)` — duplicate name; unknown members | `CreateTeamCommandValidator` (required name, lengths, member list) |

Handlers call `FindRuleViolations` before writing. MCP tools already branch on `Succeeded` /
`Success` and only catch `ValidationException` as a safety net for other commands.

## What must not change

- HTTP 400 responses and FluentValidation messages for the three commands
- `ValidationBehavior` (no type exception list, no opt-out marker)
- Registration of the three validators only through `HttpEnvelopeValidatorSet` / envelope resolution

## Characterization tests

`src/UnitTests/Core/Validation/InProcessCreateCommandValidationTests.cs` locks the in-process
Failed-result contract and that blank vendor/team names still reach the handler when the HTTP
FluentValidators are not registered for MediatR. The overview pointer lives in
[request-flow.md](../request-flow.md#validation).
