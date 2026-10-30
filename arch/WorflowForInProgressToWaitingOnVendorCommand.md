# InProgressToWaitingOnVendorCommand Sequence Diagram

```mermaid
sequenceDiagram
autonumber

actor user as "User"
participant ui as "Blazor WASM Client"
participant api as "SingleApiController (UI.Server)"
participant sBus as "Bus : IBus"
participant mediator as "IMediator"
participant cmdHandler as "StateCommandHandler"
participant cmd as "InProgressToWaitingOnVendorCommand"
participant cmdBase as "StateCommandBase"
participant order as "WorkOrder"
participant db as "DataContext (EF Core)"
participant dBus as "DistributedBus : IDistributedBus"

user->>ui: Wait on vendor work order
ui->>api: POST WebServiceMessage (InProgressToWaitingOnVendorCommand)
api->>api: Deserialize WebServiceMessage.GetBodyObject()
api->>sBus: Send(InProgressToWaitingOnVendorCommand)
sBus->>mediator: Send(InProgressToWaitingOnVendorCommand)
mediator->>cmdHandler: Handle(InProgressToWaitingOnVendorCommand, CancellationToken)
cmdHandler->>cmd: Execute(StateCommandContext { CurrentDateTime = UtcNow })
cmd->>order: WaitingOnVendorSince = CurrentDateTime
cmd->>cmdBase: base.Execute(context)
cmdBase->>order: ChangeStatus(CurrentUser, CurrentDateTime, InProgress)
cmdHandler->>db: Attach/Add or Update(order)
cmdHandler->>db: SaveChangesAsync()
db-->>cmdHandler: persisted
cmdHandler->>cmdHandler: Build debug message
cmdHandler->>dBus: PublishAsync(StateTransitionEvent)
Note right of dBus: StateTransitionEvent is null for this command
cmdHandler-->>mediator: StateCommandResult(order, "Wait on vendor", debugMessage)
mediator-->>sBus: StateCommandResult
sBus-->>api: StateCommandResult
api-->>ui: WebServiceMessage (serialized result)
```

Note: Begin status: **InProgress**. End status: **InProgress**. Allowed role: **Assignee only** (CurrentUser == WorkOrder.Assignee), and only while WorkOrder.WaitingOnVendorSince is null. The matching **Resume** command (WaitingOnVendorToInProgressCommand) clears WaitingOnVendorSince; leaving In Progress (Shelve, Complete, Cancel) also clears it.
