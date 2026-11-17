namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.AotHandlers;

/// <summary>
/// Handlers that run unchanged in the UI.Server Native AOT build, each with why that is safe. Under AOT, EF Core runs the
/// precompiled form of each query, and EF 11 failed to bind the date-range parameters of SlaHealthQueryHandler's query
/// (HTTP 500, PR #1911), so a handler belongs here only when its queries capture no DateTime or DateOnly parameter and it
/// has been exercised against the AOT image. Any other handler gets an Aot* snapshot handler and a parity fixture
/// instead (see <see cref="AotHandlerCoverageTests"/>).
/// </summary>
public static class AotSafeHandlerRegister
{
    /// <summary>Handler type name to the reason it is safe under Native AOT.</summary>
    public static IReadOnlyDictionary<string, string> Handlers { get; } = new Dictionary<string, string>
    {
        ["AddAttachmentMetadataCommandHandler"] = "Inserts an attachment row; no query.",
        ["CreateDatedWorkOrdersHandler"] = "Reads the employee by user name (string) and inserts work orders.",
        ["DatedWorkOrdersCreatedTelemetryHandler"] = "Adds to a metrics counter; no data access.",
        ["EmployeeQueryHandler"] = "Reads one employee by user name (string), or all employees, with roles.",
        ["FeatureFlagsQueryHandler"] = "Asks the feature-flag evaluator; no data access.",
        ["ForecastQueryHandler"] = "Generates sample values; no data access.",
        ["HealthCheckRemotableRequestHandler"] = "Reports health; no EF query.",
        ["ReformatWorkOrderCommandHandler"] = "Reads the work order by number (string) and saves it.",
        ["RemoveAttachmentCommandHandler"] = "Reads rows by id (Guid) and user name (string), then deletes one.",
        ["SavedSearchViewHandler"] = "Reads views by employee id (Guid), then inserts or deletes one.",
        ["StateCommandHandler"] = "Checks a work order number (string) exists and saves the change.",
        ["TeamQueryHandler"] = "Unfiltered reads of teams and employees, then an insert.",
        ["TelemetryHandler"] = "Adds to a metrics counter; no data access.",
        ["VendorQueryHandler"] = "Unfiltered reads of vendors, then an insert.",
        ["WorkOrderAttachmentsQueryHandler"] = "Reads attachments by work order id (Guid).",
        ["WorkOrderBuildingsQueryHandler"] = "Constant predicate and a string projection; no parameters.",
        ["WorkOrderCountByStatusQueryHandler"] = "Groups by status; no parameters.",
        ["WorkOrderQueryHandler"] = "Reads by number (string), or lists by optional assignee, creator (entity keys) and status.",
        ["WorkOrderStatusHistoryQueryHandler"] = "Reads status changes by work order id (Guid).",
        ["ChatClientConfigQueryHandler"] = "Reads configuration; no data access.",
        ["DeploymentIdentityQueryHandler"] = "Reads configuration; no data access.",
        ["ServerHealthCheckHandler"] = "Runs the ASP.NET Core health checks; no EF query of its own.",
        ["ApplicationChatHandler"] = "Calls the chat client; data goes through IBus to the handlers above.",
        ["WorkOrderChatHandler"] = "Calls the chat client; data goes through IBus to the handlers above."
    };
}
