using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Client.HealthChecks;

namespace ClearMeasure.Bootcamp.UnitTests.Architecture;

/// <summary>
/// Master's HTTP single-API allow-list: the 42 payload types that had a FluentValidation
/// <c>IValidator&lt;T&gt;</c> registered (excluding the <c>WebServiceMessage</c> envelope).
/// </summary>
internal static class HttpAllowListTypes
{
    internal static readonly IReadOnlyList<string> MasterNames =
    [
        "ApplicationChatQuery",
        "AssignedToCancelledCommand",
        "AssignedToInProgressCommand",
        "BuildingRoomSummaryQuery",
        "CreateDatedWorkOrdersCommand",
        "CreateTeamCommand",
        "CreateVendorCommand",
        "DeleteSearchViewCommand",
        "DraftToAssignedCommand",
        "EmployeeByUserNameQuery",
        "EmployeeGetAllQuery",
        "EmployeeWorkLoadQuery",
        "EmployeeWorkLoadSummaryQuery",
        "FeatureFlagsQuery",
        "ForecastQuery",
        "HealthCheckRemotableRequest",
        "InProgressToAssignedCommand",
        "InProgressToCancelledCommand",
        "InProgressToCompleteCommand",
        "InProgressToWaitingOnVendorCommand",
        "NotificationsPanelQuery",
        "RecentWorkOrdersQuery",
        "RemoveAttachmentCommand",
        "SaveDraftCommand",
        "SaveSearchViewCommand",
        "SavedSearchViewsQuery",
        "ServerHealthCheckQuery",
        "SlaHealthQuery",
        "TeamsQuery",
        "UserLoggedInEvent",
        "VendorsQuery",
        "WaitingOnVendorToCancelledCommand",
        "WaitingOnVendorToInProgressCommand",
        "WorkOrderAttachmentsQuery",
        "WorkOrderBuildingsQuery",
        "WorkOrderByNumberQuery",
        "WorkOrderCountByStatusQuery",
        "WorkOrderReportsQuery",
        "WorkOrderSearchSummaryQuery",
        "WorkOrderSpecificationQuery",
        "WorkOrderStatusHistoryQuery",
        "WorkOrdersByNumbersQuery"
    ];

    internal static IReadOnlyList<string> ConcreteRemotableTypeNames()
    {
        var assemblies = new[]
        {
            typeof(IRemotableRequest).Assembly,
            typeof(ServerHealthCheckQuery).Assembly,
            typeof(ChatClientConfigQuery).Assembly
        };

        return assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(IsConcreteRemotablePayload)
            .Select(type => type.Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsConcreteRemotablePayload(Type type) =>
        type is { IsClass: true, IsAbstract: false }
        && (typeof(IRemotableRequest).IsAssignableFrom(type)
            || typeof(IRemotableEvent).IsAssignableFrom(type));
}
