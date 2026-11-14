using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.LlmGateway;

/// <summary>Echoes tool results so the function-invocation loop can finish.</summary>
internal static class StubChatFunctionResults
{
    internal static bool TryEcho(IReadOnlyList<ChatMessage> messages, out ChatResponse response)
    {
        var results = messages
            .SelectMany(m => m.Contents.OfType<FunctionResultContent>())
            .Select(r => r.Result?.ToString())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();

        if (results.Count == 0)
        {
            response = null!;
            return false;
        }

        response = new ChatResponse([new ChatMessage(ChatRole.Assistant, string.Join('\n', results))]);
        return true;
    }
}
