using Microsoft.AspNetCore.Http.Metadata;

namespace ClearMeasure.Bootcamp.UI.Api.Endpoints;

/// <summary>Endpoint metadata capping the request body at <see cref="MaxRequestBodySize"/> bytes.</summary>
public sealed record RequestSizeLimit(long? MaxRequestBodySize) : IRequestSizeLimitMetadata;
