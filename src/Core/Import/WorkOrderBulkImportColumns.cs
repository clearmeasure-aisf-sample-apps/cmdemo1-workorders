namespace ClearMeasure.Bootcamp.Core.Import;

/// <summary>
/// The header names of the work order bulk-import CSV columns, named once for the parser and its messages.
/// </summary>
public static class WorkOrderBulkImportColumns
{
    /// <summary>The work order title column (required).</summary>
    public const string Title = "Title";

    /// <summary>The work order description column (required).</summary>
    public const string Description = "Description";

    /// <summary>The creator's username column (required).</summary>
    public const string CreatorUsername = "CreatorUsername";

    /// <summary>The room number column (optional).</summary>
    public const string RoomNumber = "RoomNumber";

    /// <summary>The building column (optional).</summary>
    public const string Building = "Building";

    /// <summary>The priority column (optional).</summary>
    public const string Priority = "Priority";

    /// <summary>The due date column (optional, yyyy-MM-dd).</summary>
    public const string DueDate = "DueDate";

    /// <summary>The columns every CSV header must name, in their expected order.</summary>
    public static IReadOnlyList<string> Required { get; } = [Title, Description, CreatorUsername];

    /// <summary>The columns a CSV header may name, in their expected order.</summary>
    public static IReadOnlyList<string> Optional { get; } = [RoomNumber, Building, Priority, DueDate];

    /// <summary>
    /// The expected header sentence, e.g. "Title, Description, CreatorUsername, RoomNumber (optional), ...".
    /// </summary>
    public static string ExpectedHeader { get; } =
        string.Join(", ", Required.Concat(Optional.Select(name => $"{name} (optional)")));
}
