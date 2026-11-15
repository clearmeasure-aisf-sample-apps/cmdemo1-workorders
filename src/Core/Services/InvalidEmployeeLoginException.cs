namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>
/// Thrown when a signed-in user name does not resolve to a valid <see cref="Model.Employee"/>.
/// </summary>
public sealed class InvalidEmployeeLoginException()
    : Exception("That user doesn't exist or is not valid.");
