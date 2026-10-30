# WaitingOnVendorToCancelledCommand Sequence Diagram

```mermaid
sequenceDiagram
autonumber

actor user as "User"
participant ui as "Blazor WASM Client"
participant api as "SingleApiController (UI.Server)"
participant sBus as "Bus : IBus"
participant mediator as "IMediator"
participant cmdHandler as "StateCommandHandler"
participant cmd as "WaitingOnVendorToCancelledCommand"
participant cmdBase as "StateCommandBase"
participant order as "WorkOrder"
participant db as "DataContext (EF Core)"
participant dBus as "DistributedBus : IDistributedBus"

user->>ui: Cancel work order waiting on vendor
ui->>api: POST WebServiceMessage (WaitingOnVendorToCancelledCommand)
api->>api: Deserialize WebServiceMessage.GetBodyObject()
api->>sBus: Send(WaitingOnVendorToCancelledCommand)
sBus->>mediator: Send(WaitingOnVendorToCancelledCommand)
mediator->>cmdHandler: Handle(WaitingOnVendorToCancelledCommand, CancellationToken)
cmdHandler->>cmd: Execute(StateCommandContext { CurrentDateTime = UtcNow })
cmd->>order: AssignedDate = null
cmd->>order: Assignee = null
cmd->>cmdBase: base.Execute(context)
cmdBase->>order: ChangeStatus(CurrentUser, CurrentDateTime, Cancelled)
order->>order: Status = Cancelled, WaitingOnVendorSince = null
cmdHandler->>db: Load existing order, copy values, resolve relationships
cmdHandler->>db: Add WorkOrderStatusChange (InProgress to Cancelled)
cmdHandler->>db: SaveChangesAsync(CancellationToken)
db-->>cmdHandler: persisted
cmdHandler->>cmdHandler: Build debug message
cmdHandler->>dBus: PublishAsync(StateTransitionEvent, CancellationToken)
Note right of dBus: StateTransitionEvent is null for this command
cmdHandler-->>mediator: StateCommandResult(order, "Cancel", debugMessage)
mediator-->>sBus: StateCommandResult
sBus-->>api: StateCommandResult
api-->>ui: WebServiceMessage (serialized result)
```

Note: Begin status: **InProgress** (WaitingOnVendor substate). End status: **Cancelled**. Allowed role: **Creator only** (CurrentUser == WorkOrder.Creator). Guard: **WorkOrder.WaitingOnVendorSince != null**, in addition to the begin status and creator checks in StateCommandBase.IsValid(). Removes assignment (AssignedDate and Assignee) when canceling; WorkOrder.ChangeStatus also clears WaitingOnVendorSince when leaving InProgress.
