using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.LlmGateway;

/// <summary>Reads plain text from chat history for the stub planner.</summary>
internal static class StubChatMessageText
{
    internal static string LastUserText(IReadOnlyList<ChatMessage> messages) =>
        messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text ?? string.Empty;

    internal static string CombinedSystemText(IReadOnlyList<ChatMessage> messages) =>
        string.Join('\n', messages.Where(m => m.Role == ChatRole.System).Select(m => m.Text));
}
