using ClearMeasure.Bootcamp.Core.Model;
using MediatR;
using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.LlmGateway;

/// <summary>
/// Server-only work-order chat. Not an <c>IRemotableRequest</c> — must not cross the
/// Blazor WASM single API (#1804). In-process bus sends on the server still work.
/// </summary>
public record WorkOrderChatQuery(string Prompt, WorkOrder CurrentWorkOrder) : IRequest<ChatResponse>;
