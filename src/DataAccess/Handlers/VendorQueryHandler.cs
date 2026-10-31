using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Reads and creates vendors.
/// </summary>
public class VendorQueryHandler(DataContext context)
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
    /// Creates the vendor when its name is unique ignoring case. Otherwise returns the violation and writes
    /// nothing.
    /// </summary>
    public async Task<CreateVendorResult> Handle(CreateVendorCommand request,
        CancellationToken cancellationToken = default)
    {
        var existingNames = await context.Set<Vendor>()
            .Select(v => v.Name)
            .ToListAsync(cancellationToken);

        var violations = request.FindRuleViolations(existingNames);
        if (violations.Count > 0)
        {
            return new CreateVendorResult(false, string.Join(" ", violations), null);
        }

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
}
