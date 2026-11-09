namespace ClearMeasure.Bootcamp.UI.Shared.Models;

/// <summary>
/// Which result rows (by work order number) are selected. Select-all works on the current page;
/// the page header checkbox is indeterminate when only part of the page is selected.
/// </summary>
public sealed class WorkOrderRowSelection
{
    private readonly HashSet<string> _numbers = new(StringComparer.Ordinal);

    /// <summary>Selected work order numbers.</summary>
    public IReadOnlyCollection<string> Numbers => _numbers;

    public int Count => _numbers.Count;

    public bool IsSelected(string number) => _numbers.Contains(number);

    public void Toggle(string number)
    {
        if (!_numbers.Remove(number))
        {
            _numbers.Add(number);
        }
    }

    /// <summary>Selects every row of the page, or clears them when all were already selected.</summary>
    public void TogglePage(IEnumerable<string> pageNumbers)
    {
        var page = pageNumbers.ToArray();
        if (AllSelected(page))
        {
            _numbers.ExceptWith(page);
            return;
        }

        _numbers.UnionWith(page);
    }

    /// <summary>True when the page has rows and all of them are selected.</summary>
    public bool AllSelected(IReadOnlyCollection<string> pageNumbers) =>
        pageNumbers.Count > 0 && pageNumbers.All(_numbers.Contains);

    /// <summary>True when some, but not all, rows of the page are selected.</summary>
    public bool IsIndeterminate(IReadOnlyCollection<string> pageNumbers) =>
        pageNumbers.Any(_numbers.Contains) && !AllSelected(pageNumbers);

    public void Clear() => _numbers.Clear();
}
