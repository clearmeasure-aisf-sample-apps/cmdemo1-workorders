using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using ClearMeasure.Bootcamp.UI.Shared;
using Mediator;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderManage;

/// <summary>
/// Configurable <see cref="IBus"/> for manage-page bUnit tests: queries, state commands, and attachments.
/// </summary>
public sealed class WorkOrderManageBus : Bus
{
    private readonly List<WorkOrderAttachment> _attachments = [];
    private WorkOrderStatusChange[] _history = [];
    private WorkOrder? _existing;
    private Task? _commandGate;
    private bool _captureStateCommands;
    private bool _captureRemoveAttachment;

    // ReSharper disable once ConvertToPrimaryConstructor -- epic guardrail: no mass primary-constructor conversion
    public WorkOrderManageBus() : base(null!)
    {
    }

    /// <summary>Last captured <see cref="IStateCommand"/>, when capture is enabled.</summary>
    public object? LastCommand { get; private set; }

    /// <summary>How many state commands were received.</summary>
    public int CommandCount { get; private set; }

    /// <summary>How many <see cref="WorkOrderByNumberQuery"/> hits occurred.</summary>
    public int WorkOrderByNumberHits { get; private set; }

    /// <summary>How many <see cref="EmployeeByUserNameQuery"/> hits occurred.</summary>
    public int EmployeeByUserNameHits { get; private set; }

    /// <summary>Last remove-attachment command, when capture is enabled.</summary>
    public RemoveAttachmentCommand? LastRemoveCommand { get; private set; }

    /// <summary>Result returned for remove-attachment commands.</summary>
    public RemoveAttachmentResult RemoveResult { get; set; } = RemoveAttachmentResult.Success;

    /// <summary>Seeds the work order returned by <see cref="WorkOrderByNumberQuery"/>.</summary>
    public WorkOrderManageBus WithExisting(WorkOrder workOrder)
    {
        _existing = workOrder;
        return this;
    }

    /// <summary>Seeds attachment rows returned by <see cref="WorkOrderAttachmentsQuery"/>.</summary>
    public WorkOrderManageBus WithAttachments(params WorkOrderAttachment[] attachments)
    {
        _attachments.Clear();
        _attachments.AddRange(attachments);
        return this;
    }

    /// <summary>Seeds status-history rows.</summary>
    public WorkOrderManageBus WithHistory(params WorkOrderStatusChange[] history)
    {
        _history = history;
        return this;
    }

    /// <summary>Captures <see cref="IStateCommand"/> sends and returns a success result.</summary>
    public WorkOrderManageBus CaptureStateCommands(Task? gate = null)
    {
        _captureStateCommands = true;
        _commandGate = gate;
        return this;
    }

    /// <summary>Captures <see cref="RemoveAttachmentCommand"/> and mutates the attachment list on success.</summary>
    public WorkOrderManageBus CaptureRemoveAttachment()
    {
        _captureRemoveAttachment = true;
        return this;
    }

    /// <inheritdoc />
    public override Task Publish(INotification notification) => Task.CompletedTask;

    /// <inheritdoc />
    public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        if (TryHandleQuery(request, out TResponse? queryResponse))
        {
            return Task.FromResult(queryResponse!);
        }

        if (_captureRemoveAttachment && request is RemoveAttachmentCommand remove)
        {
            return Task.FromResult((TResponse)(object)Remove(remove));
        }

        if (_captureStateCommands && request is IStateCommand)
        {
            return CaptureCommand(request);
        }

        throw new NotImplementedException($"Unhandled request type: {request.GetType().Name}");
    }

    private bool TryHandleQuery<TResponse>(IRequest<TResponse> request, out TResponse? response)
    {
        response = default;
        switch (request)
        {
            case EmployeeGetAllQuery:
                response = (TResponse)(object)Array.Empty<Employee>();
                return true;
            case WorkOrderAttachmentsQuery:
                response = (TResponse)(object)_attachments.ToArray();
                return true;
            case WorkOrderStatusHistoryQuery:
                response = (TResponse)(object)_history;
                return true;
            case WorkOrderByNumberQuery when _existing != null:
                WorkOrderByNumberHits++;
                response = (TResponse)(object)_existing;
                return true;
            case EmployeeByUserNameQuery byUser:
                response = (TResponse)(object)ResolveEmployee(byUser);
                return true;
            default:
                return false;
        }
    }

    private Employee ResolveEmployee(EmployeeByUserNameQuery byUser)
    {
        EmployeeByUserNameHits++;
        if (_existing?.Assignee?.UserName == byUser.Username)
        {
            return _existing.Assignee;
        }

        return EmployeeBuilder.Create(byUser.Username)
            .WithFirstName("A")
            .WithLastName("B")
            .WithEmail("a@b.com")
            .Build();
    }

    private async Task<TResponse> CaptureCommand<TResponse>(IRequest<TResponse> request)
    {
        LastCommand = request;
        CommandCount++;
        var wo = _existing ?? WorkOrderBuilder.Create()
            .WithNumber("WO-TEST")
            .WithStatus(WorkOrderStatus.Draft)
            .Build();
        var result = (TResponse)(object)new StateCommandResult(wo);
        if (_commandGate != null)
        {
            await _commandGate;
        }

        return result;
    }

    private RemoveAttachmentResult Remove(RemoveAttachmentCommand command)
    {
        LastRemoveCommand = command;
        if (RemoveResult.Succeeded)
        {
            _attachments.RemoveAll(a => a.Id == command.AttachmentId);
        }

        return RemoveResult;
    }
}
