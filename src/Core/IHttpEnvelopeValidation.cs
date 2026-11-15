namespace ClearMeasure.Bootcamp.Core;

/// <summary>
/// Marks a remotable request whose FluentValidation rules run on the HTTP single-API
/// envelope only. The MediatR validation pipeline skips these in-process so MCP / Blazor
/// Server callers keep today's handler results (#1804).
/// </summary>
public interface IHttpEnvelopeValidation;
