using ClearMeasure.Bootcamp.Core.FeatureFlags;
using ClearMeasure.Bootcamp.Core.Validation;
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
    /// In-process validation (#1824): shape rules from <c>CreateVendorCommandValidator</c> (same
    /// messages as HTTP), then duplicate name ignoring case. An empty list means the vendor can be
    /// created. Handlers map these to <see cref="CreateVendorResult"/> with <c>Succeeded</c> false —
    /// never a FluentValidation exception. HTTP still runs the envelope FluentValidator for 400.
    /// </summary>
    public IReadOnlyList<string> FindRuleViolations(IEnumerable<string> existingVendorNames)
    {
        var shape = InProcessCreateCommandShapeValidation.ErrorMessages(this);
        if (shape.Count > 0)
        {
            return shape;
        }

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
