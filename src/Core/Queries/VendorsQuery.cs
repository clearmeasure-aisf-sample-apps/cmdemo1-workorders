using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Returns every vendor: active vendors first, then inactive ones, each group ordered by name ignoring case.
/// </summary>
public record VendorsQuery : IRequest<Vendor[]>, IRemotableRequest;
