# Command

A **Command** is a write request in this application's CQRS flow. [`StateCommandBase`](../../src/Core/Model/StateCommands/StateCommandBase.cs) in `src/Core/Model/StateCommands` implements [`IStateCommand`](../../src/Core/Services/IStateCommand.cs) and is sent with `IBus.Send`; MediatR dispatches it to [`StateCommandHandler`](../../src/DataAccess/Handlers/StateCommandHandler.cs) in `DataAccess`. The path is described in [CQRS request flow](../conventions/cqrs-request-flow.md#commands-and-local-dispatch).
