# WaitingOnVendorToInProgressCommand Sequence Diagram

```mermaid
sequenceDiagram
autonumber

actor user as "User"
participant ui as "Blazor WASM Client"
participant api as "SingleApiController (UI.Server)"
participant sBus as "Bus : IBus"
participant mediator as "IMediator"
participant cmdHandler as "StateCommandHandler"
participant cmd as "WaitingOnVendorToInProgressCommand"
participant cmdBase as "StateCommandBase"
participant order as "WorkOrder"
participant db as "DataContext (EF Core)"
participant dBus as "DistributedBus : IDistributedBus"

user->>ui: Resume work order
ui->>api: POST WebServiceMessage (WaitingOnVendorToInProgressCommand)
api->>api: Deserialize WebServiceMessage.GetBodyObject()
api->>sBus: Send(WaitingOnVendorToInProgressCommand)
sBus->>mediator: Send(WaitingOnVendorToInProgressCommand)
mediator->>cmdHandler: Handle(WaitingOnVendorToInProgressCommand, CancellationToken)
cmdHandler->>cmd: Execute(StateCommandContext { CurrentDateTime = UtcNow })
cmd->>order: WaitingOnVendorSince = null
cmd->>cmdBase: base.Execute(context)
cmdBase->>order: ChangeStatus(CurrentUser, CurrentDateTime, InProgress)
cmdHandler->>db: Attach/Add or Update(order)
cmdHandler->>db: SaveChangesAsync()
db-->>cmdHandler: persisted
cmdHandler->>cmdHandler: Build debug message
cmdHandler->>dBus: PublishAsync(StateTransitionEvent)
Note right of dBus: StateTransitionEvent is null for this command
cmdHandler-->>mediator: StateCommandResult(order, "Resume", debugMessage)
mediator-->>sBus: StateCommandResult
sBus-->>api: StateCommandResult
api-->>ui: WebServiceMessage (serialized result)
```

Note: Begin status: **InProgress**. End status: **InProgress**. Allowed role: **Assignee only** (CurrentUser == WorkOrder.Assignee), and only when WorkOrder.WaitingOnVendorSince != null. This is the resume/unpause command paired with InProgressToWaitingOnVendorCommand.
