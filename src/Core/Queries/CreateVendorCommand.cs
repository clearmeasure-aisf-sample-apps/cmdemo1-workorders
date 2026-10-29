using MediatR;

// ReSharper disable NotAccessedPositionalProperty.Global -- Message and Id are read by the vendor UI (later item) and by tests
namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Creates a vendor.
/// </summary>
public record CreateVendorCommand(
    string Name,
    string? Trade,
    string? ContactName,
    string? EmailAddress,
    string? PhoneNumber,
    bool IsActive = true) : IRequest<CreateVendorResult>, IRemotableRequest
{
    /// <summary>
    /// Returns the rules this command breaks given the data already stored: a vendor name that matches an
    /// existing one ignoring case. An empty list means the vendor can be created.
    /// </summary>
    public IReadOnlyList<string> FindRuleViolations(IEnumerable<string> existingVendorNames)
    {
        var name = Name.Trim();
        return existingVendorNames.Contains(name, StringComparer.OrdinalIgnoreCase)
            ? [$"A vendor named '{name}' already exists."]
            : [];
    }
}

/// <summary>
/// Result of <see cref="CreateVendorCommand"/>. When <see cref="Succeeded"/> is false nothing was written and
/// <see cref="Message"/> lists the rules that were broken.
/// </summary>
public record CreateVendorResult(bool Succeeded, string Message, Guid? Id);
