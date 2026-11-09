// ReSharper disable NotAccessedPositionalProperty.Global -- NServiceBus message contract; handlers bind the positional members
// ReSharper disable NotAccessedPositionalProperty.Local -- NServiceBus message contract; handlers bind the positional members
namespace ClearMeasure.Bootcamp.Core.Model.Events;

public record WorkOrderAssignedToBotEvent(string WorkOrderNumber, Guid BotUserId) : IStateTransitionEvent;