using System.Collections.Frozen;
using System.Text.Json.Serialization.Metadata;

namespace ClearMeasure.Bootcamp.Core.Messaging;

/// <summary>
/// Resolves a <see cref="WebServiceMessage.TypeName"/> to its source-generated JSON contract. The catalog is also the
/// wire allow-list: a type name outside it is never instantiated.
/// </summary>
public static class RemotableMessageCatalog
{
    private static readonly FrozenDictionary<string, JsonTypeInfo> ContractsByTypeName = IndexByTypeName();

    public static string TypeNameOf(Type type) => type.FullName + ", " + type.Assembly.GetName().Name;

    public static IEnumerable<Type> Types => ContractsByTypeName.Values.Select(contract => contract.Type);

    public static JsonTypeInfo ContractFor(Type type) =>
        RemotableMessageJsonContext.Default.GetTypeInfo(type)
        ?? throw new InvalidOperationException($"{type.FullName} is not a remotable message type.");

    public static JsonTypeInfo ContractFor(string typeName) =>
        ContractsByTypeName.TryGetValue(typeName, out var contract)
            ? contract
            : throw new InvalidOperationException($"{typeName} is not a remotable message type.");

    private static FrozenDictionary<string, JsonTypeInfo> IndexByTypeName()
    {
        var context = RemotableMessageJsonContext.Default;
        JsonTypeInfo[] contracts =
        [
            context.ApplicationChatQuery, context.AssignedToCancelledCommand, context.AssignedToInProgressCommand,
            context.BuildingRoomSummaryQuery, context.CreateDatedWorkOrdersCommand, context.CreateTeamCommand,
            context.CreateVendorCommand, context.DeleteSearchViewCommand, context.DeploymentIdentityQuery,
            context.DraftToAssignedCommand, context.EmployeeByUserNameQuery, context.EmployeeGetAllQuery,
            context.EmployeeWorkLoadQuery, context.EmployeeWorkLoadSummaryQuery, context.FeatureFlagsQuery,
            context.ForecastQuery, context.HealthCheckRemotableRequest, context.InProgressToAssignedCommand,
            context.InProgressToCancelledCommand, context.InProgressToCompleteCommand,
            context.InProgressToWaitingOnVendorCommand, context.NotificationsPanelQuery, context.RecentWorkOrdersQuery,
            context.RemoveAttachmentCommand, context.SaveDraftCommand, context.SaveSearchViewCommand,
            context.SavedSearchViewsQuery, context.ServerHealthCheckQuery, context.SlaHealthQuery, context.TeamsQuery,
            context.UserLoggedInEvent, context.VendorsQuery, context.WaitingOnVendorToCancelledCommand,
            context.WaitingOnVendorToInProgressCommand, context.WorkOrderAttachmentsQuery,
            context.WorkOrderBuildingsQuery, context.WorkOrderByNumberQuery, context.WorkOrderCountByStatusQuery,
            context.WorkOrderReportsQuery, context.WorkOrderSearchSummaryQuery, context.WorkOrderSpecificationQuery,
            context.WorkOrderStatusHistoryQuery, context.WorkOrdersByNumbersQuery,
            context.ApplicationChatResult, context.BuildingSummaryArray, context.CreateDatedWorkOrdersResult,
            context.CreateTeamResult, context.CreateVendorResult, context.DeploymentIdentity,
            context.DictionaryStringInt32, context.EmployeeArray, context.EmployeeWorkLoad,
            context.EmployeeWorkLoadSummaryArray, context.FeatureFlagsResult, context.HealthStatus,
            context.NotificationsSummary, context.RemoveAttachmentResult, context.SavedSearchViewArray,
            context.SavedSearchViewResult, context.SlaHealth, context.StateCommandResult, context.StringArray,
            context.TeamArray, context.VendorArray, context.WeatherForecastArray, context.WorkOrderArray,
            context.WorkOrderAttachmentArray, context.WorkOrderReportsData, context.WorkOrderSearchSummary,
            context.WorkOrderStatusChangeArray,
            context.Employee, context.Role, context.Team, context.Vendor, context.WeatherForecast, context.WorkOrder,
            context.WorkOrderStatus, context.WorkOrderStatusChange
        ];
        return contracts.ToFrozenDictionary(contract => TypeNameOf(contract.Type), StringComparer.Ordinal);
    }
}
