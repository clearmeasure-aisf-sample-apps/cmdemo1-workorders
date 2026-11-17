using System.ComponentModel;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.McpServer.Tools;
using ModelContextProtocol.Server;

namespace ClearMeasure.Bootcamp.McpServer.Resources;

[McpServerResourceType]
public class ReferenceResources
{
    [McpServerResource(UriTemplate = "churchbulletin://reference/work-order-statuses", Name = "work-order-statuses"),
     Description("All valid work order statuses with their code, key, and friendly name.")]
    public static string GetWorkOrderStatuses()
    {
        var statuses = WorkOrderStatus.GetAllItems()
            .Select(s => new WorkOrderStatusJson(s.Code, s.Key, s.FriendlyName))
            .ToArray();

        return JsonSerializer.Serialize(statuses, McpJsonContext.Default.WorkOrderStatusJsonArray);
    }

    [McpServerResource(UriTemplate = "churchbulletin://reference/work-order-priorities", Name = "work-order-priorities"),
     Description("All valid work order priorities with their name and value.")]
    public static string GetWorkOrderPriorities()
    {
        var priorities = Enum.GetValues<WorkOrderPriority>()
            .Select(priority => new WorkOrderPriorityJson(priority.ToString(), (int)priority))
            .ToArray();

        return JsonSerializer.Serialize(priorities, McpJsonContext.Default.WorkOrderPriorityJsonArray);
    }

    [McpServerResource(UriTemplate = "churchbulletin://reference/roles", Name = "roles"),
     Description("All defined roles with their permissions.")]
    public static string GetRoles()
    {
        RoleJson[] roles =
        [
            new("Manager", CanCreateWorkOrder: true, CanFulfillWorkOrder: false),
            new("Worker", CanCreateWorkOrder: false, CanFulfillWorkOrder: true),
            new("Admin", CanCreateWorkOrder: true, CanFulfillWorkOrder: true)
        ];

        return JsonSerializer.Serialize(roles, McpJsonContext.Default.RoleJsonArray);
    }

    [McpServerResource(UriTemplate = "churchbulletin://reference/status-transitions", Name = "status-transitions"),
     Description("Valid state transitions for work orders. Maps each status to the commands that can be executed and their target status.")]
    public static string GetStatusTransitions()
    {
        var transitions = new StatusTransitionsJson(
            Draft:
            [
                new("SaveDraftCommand", "Draft"),
                new("DraftToAssignedCommand", "Assigned")
            ],
            Assigned: [new("AssignedToInProgressCommand", "InProgress")],
            InProgress: [new("InProgressToCompleteCommand", "Complete")],
            Complete: []);

        return JsonSerializer.Serialize(transitions, McpJsonContext.Default.StatusTransitionsJson);
    }
}

internal sealed record WorkOrderStatusJson(string Code, string Key, string FriendlyName);

internal sealed record WorkOrderPriorityJson(string Name, int Value);

internal sealed record StatusTransitionJson(string Command, string TargetStatus);

internal sealed record StatusTransitionsJson(
    StatusTransitionJson[] Draft,
    StatusTransitionJson[] Assigned,
    StatusTransitionJson[] InProgress,
    StatusTransitionJson[] Complete);
