using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.Core.Services.Impl;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Microsoft.AspNetCore.Components;
using Palermo.BlazorMvc;
using Microsoft.JSInterop;
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
    private Guid? _pendingRemoveId;
    private string? _removeAttachmentError;
    private string? _shelveReasonError;
    private string _currentUserName = string.Empty;
    private string _preferredLanguage = "en-US";
    private DictationTarget _dictationTarget = DictationTarget.None;
    private bool _isSubmitting;
    private bool _copyButtonShowSuccess;
    private IJSObjectReference? _clipboardModule;
    [Inject] public IWorkOrderBuilder? WorkOrderBuilder { get; set; }
    [Inject] public IUserSession? UserSession { get; set; }
    [Inject] private NavigationManager? NavigationManager { get; set; }
    [Inject] public ITranslationService? TranslationService { get; set; }
    [Inject] public SpeechSynthesis? SpeechSynthesis { get; set; }
    [Inject] public SpeechRecognition? SpeechRecognition { get; set; }
    [Inject] public TimeProvider Clock { get; set; } = TimeProvider.System;
    [Inject] private IJSRuntime? Js { get; set; }

    private DueDateUrgency CurrentDueDateUrgency =>
        _workOrder == null
            ? DueDateUrgency.None
            : DueDateUrgencyCalculator.Calculate(Model.DueDateInput, _workOrder.Status, Clock);

    private string CurrentDueDateCssClass =>
        DueDateUrgencyCalculator.CssClass(CurrentDueDateUrgency);

    private string? CurrentDueDateUrgencyText =>
        DueDateUrgencyCalculator.ScreenReaderText(CurrentDueDateUrgency);

    private string CurrentStatusBadgeCssClass =>
        _workOrder == null
            ? string.Empty
            : StatusBadgeCssClassMapper.CssClass(_workOrder.Status);

    public WorkOrderManageModel Model { get; set; } = new();
    private List<SelectListItem> UserOptions { get; set; } = new();
    private IEnumerable<IStateCommand> ValidCommands { get; set; } = new List<IStateCommand>();

    private bool ShowShelveReason =>
        ValidCommands.Any(command => command.TransitionVerbPresentTense == InProgressToAssignedCommand.Name);

    private string? SelectedCommand { get; set; }
    private List<string>? _validCommandNames;

    private List<string> ValidCommandNames
    {
        get
        {
            _validCommandNames ??= ValidCommands.Select(c => c.TransitionVerbPresentTense).ToList();
            return _validCommandNames;
        }
    }

    private IEnumerable<IStateCommand> SortedValidCommands
    {
        get
        {
            return ValidCommands
                .OrderBy(c =>
                {
                    var style = CommandButtonStyleHelper.GetButtonStyle(c.TransitionVerbPresentTense, ValidCommandNames);
                    // Sort order: secondary first (0), danger (1), primary last (2)
                    return style switch
                    {
                        "btn-outline-secondary" => 0,
                        "btn-outline-danger" => 1,
                        "btn-primary" => 2,
                        _ => 0
                    };
                })
                .ToList();
        }
    }

    [Parameter] public string? Id { get; set; }

    [SupplyParameterFromQuery] public string? Mode { get; set; }
    private EditMode CurrentMode => Mode?.ToLower() == "edit" ? EditMode.Edit : EditMode.New;

    protected override async Task OnInitializedAsync()
    {
        if (SpeechRecognition != null)
        {
            SpeechRecognition.Result += OnSpeechResult;
            SpeechRecognition.End += OnSpeechEnd;
        }

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
        WorkOrder workOrder;

        if (CurrentMode == EditMode.New)
        {
            workOrder = WorkOrderBuilder!.CreateNewWorkOrder(currentUser);
            if (!string.IsNullOrEmpty(Id))
            {
                workOrder.Number = Id;
            }
        }
        else
        {
            workOrder = (await Bus.Send(new WorkOrderByNumberQuery(Id!)))!;
        }

        Model = CreateViewModel(CurrentMode, workOrder);
        var commandList = new StateCommandList();
        Model.IsReadOnly = !commandList.GetValidStateCommands(workOrder, currentUser).Any();
        ValidCommands = commandList.GetValidStateCommands(workOrder, currentUser);
        _validCommandNames = null;  // Reset cache when commands change
        _workOrder = workOrder;

        if (workOrder.Id != Guid.Empty)
        {
            _attachments = await Bus.Send(new WorkOrderAttachmentsQuery(workOrder.Id));
            _statusHistory = await Bus.Send(new WorkOrderStatusHistoryQuery(workOrder.Id));
            _canRemoveAttachments = RemoveAttachmentCommand.CanRemove(workOrder, currentUser);
        }
    }

    private void BeginRemoveAttachment(WorkOrderAttachment attachment)
    {
        _pendingRemoveId = attachment.Id;
        _removeAttachmentError = null;
    }

    private void CancelRemoveAttachment() => _pendingRemoveId = null;

    private async Task ConfirmRemoveAttachmentAsync(WorkOrderAttachment attachment)
    {
        var result = await Bus.Send(new RemoveAttachmentCommand(_workOrder!.Id, attachment.Id, _currentUserName));
        _pendingRemoveId = null;
        if (!result.Succeeded)
        {
            _removeAttachmentError = result.Error;
            return;
        }

        _removeAttachmentError = null;
        _attachments = await Bus.Send(new WorkOrderAttachmentsQuery(_workOrder.Id));
    }

    private WorkOrderManageModel CreateViewModel(EditMode mode, WorkOrder workOrder)
    {
        return new WorkOrderManageModel
        {
            WorkOrder = workOrder,
            Mode = mode,
            WorkOrderNumber = workOrder.Number,
            Status = workOrder.Status.FriendlyName,
            CreatorFullName = workOrder.Creator!.DisplayName,
            AssignedToUserName = workOrder.Assignee?.UserName,
            Title = workOrder.Title,
            Description = workOrder.Description,
            Instructions = workOrder.Instructions,
            RoomNumber = workOrder.RoomNumber,
            OriginalRoomNumber = workOrder.RoomNumber,
            CreatedDate = ClientDateFormatter.Format(workOrder.CreatedDate),
            CreatedRelativeTime = RelativeTimeFormatter.Format(workOrder.CreatedDate, Clock),
            AssignedDate = ClientDateFormatter.Format(workOrder.AssignedDate),
            CompletedDate = ClientDateFormatter.Format(workOrder.CompletedDate),
            DueDateInput = workOrder.DueDate,
            OriginalDueDate = workOrder.DueDate,
            ChurchToday = ChurchTimeZone.Today(Clock),
            Priority = workOrder.Priority,
            Building = workOrder.Building,
            Floor = workOrder.Floor,
            DueDateDisplay = ClientDateFormatter.Format(workOrder.DueDate)
        };
    }

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
        WorkOrder workOrder;

        if (Model.Mode == EditMode.New)
        {
            workOrder = WorkOrderBuilder!.CreateNewWorkOrder(currentUser);
        }
        else
        {
            workOrder = (await Bus.Send(new WorkOrderByNumberQuery(Model.WorkOrderNumber!)))!;
        }

        Employee? assignee = null;
        if (Model.AssignedToUserName != null)
        {
            assignee = await Bus.Send(new EmployeeByUserNameQuery(Model.AssignedToUserName));
        }

        workOrder.Number = Model.WorkOrderNumber;
        workOrder.Assignee = assignee;
        workOrder.Title = Model.Title;
        workOrder.Description = Model.Description;
        workOrder.Instructions = Model.Instructions;
        workOrder.RoomNumber = Model.RoomNumber;
        var previousDueDate = Model.OriginalDueDate;
        workOrder.DueDate = Model.DueDateInput;
        workOrder.Priority = Model.Priority;
        workOrder.Building = string.IsNullOrWhiteSpace(Model.Building) ? null : Model.Building.Trim();
        workOrder.Floor = string.IsNullOrWhiteSpace(Model.Floor) ? null : Model.Floor.Trim();

        var matchingCommand = CommandWithShelveReason(SaveDraftCommand.WithPreviousRoomNumber(
            new StateCommandList().GetMatchingCommand(workOrder, currentUser, SelectedCommand!),
            Model.OriginalRoomNumber));
        if (matchingCommand is SaveDraftCommand saveDraft)
        {
            matchingCommand = saveDraft with { PreviousDueDate = previousDueDate };
        }

        await SendIfReady(matchingCommand);
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

    private async Task SpeakTitleAsync()
    {
        await SpeakTextAsync(Model.Title);
    }

    private async Task SpeakDescriptionAsync()
    {
        await SpeakTextAsync(Model.Description);
    }

    private async Task SpeakTextAsync(string? text)
    {
        if (!CanSpeak(text))
        {
            return;
        }

        var translatedText = await TranslationService!.TranslateAsync(text!, _preferredLanguage);
        await SpeakTranslatedTextAsync(translatedText);
    }

    private bool CanSpeak(string? text) =>
        !string.IsNullOrEmpty(text) && SpeechSynthesis != null && TranslationService != null;

    private async Task SpeakTranslatedTextAsync(string translatedText)
    {
        try
        {
            var voices = await SpeechSynthesis!.GetVoicesAsync();
            var utterance = WorkOrderSpeechHelper.CreateUtterance(translatedText, _preferredLanguage, voices);
            await SpeechSynthesis.SpeakAsync(utterance);
        }
        catch
        {
            // Speech synthesis may not be available in all environments
        }
    }

    private async Task DictateTitleAsync()
    {
        await ToggleDictationAsync(DictationTarget.Title);
    }

    private async Task DictateDescriptionAsync()
    {
        await ToggleDictationAsync(DictationTarget.Description);
    }

    private async Task ToggleDictationAsync(DictationTarget target)
    {
        if (SpeechRecognition == null)
        {
            return;
        }

        if (_dictationTarget == target)
        {
            _dictationTarget = DictationTarget.None;
            try
            {
                await SpeechRecognition.StopAsync();
            }
            catch
            {
                // Speech recognition may not be available in all environments
            }
            return;
        }

        SpeechRecognition.Lang = _preferredLanguage;
        SpeechRecognition.Continuous = false;
        SpeechRecognition.InterimResults = false;
        _dictationTarget = target;

        try
        {
            await SpeechRecognition.StartAsync();
        }
        catch
        {
            // Speech recognition may not be available in all environments
            _dictationTarget = DictationTarget.None;
        }
    }

    private void OnSpeechResult(object? sender, SpeechRecognitionEventArgs args)
    {
        var transcripts = (args.Results ?? [])
            .Skip(args.ResultIndex)
            .Where(result => result.IsFinal)
            .Select(result => result.Items?.FirstOrDefault()?.Transcript?.Trim())
            .Where(transcript => !string.IsNullOrEmpty(transcript));
        var transcript = string.Join(" ", transcripts);

        if (string.IsNullOrEmpty(transcript))
        {
            return;
        }

        if (_dictationTarget == DictationTarget.Title)
        {
            Model.Title = AppendTranscript(Model.Title, transcript);
        }
        else if (_dictationTarget == DictationTarget.Description)
        {
            Model.Description = AppendTranscript(Model.Description, transcript);
        }

        InvokeAsync(StateHasChanged);
    }

    private void OnSpeechEnd(object? sender, EventArgs e)
    {
        _dictationTarget = DictationTarget.None;
        InvokeAsync(StateHasChanged);
    }

    private static string AppendTranscript(string? existingText, string transcript)
    {
        return string.IsNullOrEmpty(existingText) ? transcript : $"{existingText} {transcript}";
    }

    private string DictateButtonClass(DictationTarget target)
    {
        return _dictationTarget == target ? "btn btn-sm btn-danger" : "btn btn-sm btn-outline-secondary";
    }

    private string DictateAriaPressed(DictationTarget target)
    {
        return _dictationTarget == target ? "true" : "false";
    }

    private async Task CopyWorkOrderNumberAsync()
    {
        try
        {
            if (Js == null || string.IsNullOrEmpty(Model.WorkOrderNumber))
            {
                return;
            }

            _clipboardModule ??= await Js.InvokeAsync<IJSObjectReference>("import",
                "./_content/ClearMeasure.Bootcamp.UI.Shared/js/clipboard.js");

            await _clipboardModule.InvokeAsync<object>("copyToClipboard", Model.WorkOrderNumber);
            _copyButtonShowSuccess = true;
            await InvokeAsync(StateHasChanged);

            await Task.Delay(2000);
            _copyButtonShowSuccess = false;
            await InvokeAsync(StateHasChanged);
        }
        catch (JSDisconnectedException)
        {
            // Clipboard API not available or page disconnected; silently ignore
        }
        catch (JSException)
        {
            // Clipboard permission denied or other JS error; silently ignore
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (SpeechRecognition == null)
        {
            return;
        }

        SpeechRecognition.Result -= OnSpeechResult;
        SpeechRecognition.End -= OnSpeechEnd;

        try
        {
            if (_dictationTarget != DictationTarget.None)
            {
                await SpeechRecognition.StopAsync();
            }

            await SpeechRecognition.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
        }
    }

    private enum DictationTarget
    {
        None,
        Title,
        Description
    }
}

public record WorkOrderChangedEvent(StateCommandResult Result) : IUiBusEvent;
