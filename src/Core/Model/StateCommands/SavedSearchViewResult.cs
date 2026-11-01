namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

/// <summary>Outcome of saving or deleting a saved search view; <see cref="Error"/> is set on failure.</summary>
public record SavedSearchViewResult(bool Succeeded, string? Error)
{
    public static SavedSearchViewResult Success { get; } = new(true, null);

    public static SavedSearchViewResult Failure(string error) => new(false, error);
}
