using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>The Work Order Search views saved by one user, ordered by name.</summary>
public record SavedSearchViewsQuery(string UserName) : IRequest<SavedSearchView[]>, IRemotableRequest;
