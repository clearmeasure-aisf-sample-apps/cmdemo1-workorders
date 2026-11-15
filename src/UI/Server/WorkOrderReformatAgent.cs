using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.LlmGateway;
using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Asks the chat model to reformat a work order's title and description, then parses the answer.
/// Persistence is the caller's responsibility (via <c>ReformatWorkOrderCommand</c>).
/// </summary>
public class WorkOrderReformatAgent(
    ChatClientFactory chatClientFactory,
    ILogger<WorkOrderReformatAgent> logger)
{
    /// <summary>Reformatted title and description when the model proposed a change.</summary>
    public sealed record ReformatResult(string Title, string Description);

    /// <summary>
    /// Returns a result when the model proposes a change; <c>null</c> for <c>NO_CHANGES</c>
    /// or an answer that matches the current fields. Exceptions propagate to the caller.
    /// </summary>
    public virtual async Task<ReformatResult?> ReformatWorkOrderAsync(WorkOrder workOrder)
    {
        var responseText = await RequestReformatTextAsync(workOrder);
        if (IsNoChangeResponse(responseText))
        {
            logger.LogInformation("No reformatting needed for WorkOrder {WorkOrderNumber}", workOrder.Number);
            return null;
        }

        var result = ParseResponse(responseText!, workOrder);
        LogReformat(workOrder, result);
        return result;
    }

    internal static ReformatResult? ParseResponse(string responseText, WorkOrder workOrder)
    {
        var title = ReformatResponseLineParser.ReadTitle(responseText, workOrder.Title ?? string.Empty);
        var description = ReformatResponseLineParser.ReadDescription(responseText, workOrder.Description ?? string.Empty);
        return title == (workOrder.Title ?? string.Empty)
            && description == (workOrder.Description ?? string.Empty)
            ? null
            : new ReformatResult(title, description);
    }

    private static bool IsNoChangeResponse(string? responseText) =>
        string.IsNullOrWhiteSpace(responseText)
        || responseText.Equals("NO_CHANGES", StringComparison.OrdinalIgnoreCase);

    private void LogReformat(WorkOrder workOrder, ReformatResult? result)
    {
        if (result == null)
        {
            return;
        }

        logger.LogInformation(
            "Reformatted WorkOrder {WorkOrderNumber}: Title changed={TitleChanged}, Description changed={DescriptionChanged}",
            workOrder.Number,
            result.Title != workOrder.Title,
            result.Description != workOrder.Description);
    }

    private async Task<string?> RequestReformatTextAsync(WorkOrder workOrder)
    {
        var chatClient = await chatClientFactory.GetChatClient();
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, ReformatPrompts.System),
            new(ChatRole.User, ReformatPrompts.ForWorkOrder(workOrder))
        };
        var response = await chatClient.GetResponseAsync(messages);
        return response.Text.Trim();
    }
}

internal static class ReformatResponseLineParser
{
    internal static string ReadTitle(string responseText, string defaultTitle)
    {
        foreach (var line in responseText.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (TryParseTitle(line, out var title))
            {
                return title;
            }
        }

        return defaultTitle;
    }

    internal static string ReadDescription(string responseText, string defaultDescription)
    {
        foreach (var line in responseText.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (TryParseDescription(line, out var description))
            {
                return description;
            }
        }

        return defaultDescription;
    }

    private static bool TryParseTitle(string line, out string value) =>
        TryParsePrefixedLine(line, "TITLE:", out value);

    private static bool TryParseDescription(string line, out string value) =>
        TryParsePrefixedLine(line, "DESCRIPTION:", out value);

    private static bool TryParsePrefixedLine(string line, string prefix, out string value)
    {
        value = string.Empty;
        if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        value = line[prefix.Length..].Trim();
        return true;
    }
}

internal static class ReformatPrompts
{
    internal const string System = """
                                   You are an AI agent responsible for reformatting work order fields.
                                   You will receive a work order title and description.

                                   Your tasks:
                                   1. Correct the description for grammar and punctuation. Do not change the meaning.
                                   2. Ensure the title starts with a capital letter. Do not change anything else about the title.

                                   If no changes are needed, respond with exactly: NO_CHANGES

                                   Otherwise respond in this exact format (two lines only):
                                   TITLE: <corrected title>
                                   DESCRIPTION: <corrected description>
                                   """;

    internal static string ForWorkOrder(WorkOrder workOrder) =>
        $"""
         Title: {workOrder.Title}
         Description: {workOrder.Description}
         """;
}
