using ClearMeasure.Bootcamp.Core.FeatureFlags;
using MediatR;

// ReSharper disable NotAccessedPositionalProperty.Global -- Message and Id are read by the vendor UI (later item) and by tests
namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Creates a vendor. Requires <see cref="FeatureFlagNames.VendorManagement"/>.
/// </summary>
[RequiresFeature(FeatureFlagNames.VendorManagement)]
public record CreateVendorCommand(
    string Name,
    string? Trade,
    string? ContactName,
    string? EmailAddress,
    string? PhoneNumber,
    bool IsActive = true) : IRequest<CreateVendorResult>, IRemotableRequest
{
    /// <summary>
    /// Stored-data rules for in-process create (#1824): duplicate name ignoring case. Shape rules
    /// run in the handler via the injected <c>CreateVendorCommandValidator</c>. An empty list means
    /// the vendor can be created. Handlers map violations to <see cref="CreateVendorResult"/> with
    /// <c>Succeeded</c> false — never a FluentValidation exception. HTTP still runs the envelope
    /// FluentValidator for 400.
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
