using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.LlmGateway;

/// <summary>Deterministic title/description cleanup for the auto-reformat agent.</summary>
internal static class StubChatReformat
{
    internal static bool TryReformat(IReadOnlyList<ChatMessage> messages, out ChatResponse response)
    {
        var system = StubChatMessageText.CombinedSystemText(messages);
        if (!system.Contains("reformatting work order fields", StringComparison.OrdinalIgnoreCase))
        {
            response = null!;
            return false;
        }

        var user = StubChatMessageText.LastUserText(messages);
        var title = CapitalizeFirst(ReadField(user, "Title:") ?? "Work order");
        var description = FixDescription(ReadField(user, "Description:") ?? string.Empty);
        response = new ChatResponse([
            new ChatMessage(ChatRole.Assistant, $"TITLE: {title}\nDESCRIPTION: {description}")
        ]);
        return true;
    }

    private static string? ReadField(string userText, string prefix)
    {
        foreach (var line in userText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return line[prefix.Length..].Trim();
            }
        }

        return null;
    }

    private static string CapitalizeFirst(string text) =>
        text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];

    private static string FixDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return "Needs attention.";
        }

        var text = CapitalizeFirst(description.Trim()
            .Replace("grammer", "grammar", StringComparison.OrdinalIgnoreCase));
        return EndsWithSentencePunctuation(text) ? text : text + ".";
    }

    private static bool EndsWithSentencePunctuation(string text) =>
        text.EndsWith('.') || text.EndsWith('!') || text.EndsWith('?');
}
