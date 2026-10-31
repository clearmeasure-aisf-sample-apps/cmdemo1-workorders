# AssignedToCancelledCommand Sequence Diagram

```mermaid
sequenceDiagram
autonumber

actor user as "User"
participant ui as "Blazor WASM Client"
participant api as "SingleApiController (UI.Server)"
participant sBus as "Bus : IBus"
participant mediator as "IMediator"
participant cmdHandler as "StateCommandHandler"
participant cmd as "AssignedToCancelledCommand"
participant cmdBase as "StateCommandBase"
participant order as "WorkOrder"
participant db as "DataContext (EF Core)"
participant dBus as "DistributedBus : IDistributedBus"

user->>ui: Cancel work order
ui->>api: POST WebServiceMessage (AssignedToCancelledCommand)
api->>api: Deserialize WebServiceMessage.GetBodyObject()
api->>sBus: Send(AssignedToCancelledCommand)
sBus->>mediator: Send(AssignedToCancelledCommand)
mediator->>cmdHandler: Handle(AssignedToCancelledCommand, CancellationToken)
cmdHandler->>cmd: Execute(StateCommandContext { CurrentDateTime = UtcNow })
cmd->>order: AssignedDate = null
cmd->>order: Assignee = null
cmd->>cmdBase: base.Execute(context)
cmdBase->>order: ChangeStatus(CurrentUser, CurrentDateTime, Cancelled)
cmdHandler->>db: Attach/Add or Update(order)
cmdHandler->>db: SaveChangesAsync()
db-->>cmdHandler: persisted
cmdHandler->>cmdHandler: Build debug message
cmdHandler->>dBus: PublishAsync(StateTransitionEvent)
Note right of dBus: StateTransitionEvent is null for this command
cmdHandler-->>mediator: StateCommandResult(order, "Cancelled", debugMessage)
mediator-->>sBus: StateCommandResult
sBus-->>api: StateCommandResult
api-->>ui: WebServiceMessage (serialized result)
```

Note: Begin status: **Assigned**. End status: **Cancelled**. Allowed role: **Creator only** (CurrentUser == WorkOrder.Creator).
