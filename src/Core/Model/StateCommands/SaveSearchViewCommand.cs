using MediatR;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

/// <summary>Saves the current Work Order Search query string as a named view of the user.</summary>
public record SaveSearchViewCommand(string UserName, string Name, string Query)
    : IRequest<SavedSearchViewResult>, IRemotableRequest;
