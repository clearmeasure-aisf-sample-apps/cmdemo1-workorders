using System.ComponentModel;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.McpServer.Tools.Arguments;
using ModelContextProtocol.Server;

namespace ClearMeasure.Bootcamp.McpServer.Tools;

/// <summary>
/// The work-order MCP tools. Each tool's parameters are its MCP input schema; the tool turns them into an argument
/// object, sends the request through <see cref="IBus"/> and answers with <see cref="WorkOrderJson"/> or a message.
/// </summary>
[McpServerToolType]
public class WorkOrderTools
{
    [McpServerTool(Name = "list-work-orders"), Description("Lists all work orders, optionally filtered by status, creator username, and assignee username. Valid statuses: Draft, Assigned, InProgress, Complete.")]
    public static async Task<string> ListWorkOrders(
        IBus bus,
        [Description("Optional status filter (Draft, Assigned, InProgress, Complete)")] string? status = null,
        [Description("Optional creator username filter")] string? creatorUsername = null,
        [Description("Optional assignee username filter")] string? assigneeUsername = null)
    {
        var query = await new WorkOrderListFilter(status, creatorUsername, assigneeUsername).ToQueryAsync(bus);
        return query == null ? "[]" : WorkOrderJson.Summaries(await bus.Send(query));
    }

    [McpServerTool(Name = "get-work-order"), Description("Retrieves a single work order by its number, including full details.")]
    public static async Task<string> GetWorkOrder(
        IBus bus,
        [Description("The work order number")] string workOrderNumber)
    {
        var workOrder = await bus.Send(new WorkOrderByNumberQuery(workOrderNumber));
        return workOrder == null ? NotFound(workOrderNumber) : WorkOrderJson.Detail(workOrder);
    }

    [McpServerTool(Name = "create-work-order"), Description("Creates a new draft work order. Requires a title, description, and the username of the creator. Optionally accepts a room number, due date (yyyy-MM-dd), and instructions. Priority values: Low, Medium (default), High (case-insensitive). Building is a short name (max 100 characters).")]
    public static async Task<string> CreateWorkOrder(
        IBus bus,
        IWorkOrderNumberGenerator numberGenerator,
        [Description("Title of the work order")] string title,
        [Description("Description of the work order")] string description,
        [Description("Username of the employee creating the work order")] string creatorUsername,
        [Description("Optional room number or location for the work order")] string? roomNumber = null,
        [Description("Optional due date as yyyy-MM-dd")] string? dueDate = null,
        [Description("Optional instructions for the work order")] string? instructions = null,
        [Description("Optional priority: Low, Medium (default), High; case-insensitive")] string? priority = null,
        [Description("Optional building name; max 100 characters")] string? building = null)
    {
        var arguments = new NewWorkOrderArguments(title, description, roomNumber, dueDate, instructions, priority, building);
        try
        {
            var creator = await EmployeeLookup.FindAsync(bus, creatorUsername);
            var error = creator == null ? $"Employee with username '{creatorUsername}' not found." : arguments.Error;
            return error ?? await SaveDraftAsync(bus, arguments.ToDraft(creator!, numberGenerator.GenerateNumber()));
        }
        catch (Exception ex)
        {
            return $"Error creating work order: {ex.Message}";
        }
    }

    [McpServerTool(Name = "create-dated-work-orders"), Description(
        "Creates multiple dated assigned work orders in ONE transaction. " +
        "Use this for scheduling (e.g. next N Saturdays). " +
        "Looks up the assignee first; if missing, creates nothing. " +
        "Pass dueDates as comma-separated yyyy-MM-dd values, or omit dueDates and set saturdayCount to create consecutive Saturdays in America/Chicago starting with the coming Saturday.")]
    public static async Task<string> CreateDatedWorkOrders(
        IBus bus,
        TimeProvider timeProvider,
        [Description("Username of the employee creating the work orders (logged-in user)")] string creatorUsername,
        [Description("Username of the assignee (e.g. gwillie)")] string assigneeUsername,
        [Description("Title for each work order")] string title,
        [Description("Description for each work order")] string description,
        [Description("Optional comma-separated due dates as yyyy-MM-dd")] string? dueDates = null,
        [Description("When dueDates is omitted, number of consecutive Chicago Saturdays to schedule (default 10)")] int saturdayCount = 10)
    {
        try
        {
            var (dates, resolveError) = DatedWorkOrderScheduling.ResolveDueDates(timeProvider, dueDates, saturdayCount);
            var error = resolveError ?? NewWorkOrderArguments.DescriptionError(description);
            return error ?? DatedWorkOrderScheduling.FormatResult(await bus.Send(
                new CreateDatedWorkOrdersCommand(creatorUsername, assigneeUsername, title, description, dates)));
        }
        catch (Exception ex)
        {
            return $"Error creating dated work orders: {ex.Message}";
        }
    }

    [McpServerTool(Name = "save-work-order"), Description(
        "Saves title, description, instructions, room, and due date on an existing draft work order without changing status. " +
        "Requires the work order number and executing username (the creator). " +
        "Optional patches: omit a field to leave it unchanged; empty description/instructions/room persists empty; empty due date clears. " +
        "Use update-work-order to change priority, building, room, and due date together on existing work orders.")]
    public static async Task<string> SaveWorkOrder(
        IBus bus,
        [Description("The work order number")] string workOrderNumber,
        [Description("Username of the employee executing the save (must be the creator)")] string executingUsername,
        [Description("Optional title patch")] string? title = null,
        [Description("Optional description patch")] string? description = null,
        [Description("Optional instructions patch")] string? instructions = null,
        [Description("Optional room number patch")] string? roomNumber = null,
        [Description("Optional due date patch as yyyy-MM-dd; empty clears")] string? dueDate = null)
    {
        var patch = new SaveWorkOrderPatch(title, description, instructions, new RoomNumberPatch(roomNumber), new DueDatePatch(dueDate));
        var (context, error) = await WorkOrderRequestContext.LoadForEditAsync(bus, workOrderNumber, executingUsername);
        error ??= patch.ErrorFor(context!.WorkOrder, context.ExecutingUser);
        return error ?? WorkOrderJson.Detail((await bus.Send(patch.ApplyAsSave(context!))).WorkOrder);
    }

    [McpServerTool(Name = "update-work-order"), Description(
        "Updates priority, building, room, and/or due date on an existing draft work order. " +
        "Requires the work order number and executing username (the creator). " +
        "Omitted fields are unchanged; empty building/room/due date clears those fields; empty priority is an error. " +
        "Priority values: Low, Medium, High (case-insensitive). Building: max 100 characters.")]
    public static async Task<string> UpdateWorkOrder(
        IBus bus,
        [Description("The work order number")] string workOrderNumber,
        [Description("Username of the employee executing the update (must be the creator)")] string executingUsername,
        [Description("Optional priority: Low, Medium, High; case-insensitive")] string? priority = null,
        [Description("Optional building name; max 100 characters; empty clears")] string? building = null,
        [Description("Optional room number; empty clears")] string? roomNumber = null,
        [Description("Optional due date as yyyy-MM-dd; empty clears")] string? dueDate = null)
    {
        var patch = new UpdateWorkOrderPatch(priority, building, new RoomNumberPatch(roomNumber), new DueDatePatch(dueDate));
        var (context, error) = await WorkOrderRequestContext.LoadForEditAsync(bus, workOrderNumber, executingUsername);
        error ??= patch.ErrorFor(context!.WorkOrder, context.ExecutingUser);
        return error ?? WorkOrderJson.Detail((await bus.Send(patch.ApplyAsSave(context!))).WorkOrder);
    }

    [McpServerTool(Name = "execute-work-order-command"), Description("Executes a state command on a work order. Available commands: DraftToAssignedCommand (requires assigneeUsername), AssignedToInProgressCommand, InProgressToAssignedCommand, Shelve, InProgressToCompleteCommand, AssignedToCancelledCommand. Shelve and InProgressToAssignedCommand require a reason of at most 200 characters.")]
    public static Task<string> ExecuteWorkOrderCommand(
        IBus bus,
        [Description("The work order number")] string workOrderNumber,
        [Description("The command name (e.g., DraftToAssignedCommand)")] string commandName,
        [Description("Username of the employee executing the command")] string executingUsername,
        [Description("Username of the employee to assign the work order to (required for DraftToAssignedCommand)")] string? assigneeUsername = null,
        [Description("Reason for shelving (required for Shelve and InProgressToAssignedCommand, at most 200 characters)")] string? reason = null) =>
        WorkOrderCommandExecutor.ExecuteCommandAsync(
            bus,
            new WorkOrderCommandRequest(workOrderNumber, commandName, executingUsername, assigneeUsername, reason));

    [McpServerTool(Name = "list-work-order-attachments"), Description("Lists all attachment metadata for a given work order by its number.")]
    public static async Task<string> ListWorkOrderAttachments(
        IBus bus,
        [Description("The work order number")] string workOrderNumber)
    {
        var workOrder = await bus.Send(new WorkOrderByNumberQuery(workOrderNumber));
        return workOrder == null
            ? NotFound(workOrderNumber)
            : WorkOrderJson.Attachments(await bus.Send(new WorkOrderAttachmentsQuery(workOrder.Id)));
    }

    private static async Task<string> SaveDraftAsync(IBus bus, WorkOrder draft) =>
        WorkOrderJson.Detail((await bus.Send(new SaveDraftCommand(draft, draft.Creator!))).WorkOrder);

    private static string NotFound(string workOrderNumber) => $"No work order found with number '{workOrderNumber}'.";
}
