using ClearMeasure.Bootcamp.Core;
using Mediator;

namespace ClearMeasure.Bootcamp.LlmGateway;

/// <summary>
/// Server-only: reads chat client configuration (including secrets). Not an
/// <c>IRemotableRequest</c> — must not cross the Blazor WASM single API (#1804).
/// </summary>
public record ChatClientConfigQuery : IRequest<ChatClientConfig>, IBusMessage;
