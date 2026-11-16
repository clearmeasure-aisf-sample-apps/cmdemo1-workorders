using Mediator;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

/// <summary>
/// Applies an AI-reformatted title and description to an existing work order by number.
/// Truncation of the description is a <see cref="WorkOrder"/> behavior.
/// </summary>
public record ReformatWorkOrderCommand(string Number, string Title, string Description) : IRequest<WorkOrder?>;
