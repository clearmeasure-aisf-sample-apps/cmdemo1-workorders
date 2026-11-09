using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.LlmGateway;

/// <summary>
/// A delegating chat client that adds distributed tracing spans (<see cref="ChatActivity"/>) around chat operations.
/// </summary>
public class TracingChatClient(IChatClient innerClient) : DelegatingChatClient(innerClient)
{
    /// <inheritdoc />
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var messageList = AsMaterializedList(messages);
        using var activity = ChatActivity.Start("ChatClient.GetResponseAsync", messageList);
        try
        {
            var response = await base.GetResponseAsync(messageList, options, cancellationToken);
            activity.Completed(response.ModelId, response.Text);
            return response;
        }
        catch (Exception ex)
        {
            activity.Failed(ex);
            throw;
        }
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messageList = AsMaterializedList(messages);
        using var activity = ChatActivity.Start("ChatClient.GetStreamingResponseAsync", messageList);
        ChatResponseUpdate? lastUpdate = null;
        var responseText = new StringBuilder();
        await using var updates = base
            .GetStreamingResponseAsync(messageList, options, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);

        while (await MoveNextAsync(updates, activity))
        {
            lastUpdate = updates.Current;
            if (!string.IsNullOrWhiteSpace(lastUpdate.Text))
            {
                responseText.Append(lastUpdate.Text);
                yield return lastUpdate;
            }
        }

        activity.Completed(lastUpdate?.ModelId, responseText.ToString());
    }

    /// <summary>The next update; a failure of the inner stream is recorded on the span (a yield cannot sit in a try).</summary>
    private static async Task<bool> MoveNextAsync(IAsyncEnumerator<ChatResponseUpdate> updates, ChatActivity activity)
    {
        try
        {
            return await updates.MoveNextAsync();
        }
        catch (Exception ex)
        {
            activity.Failed(ex);
            throw;
        }
    }

    /// <summary>
    /// Materializes the caller-supplied conversation history exactly once. The message list is a small,
    /// finite, in-memory sequence (unlike the streaming response below, which must never be buffered),
    /// so it is safe — and necessary — to snapshot it here to avoid re-enumerating a deferred/one-shot
    /// source (e.g. a LINQ query or an already-consumed iterator) once for tracing and again for the
    /// actual chat call.
    /// </summary>
    private static IReadOnlyList<ChatMessage> AsMaterializedList(IEnumerable<ChatMessage> messages)
    {
        return messages as IReadOnlyList<ChatMessage> ?? messages.ToList();
    }
}
