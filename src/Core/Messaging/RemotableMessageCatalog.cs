using System.Collections.Frozen;
using System.Text.Json.Serialization.Metadata;
using FluentValidation;

namespace ClearMeasure.Bootcamp.Core.Messaging;

/// <summary>
/// Resolves a <see cref="WebServiceMessage.TypeName"/> to its source-generated JSON contract. The catalog is also the
/// wire allow-list: a type name outside it is never instantiated.
/// Each entry also resolves the payload's validator without building a generic type at run time.
/// </summary>
public static class RemotableMessageCatalog
{
    private static readonly IReadOnlyList<MessageType> MessageTypes = Catalog();
    private static readonly FrozenDictionary<Type, MessageType> ByType = MessageTypes.ToFrozenDictionary(type => type.Contract.Type);

    private static readonly FrozenDictionary<string, MessageType> ByTypeName =
        MessageTypes.ToFrozenDictionary(type => TypeNameOf(type.Contract.Type), StringComparer.Ordinal);

    public static string TypeNameOf(Type type) => type.FullName + ", " + type.Assembly.GetName().Name;

    public static IEnumerable<Type> Types => ByType.Keys;

    public static JsonTypeInfo ContractFor(Type type) => Find(ByType, type, type.FullName).Contract;

    public static JsonTypeInfo ContractFor(string typeName) => Find(ByTypeName, typeName, typeName).Contract;

    /// <summary>
    /// The FluentValidation validator registered for exactly the payload's type, or null. Resolved through the
    /// catalog's closed <see cref="IValidator{T}"/> so no generic type is built at run time (Native AOT).
    /// </summary>
    public static IValidator? ValidatorFor(object payload, IServiceProvider services) =>
        ByType.TryGetValue(payload.GetType(), out var type) ? type.ResolveValidator(services) : null;

    private static MessageType Find<TKey>(FrozenDictionary<TKey, MessageType> index, TKey key, string? name)
        where TKey : notnull =>
        index.TryGetValue(key, out var type)
            ? type
            : throw new InvalidOperationException($"{name} is not a remotable message type.");

    private static MessageType Of<T>(JsonTypeInfo<T> contract) =>
        new(contract, services => services.GetService(typeof(IValidator<T>)) as IValidator);

    private sealed record MessageType(JsonTypeInfo Contract, Func<IServiceProvider, IValidator?> ResolveValidator);

    private static MessageType[] Catalog()
    {
        var context = RemotableMessageJsonContext.Default;
        return [.. RequestsAndEvents(context), .. Responses(context), .. DomainModels(context)];
    }

    /// <summary>Every <see cref="IRemotableRequest"/> and <see cref="IRemotableEvent"/> a client may post.</summary>
    private static MessageType[] RequestsAndEvents(RemotableMessageJsonContext context) =>
    [
        Of(context.ApplicationChatQuery), Of(context.AssignedToCancelledCommand), Of(context.AssignedToInProgressCommand),
        Of(context.BuildingRoomSummaryQuery), Of(context.CreateDatedWorkOrdersCommand), Of(context.CreateTeamCommand),
        Of(context.CreateVendorCommand), Of(context.DeleteSearchViewCommand), Of(context.DeploymentIdentityQuery),
        Of(context.DraftToAssignedCommand), Of(context.EmployeeByUserNameQuery), Of(context.EmployeeGetAllQuery),
        Of(context.EmployeeWorkLoadQuery), Of(context.EmployeeWorkLoadSummaryQuery), Of(context.FeatureFlagsQuery),
        Of(context.ForecastQuery), Of(context.HealthCheckRemotableRequest), Of(context.InProgressToAssignedCommand),
        Of(context.InProgressToCancelledCommand), Of(context.InProgressToCompleteCommand),
        Of(context.InProgressToWaitingOnVendorCommand), Of(context.NotificationsPanelQuery), Of(context.RecentWorkOrdersQuery),
        Of(context.RemoveAttachmentCommand), Of(context.SaveDraftCommand), Of(context.SaveSearchViewCommand),
        Of(context.SavedSearchViewsQuery), Of(context.ServerHealthCheckQuery), Of(context.SlaHealthQuery), Of(context.TeamsQuery),
        Of(context.UserLoggedInEvent), Of(context.VendorsQuery), Of(context.WaitingOnVendorToCancelledCommand),
        Of(context.WaitingOnVendorToInProgressCommand), Of(context.WorkOrderAttachmentsQuery),
        Of(context.WorkOrderBuildingsQuery), Of(context.WorkOrderByNumberQuery), Of(context.WorkOrderCountByStatusQuery),
        Of(context.WorkOrderReportsQuery), Of(context.WorkOrderSearchSummaryQuery), Of(context.WorkOrderSpecificationQuery),
        Of(context.WorkOrderStatusHistoryQuery), Of(context.WorkOrdersByNumbersQuery)
    ];

    /// <summary>The response each request answers with.</summary>
    private static MessageType[] Responses(RemotableMessageJsonContext context) =>
    [
        Of(context.ApplicationChatResult), Of(context.BuildingSummaryArray), Of(context.CreateDatedWorkOrdersResult),
        Of(context.CreateTeamResult), Of(context.CreateVendorResult), Of(context.DeploymentIdentity),
        Of(context.DictionaryStringInt32), Of(context.EmployeeArray), Of(context.EmployeeWorkLoad),
        Of(context.EmployeeWorkLoadSummaryArray), Of(context.FeatureFlagsResult), Of(context.HealthStatus),
        Of(context.NotificationsSummary), Of(context.RemoveAttachmentResult), Of(context.SavedSearchViewArray),
        Of(context.SavedSearchViewResult), Of(context.SlaHealth), Of(context.StateCommandResult), Of(context.StringArray),
        Of(context.TeamArray), Of(context.VendorArray), Of(context.WeatherForecastArray), Of(context.WorkOrderArray),
        Of(context.WorkOrderAttachmentArray), Of(context.WorkOrderReportsData), Of(context.WorkOrderSearchSummary),
        Of(context.WorkOrderStatusChangeArray)
    ];

    /// <summary>Domain models a response can be on its own.</summary>
    private static MessageType[] DomainModels(RemotableMessageJsonContext context) =>
    [
        Of(context.Employee), Of(context.Role), Of(context.Team), Of(context.Vendor), Of(context.WeatherForecast), Of(context.WorkOrder),
        Of(context.WorkOrderStatus), Of(context.WorkOrderStatusChange)
    ];
}
