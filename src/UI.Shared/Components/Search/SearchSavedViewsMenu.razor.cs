using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace ClearMeasure.Bootcamp.UI.Shared.Components.Search;

/// <summary>
/// The Saved views menu of the search page: the preset views, the user's own views (open or delete), and a form
/// that saves the current search under a name.
/// </summary>
public partial class SearchSavedViewsMenu
{
    private static readonly (string Name, string Href)[] PresetViews =
    [
        ("Open work orders", WorkOrderSearchUrlState.ForCard(SummaryCard.Open).ToRelativeUri()),
        ("Assigned to me", WorkOrderSearchUrlState.ForCard(SummaryCard.AssignedToMe).ToRelativeUri()),
        ("Overdue", WorkOrderSearchUrlState.ForCard(SummaryCard.Overdue).ToRelativeUri()),
        ("Completed in last 30 days", WorkOrderSearchUrlState.ForCard(SummaryCard.Completed).ToRelativeUri())
    ];

    private bool _open;
    private bool _saveFormOpen;
    private bool _focusNameInput;
    private string _newName = string.Empty;
    private string? _saveError;
    private string? _deleteError;
    private SavedSearchView[] _userViews = [];
    private string? _loadedFor;
    private ElementReference _button;
    private ElementReference _nameInput;

    /// <summary>The signed-in user, whose views are listed; no views are listed or saved without one.</summary>
    [Parameter] public string? CurrentUserName { get; set; }

    /// <summary>The query string of the search on screen; "Save current view" saves it.</summary>
    [Parameter, EditorRequired] public string CurrentQuery { get; set; } = string.Empty;

    protected override async Task OnParametersSetAsync()
    {
        if (CurrentUserName != _loadedFor)
        {
            _loadedFor = CurrentUserName;
            await LoadUserViewsAsync();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_focusNameInput || !_saveFormOpen)
        {
            return;
        }

        _focusNameInput = false;
        await _nameInput.FocusAsync();
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key != "Escape" || (!_open && !_saveFormOpen))
        {
            return;
        }

        Close();
        await _button.FocusAsync();
    }

    private void Toggle()
    {
        if (_open)
        {
            Close();
            return;
        }

        _open = true;
    }

    private void Close()
    {
        _open = false;
        _saveFormOpen = false;
        _saveError = null;
        _newName = string.Empty;
    }

    private async Task LoadUserViewsAsync()
    {
        if (CurrentUserName is null)
        {
            return;
        }

        try
        {
            _userViews = await Bus.Send(new SavedSearchViewsQuery(CurrentUserName));
        }
        catch (Exception)
        {
            // Saved views are an optional extra: a failure loading them (for example a database not yet
            // upgraded with the SavedSearchView table) must never break the search page.
            _userViews = [];
        }
    }

    private void OpenSaveForm()
    {
        _saveFormOpen = true;
        _saveError = null;
        _newName = string.Empty;
        _focusNameInput = true;
    }

    private async Task CancelSaveForm()
    {
        _saveFormOpen = false;
        _saveError = null;
        _newName = string.Empty;
        await _button.FocusAsync();
    }

    private async Task HandleSave()
    {
        var error = SavedSearchView.ValidateNew(_newName, CurrentQuery, _userViews.Select(v => v.Name).ToArray());
        if (error is not null || CurrentUserName is null)
        {
            _saveError = error ?? "Sign in to save a view.";
            return;
        }

        _saveError = await SaveAsync(CurrentUserName);
        if (_saveError is null)
        {
            await LoadUserViewsAsync();
            Close();
        }
    }

    /// <summary>Saves the current query under the typed name; the error to show, or null when it was saved.</summary>
    private async Task<string?> SaveAsync(string userName)
    {
        try
        {
            var result = await Bus.Send(new SaveSearchViewCommand(userName, _newName.Trim(), CurrentQuery));
            return result.Succeeded ? null : result.Error ?? "The view could not be saved.";
        }
        catch (Exception)
        {
            return "The view could not be saved. Please try again later.";
        }
    }

    private async Task HandleDelete(SavedSearchView view)
    {
        if (CurrentUserName is null)
        {
            return;
        }

        try
        {
            var result = await Bus.Send(new DeleteSearchViewCommand(CurrentUserName, view.Id));
            if (result.Succeeded)
            {
                await LoadUserViewsAsync();
                _deleteError = null;
            }
            else
            {
                _deleteError = result.Error ?? "The view could not be deleted.";
            }
        }
        catch (Exception)
        {
            _deleteError = "The view could not be deleted. Please try again later.";
        }
    }
}
