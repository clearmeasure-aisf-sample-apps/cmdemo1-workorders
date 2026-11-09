using System.Diagnostics;
using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.LlmGateway;

/// <summary>
/// The tracing span of one chat call: started under the current activity with the provider and the last user prompt,
/// then completed with the model and response text, or failed with the exception.
/// </summary>
internal sealed class ChatActivity : IDisposable
{
    internal const string SourceName = "ChurchBulletin.LlmGateway";

    private static readonly ActivitySource ActivitySource = new(SourceName, "1.0.0");

    private readonly Activity? _activity;

    private ChatActivity(Activity? activity) => _activity = activity;

    public static ChatActivity Start(string operationName, IEnumerable<ChatMessage> messages)
    {
        var parentContext = Activity.Current?.Context;
        var activity = parentContext.HasValue
            ? ActivitySource.StartActivity(operationName, ActivityKind.Internal, parentContext.Value)
            : ActivitySource.StartActivity(operationName);
        activity?.SetTag("chat.provider", "OpenAI");
        activity?.SetTag("chat.prompt", messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text);
        activity?.AddEvent(new ActivityEvent("request.sent"));
        return new ChatActivity(activity);
    }

    public void Completed(string? modelId, string? responseText)
    {
        _activity?.AddEvent(new ActivityEvent("response.received"));
        _activity?.SetTag("chat.model", modelId);
        _activity?.SetTag("chat.response", responseText);
    }

    public void Failed(Exception exception)
    {
        _activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
        _activity?.AddEvent(new ActivityEvent("exception",
            tags: new ActivityTagsCollection
            {
                { "exception.type", exception.GetType().FullName },
                { "exception.message", exception.Message }
            }));
    }

    public void Dispose() => _activity?.Dispose();
}
