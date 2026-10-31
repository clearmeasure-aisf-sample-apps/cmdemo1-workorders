using MediatR;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

/// <summary>Deletes one of the user's saved Work Order Search views.</summary>
public record DeleteSearchViewCommand(string UserName, Guid Id)
    : IRequest<SavedSearchViewResult>, IRemotableRequest;
