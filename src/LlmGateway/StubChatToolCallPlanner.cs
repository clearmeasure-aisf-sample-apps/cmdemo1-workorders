using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.LlmGateway;

/// <summary>Plans MCP tool calls from natural-language prompts used by AI agent acceptance tests.</summary>
internal static partial class StubChatToolCallPlanner
{
    internal static bool TryPlan(IReadOnlyList<ChatMessage> messages, out ChatResponse response)
    {
        var userText = StubChatMessageText.LastUserText(messages);
        if (!LooksLikeSaturdaySchedule(userText))
        {
            response = null!;
            return false;
        }

        response = BuildDatedWorkOrdersCall(messages, userText);
        return true;
    }

    private static bool LooksLikeSaturdaySchedule(string userText) =>
        userText.Contains("Saturday", StringComparison.OrdinalIgnoreCase);

    private static ChatResponse BuildDatedWorkOrdersCall(IReadOnlyList<ChatMessage> messages, string userText)
    {
        var creator = ResolveCreator(messages);
        var assignee = ResolveAssignee(userText);
        var title = ResolveTitle(userText);
        var call = new FunctionCallContent(
            Guid.NewGuid().ToString("N"),
            "create-dated-work-orders",
            new Dictionary<string, object?>
            {
                ["creatorUsername"] = creator,
                ["assigneeUsername"] = assignee,
                ["title"] = title,
                ["description"] = "Mow the grass",
                ["saturdayCount"] = 10
            });

        return new ChatResponse([new ChatMessage(ChatRole.Assistant, [call])]);
    }

    private static string ResolveCreator(IReadOnlyList<ChatMessage> messages)
    {
        var match = LoggedInUsername().Match(StubChatMessageText.CombinedSystemText(messages));
        return match.Success ? match.Groups[1].Value : "tlovejoy";
    }

    private static string ResolveAssignee(string userText)
    {
        var match = AssigneeUsername().Match(userText);
        return match.Success ? match.Groups[1].Value : "gwillie";
    }

    private static string ResolveTitle(string userText)
    {
        var match = TitleTag().Match(userText);
        return match.Success ? $"Mow the grass [{match.Groups[1].Value}]" : "Mow the grass";
    }

    [GeneratedRegex(@"Currently logged in user is (\S+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LoggedInUsername();

    [GeneratedRegex(@"\bfor\s+(\S+)\s+to\s+mow\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AssigneeUsername();

    [GeneratedRegex(@"\[([^\]]+)\]", RegexOptions.CultureInvariant)]
    private static partial Regex TitleTag();
}
