using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.Core.Services.Impl;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Microsoft.AspNetCore.Components;
using Palermo.BlazorMvc;
using Toolbelt.Blazor.SpeechRecognition;
using Toolbelt.Blazor.SpeechSynthesis;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

[Route("/workorder/manage/{id?}")]
public partial class WorkOrderManage : AppComponentBase, IAsyncDisposable
{
    private WorkOrder? _workOrder;
    private WorkOrder? _lastNotifiedWorkOrder;
    private WorkOrderAttachment[] _attachments = [];
    private WorkOrderStatusChange[] _statusHistory = [];
    private bool _canRemoveAttachments;
    private string? _shelveReasonError;
    private string _currentUserName = string.Empty;
    private string _preferredLanguage = "en-US";
    private WorkOrderDictation? _dictation;
    private bool _isSubmitting;
    [Inject] public IWorkOrderBuilder? WorkOrderBuilder { get; set; }
    [Inject] public IUserSession? UserSession { get; set; }
    [Inject] private NavigationManager? NavigationManager { get; set; }
    [Inject] public ITranslationService? TranslationService { get; set; }
    [Inject] public SpeechSynthesis? SpeechSynthesis { get; set; }
    [Inject] public SpeechRecognition? SpeechRecognition { get; set; }
    [Inject] public TimeProvider Clock { get; set; } = TimeProvider.System;

    private DueDateUrgency CurrentDueDateUrgency =>
        _workOrder == null
            ? DueDateUrgency.None
            : DueDateUrgencyCalculator.Calculate(Model.DueDateInput, _workOrder.Status, Clock);

    private string CurrentDueDateCssClass =>
        DueDateUrgencyCalculator.CssClass(CurrentDueDateUrgency);

    private string? CurrentDueDateUrgencyText =>
        DueDateUrgencyCalculator.ScreenReaderText(CurrentDueDateUrgency);

    private StatusPill? CurrentStatusPill =>
        _workOrder == null
            ? null
            : StatusPillPresenter.Present(_workOrder, CurrentDueDateUrgency == DueDateUrgency.Overdue);

    public WorkOrderManageModel Model { get; set; } = new();
    private List<SelectListItem> UserOptions { get; set; } = new();
    private WorkOrderCommandButtons CommandButtons { get; set; } = WorkOrderCommandButtons.None;

    private string? SelectedCommand { get; set; }

    [Parameter] public string? Id { get; set; }

    [SupplyParameterFromQuery] public string? Mode { get; set; }
    private EditMode CurrentMode => Mode?.ToLower() == "edit" ? EditMode.Edit : EditMode.New;

    protected override async Task OnInitializedAsync()
    {
        _dictation = new WorkOrderDictation(SpeechRecognition, AppendDictation, () => InvokeAsync(StateHasChanged));
        await LoadUserOptions();
        await LoadWorkOrder();
    }

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        // Notify only when the loaded work order actually changes identity (e.g. after
        // LoadWorkOrder() completes for a new navigation), not on every render. Notifying
        // unconditionally on every render forced listeners of WorkOrderSelectedEvent (such as
        // WorkOrderChat) to reset their in-progress chat input/history and re-render after every
        // keystroke-driven re-render of this page, which is unnecessary churn and can interleave
        // with in-flight rendering of sibling components during rapid form edits.
        if (_workOrder != null && !ReferenceEquals(_workOrder, _lastNotifiedWorkOrder))
        {
            _lastNotifiedWorkOrder = _workOrder;
            EventBus.Notify(new WorkOrderSelectedEvent(_workOrder));
        }
        return base.OnAfterRenderAsync(firstRender);
    }

    private async Task LoadWorkOrder()
    {
        var currentUser = (await UserSession!.GetCurrentUserAsync())!;
        _preferredLanguage = currentUser.PreferredLanguage;
        _currentUserName = currentUser.UserName;
        var workOrder = await FindOrCreateWorkOrderAsync(CurrentMode, Id, currentUser);

        Model = WorkOrderManageModel.For(CurrentMode, workOrder, Clock);
        CommandButtons = new WorkOrderCommandButtons(new StateCommandList().GetValidStateCommands(workOrder, currentUser));
        Model.IsReadOnly = !CommandButtons.Any;
        _workOrder = workOrder;
        await LoadAttachmentsAndHistoryAsync(workOrder, currentUser);
    }

    /// <summary>A new draft by <paramref name="currentUser"/> (numbered <paramref name="number"/> when given), or the saved work order.</summary>
    private async Task<WorkOrder> FindOrCreateWorkOrderAsync(EditMode mode, string? number, Employee currentUser)
    {
        if (mode != EditMode.New)
        {
            return (await Bus.Send(new WorkOrderByNumberQuery(number!)))!;
        }

        var workOrder = WorkOrderBuilder!.CreateNewWorkOrder(currentUser);
        if (!string.IsNullOrEmpty(number))
        {
            workOrder.Number = number;
        }

        return workOrder;
    }

    private async Task LoadAttachmentsAndHistoryAsync(WorkOrder workOrder, Employee currentUser)
    {
        if (workOrder.Id == Guid.Empty)
        {
            return;
        }

        _attachments = await Bus.Send(new WorkOrderAttachmentsQuery(workOrder.Id));
        _statusHistory = await Bus.Send(new WorkOrderStatusHistoryQuery(workOrder.Id));
        _canRemoveAttachments = RemoveAttachmentCommand.CanRemove(workOrder, currentUser);
    }

    private async Task ReloadAttachmentsAsync() =>
        _attachments = await Bus.Send(new WorkOrderAttachmentsQuery(_workOrder!.Id));

    private async Task LoadUserOptions()
    {
        var employees = await Bus.Send(new EmployeeGetAllQuery());
        var items = employees.Select(e => new SelectListItem(e.UserName, e.DisplayName)).ToList();
        items.Insert(0, new SelectListItem("", ""));
        UserOptions = items;
    }

    private async Task HandleSubmit()
    {
        if (_isSubmitting)
        {
            return;
        }

        try
        {
            _isSubmitting = true;
            await SubmitAsync();
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private async Task SubmitAsync()
    {
        var currentUser = (await UserSession!.GetCurrentUserAsync())!;
        var workOrder = await FindOrCreateWorkOrderAsync(Model.Mode, Model.WorkOrderNumber, currentUser);
        var assignee = Model.AssignedToUserName == null
            ? null
            : await Bus.Send(new EmployeeByUserNameQuery(Model.AssignedToUserName));
        Model.ApplyTo(workOrder, assignee);
        await SendIfReady(CommandFor(workOrder, currentUser));
    }

    /// <summary>The selected command; a save records the room and due date it replaces, a shelve carries its reason.</summary>
    private IStateCommand? CommandFor(WorkOrder workOrder, Employee currentUser)
    {
        var command = CommandWithShelveReason(SaveDraftCommand.WithPreviousRoomNumber(
            new StateCommandList().GetMatchingCommand(workOrder, currentUser, SelectedCommand!),
            Model.OriginalRoomNumber));
        return command is SaveDraftCommand saveDraft ? saveDraft with { PreviousDueDate = Model.OriginalDueDate } : command;
    }

    private IStateCommand? CommandWithShelveReason(IStateCommand command)
    {
        if (command is not InProgressToAssignedCommand shelve)
        {
            return command;
        }

        return ShelvedCommand(shelve);
    }

    private InProgressToAssignedCommand? ShelvedCommand(InProgressToAssignedCommand shelve)
    {
        var reason = Model.ShelveReason?.Trim() ?? "";
        if (!ShelveReasonLengthIsValid(reason))
        {
            _shelveReasonError = ShelveReasonMessage(reason);
            return null;
        }

        _shelveReasonError = null;
        return shelve with { Reason = reason };
    }

    private static bool ShelveReasonLengthIsValid(string reason) =>
        reason is { Length: > 0 and <= InProgressToAssignedCommand.ReasonMaxLength };

    private static string ShelveReasonMessage(string reason) =>
        reason.Length == 0
            ? "A reason is required to shelve a work order."
            : $"Reason cannot exceed {InProgressToAssignedCommand.ReasonMaxLength} characters.";

    private async Task SendIfReady(IStateCommand? command)
    {
        if (command == null)
        {
            return;
        }

        var result = await Bus.Send(command);
        EventBus.Notify(new WorkOrderChangedEvent(result));
        NavigationManager!.NavigateTo("/workorder/search");
    }

    private WorkOrderReadAloud ReadAloud => new(TranslationService, SpeechSynthesis);

    private Task SpeakTitleAsync() => ReadAloud.SpeakAsync(Model.Title, _preferredLanguage);

    private Task SpeakDescriptionAsync() => ReadAloud.SpeakAsync(Model.Description, _preferredLanguage);

    private Task DictateTitleAsync() => _dictation?.ToggleAsync(DictationTarget.Title, _preferredLanguage) ?? Task.CompletedTask;

    private Task DictateDescriptionAsync() =>
        _dictation?.ToggleAsync(DictationTarget.Description, _preferredLanguage) ?? Task.CompletedTask;

    private void AppendDictation(DictationTarget target, string transcript)
    {
        if (target == DictationTarget.Title)
        {
            Model.Title = AppendTranscript(Model.Title, transcript);
        }
        else if (target == DictationTarget.Description)
        {
            Model.Description = AppendTranscript(Model.Description, transcript);
        }
    }

    private static string AppendTranscript(string? existingText, string transcript)
    {
        return string.IsNullOrEmpty(existingText) ? transcript : $"{existingText} {transcript}";
    }

    private string DictateButtonClass(DictationTarget target)
    {
        return _dictation?.IsListeningTo(target) == true
            ? "btn-app btn-app--danger btn-app--icon"
            : "btn-app btn-app--secondary btn-app--icon";
    }

    private string DictateAriaPressed(DictationTarget target)
    {
        return _dictation?.IsListeningTo(target) == true ? "true" : "false";
    }

    public async ValueTask DisposeAsync()
    {
        if (_dictation != null)
        {
            await _dictation.DisposeAsync();
        }
    }
}

public record WorkOrderChangedEvent(StateCommandResult Result) : IUiBusEvent;
