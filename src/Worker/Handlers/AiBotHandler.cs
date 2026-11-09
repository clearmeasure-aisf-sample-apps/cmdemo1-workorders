using ClearMeasure.Bootcamp.Core.Model.Events;

namespace Worker.Handlers;

// EventHandler starts the AI bot saga for this event. This handler only completes the message.
public class AiBotHandler : IHandleMessages<WorkOrderAssignedToBotEvent>
{
    public Task Handle(WorkOrderAssignedToBotEvent message, IMessageHandlerContext context)
    {
        return Task.CompletedTask;
    }
}
