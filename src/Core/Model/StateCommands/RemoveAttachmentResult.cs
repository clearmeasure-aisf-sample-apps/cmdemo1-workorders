namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

/// <summary>Outcome of <see cref="RemoveAttachmentCommand"/>: success, or the reason it was refused.</summary>
public record RemoveAttachmentResult(bool Succeeded, string? Error)
{
    public static RemoveAttachmentResult Success { get; } = new(true, null);

    public static RemoveAttachmentResult Failure(string error) => new(false, error);
}
