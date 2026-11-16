using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Reads and creates vendors.
/// </summary>
public class VendorQueryHandler(DataContext context, CreateVendorCommandShapeRules shapeRules)
    : IRequestHandler<VendorsQuery, Vendor[]>,
        IRequestHandler<CreateVendorCommand, CreateVendorResult>
{
    /// <summary>
    /// Returns active vendors first, then inactive ones, each group ordered by name ignoring case. Sorting
    /// happens in memory so SQLite and SQL Server give the same order.
    /// </summary>
    public async Task<Vendor[]> Handle(VendorsQuery request, CancellationToken cancellationToken = default)
    {
        var vendors = await context.Set<Vendor>().ToListAsync(cancellationToken);

        return vendors
            .OrderByDescending(v => v.IsActive)
            .ThenBy(v => v.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Creates the vendor when shape validation and
    /// <see cref="CreateVendorCommand.FindRuleViolations"/> pass. Otherwise returns a Failed
    /// <see cref="CreateVendorResult"/> and writes nothing (#1824).
    /// </summary>
    public async Task<CreateVendorResult> Handle(CreateVendorCommand request,
        CancellationToken cancellationToken = default)
    {
        if (FailedResult(shapeRules.ErrorMessages(request)) is { } shapeFailure)
        {
            return shapeFailure;
        }

        var existingNames = await context.Set<Vendor>()
            .Select(v => v.Name)
            .ToListAsync(cancellationToken);
        if (FailedResult(request.FindRuleViolations(existingNames)) is { } storedFailure)
        {
            return storedFailure;
        }

        return await PersistAsync(request, cancellationToken);
    }

    private async Task<CreateVendorResult> PersistAsync(
        CreateVendorCommand request, CancellationToken cancellationToken)
    {
        var vendor = new Vendor
        {
            Name = request.Name.Trim(),
            Trade = request.Trade,
            ContactName = request.ContactName,
            EmailAddress = request.EmailAddress,
            PhoneNumber = request.PhoneNumber,
            IsActive = request.IsActive
        };
        context.Add(vendor);
        await context.SaveChangesAsync(cancellationToken);
        return new CreateVendorResult(true, $"Vendor '{vendor.Name}' created.", vendor.Id);
    }

    private static CreateVendorResult? FailedResult(IReadOnlyList<string> violations) =>
        violations.Count > 0
            ? new CreateVendorResult(false, string.Join(" ", violations), null)
            : null;
}
