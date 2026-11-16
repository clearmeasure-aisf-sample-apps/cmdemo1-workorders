using System.Text.Json;
using System.Text.Json.Serialization;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.Events;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClearMeasure.Bootcamp.Core.Messaging;

/// <summary>
/// Compile-time JSON contracts for the <see cref="WebServiceMessage"/> envelope and every payload the remotable bus
/// carries: each <see cref="IRemotableRequest"/>, <see cref="IRemotableEvent"/>, the response each request answers
/// with, and the domain models a response can be on its own. <see cref="RemotableMessageCatalog"/> is the type-name
/// index over the same set.
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.General, IncludeFields = false)]
[JsonSerializable(typeof(WebServiceMessage))]
// Requests and events
[JsonSerializable(typeof(ApplicationChatQuery))]
[JsonSerializable(typeof(AssignedToCancelledCommand))]
[JsonSerializable(typeof(AssignedToInProgressCommand))]
[JsonSerializable(typeof(BuildingRoomSummaryQuery))]
[JsonSerializable(typeof(CreateDatedWorkOrdersCommand))]
[JsonSerializable(typeof(CreateTeamCommand))]
[JsonSerializable(typeof(CreateVendorCommand))]
[JsonSerializable(typeof(DeleteSearchViewCommand))]
[JsonSerializable(typeof(DeploymentIdentityQuery))]
[JsonSerializable(typeof(DraftToAssignedCommand))]
[JsonSerializable(typeof(EmployeeByUserNameQuery))]
[JsonSerializable(typeof(EmployeeGetAllQuery))]
[JsonSerializable(typeof(EmployeeWorkLoadQuery))]
[JsonSerializable(typeof(EmployeeWorkLoadSummaryQuery))]
[JsonSerializable(typeof(FeatureFlagsQuery))]
[JsonSerializable(typeof(ForecastQuery))]
[JsonSerializable(typeof(HealthCheckRemotableRequest))]
[JsonSerializable(typeof(InProgressToAssignedCommand))]
[JsonSerializable(typeof(InProgressToCancelledCommand))]
[JsonSerializable(typeof(InProgressToCompleteCommand))]
[JsonSerializable(typeof(InProgressToWaitingOnVendorCommand))]
[JsonSerializable(typeof(NotificationsPanelQuery))]
[JsonSerializable(typeof(RecentWorkOrdersQuery))]
[JsonSerializable(typeof(RemoveAttachmentCommand))]
[JsonSerializable(typeof(SaveDraftCommand))]
[JsonSerializable(typeof(SaveSearchViewCommand))]
[JsonSerializable(typeof(SavedSearchViewsQuery))]
[JsonSerializable(typeof(ServerHealthCheckQuery))]
[JsonSerializable(typeof(SlaHealthQuery))]
[JsonSerializable(typeof(TeamsQuery))]
[JsonSerializable(typeof(UserLoggedInEvent))]
[JsonSerializable(typeof(VendorsQuery))]
[JsonSerializable(typeof(WaitingOnVendorToCancelledCommand))]
[JsonSerializable(typeof(WaitingOnVendorToInProgressCommand))]
[JsonSerializable(typeof(WorkOrderAttachmentsQuery))]
[JsonSerializable(typeof(WorkOrderBuildingsQuery))]
[JsonSerializable(typeof(WorkOrderByNumberQuery))]
[JsonSerializable(typeof(WorkOrderCountByStatusQuery))]
[JsonSerializable(typeof(WorkOrderReportsQuery))]
[JsonSerializable(typeof(WorkOrderSearchSummaryQuery))]
[JsonSerializable(typeof(WorkOrderSpecificationQuery))]
[JsonSerializable(typeof(WorkOrderStatusHistoryQuery))]
[JsonSerializable(typeof(WorkOrdersByNumbersQuery))]
// Responses
[JsonSerializable(typeof(ApplicationChatResult))]
[JsonSerializable(typeof(BuildingSummary[]))]
[JsonSerializable(typeof(CreateDatedWorkOrdersResult))]
[JsonSerializable(typeof(CreateTeamResult))]
[JsonSerializable(typeof(CreateVendorResult))]
[JsonSerializable(typeof(DeploymentIdentity))]
[JsonSerializable(typeof(Dictionary<string, int>))]
[JsonSerializable(typeof(Employee[]))]
[JsonSerializable(typeof(EmployeeWorkLoad))]
[JsonSerializable(typeof(EmployeeWorkLoadSummary[]))]
[JsonSerializable(typeof(FeatureFlagsResult))]
[JsonSerializable(typeof(HealthStatus))]
[JsonSerializable(typeof(NotificationsSummary))]
[JsonSerializable(typeof(RemoveAttachmentResult))]
[JsonSerializable(typeof(SavedSearchView[]))]
[JsonSerializable(typeof(SavedSearchViewResult))]
[JsonSerializable(typeof(SlaHealth))]
[JsonSerializable(typeof(StateCommandResult))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(Team[]))]
[JsonSerializable(typeof(Vendor[]))]
[JsonSerializable(typeof(WeatherForecast[]))]
[JsonSerializable(typeof(WorkOrder[]))]
[JsonSerializable(typeof(WorkOrderAttachment[]))]
[JsonSerializable(typeof(WorkOrderReportsData))]
[JsonSerializable(typeof(WorkOrderSearchSummary))]
[JsonSerializable(typeof(WorkOrderStatusChange[]))]
// Domain models that travel on their own
[JsonSerializable(typeof(Employee))]
[JsonSerializable(typeof(Role))]
[JsonSerializable(typeof(Team))]
[JsonSerializable(typeof(Vendor))]
[JsonSerializable(typeof(WeatherForecast))]
[JsonSerializable(typeof(WorkOrder))]
[JsonSerializable(typeof(WorkOrderStatus))]
[JsonSerializable(typeof(WorkOrderStatusChange))]
public sealed partial class RemotableMessageJsonContext : JsonSerializerContext;
