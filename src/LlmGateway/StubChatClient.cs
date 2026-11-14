using System.ClientModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.LlmGateway;

/// <summary>
/// Deterministic <see cref="IChatClient"/> for pool-slot environments.
/// Selected only when <see cref="ChatClientConfig.UseStubChatClient"/> is true; never for tdd/uat/prod.
/// </summary>
public sealed class StubChatClient : IChatClient
{
    /// <summary>User-prompt substring that makes the stub refuse like a provider content filter.</summary>
    public const string RefusalPromptMarker = "triggers a provider refusal";

    /// <inheritdoc />
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var list = messages as IReadOnlyList<ChatMessage> ?? messages.ToList();
        ThrowIfRefusalRequested(list);

        if (StubChatFunctionResults.TryEcho(list, out var echoed))
        {
            return Task.FromResult(echoed);
        }

        if (StubChatToolCallPlanner.TryPlan(list, out var toolCall))
        {
            return Task.FromResult(toolCall);
        }

        if (StubChatReformat.TryReformat(list, out var reformatted))
        {
            return Task.FromResult(reformatted);
        }

        return Task.FromResult(new ChatResponse([new ChatMessage(ChatRole.Assistant, "OK")]));
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await GetResponseAsync(messages, options, cancellationToken);
        yield return new ChatResponseUpdate(ChatRole.Assistant, response.Text);
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    /// <inheritdoc />
    public void Dispose()
    {
    }

    private static void ThrowIfRefusalRequested(IReadOnlyList<ChatMessage> messages)
    {
        var userText = StubChatMessageText.LastUserText(messages);
        if (userText.Contains(RefusalPromptMarker, StringComparison.OrdinalIgnoreCase))
        {
            throw new ClientResultException("content_filter");
        }
    }
}
