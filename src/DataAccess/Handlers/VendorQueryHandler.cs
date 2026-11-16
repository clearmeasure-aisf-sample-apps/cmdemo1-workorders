using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Reads and creates vendors.
/// </summary>
public class VendorQueryHandler(DataContext context)
    : IRequestHandler<VendorsQuery, Vendor[]>,
        IRequestHandler<CreateVendorCommand, CreateVendorResult>
{
    // A field, not the primary-constructor parameter, so EF 11 can precompile the queries for Native AOT.
    // ReSharper disable once ReplaceWithPrimaryConstructorParameter
    private readonly DataContext _context = context;

    /// <summary>
    /// Returns active vendors first, then inactive ones, each group ordered by name ignoring case. Sorting
    /// happens in memory so SQLite and SQL Server give the same order.
    /// </summary>
    public async ValueTask<Vendor[]> Handle(VendorsQuery request, CancellationToken cancellationToken = default)
    {
        var ct = cancellationToken;
        var vendors = await _context.Set<Vendor>().ToListAsync(ct);

        return vendors
            .OrderByDescending(v => v.IsActive)
            .ThenBy(v => v.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Creates the vendor when its name is unique ignoring case. Otherwise returns the violation and writes
    /// nothing.
    /// </summary>
    public async ValueTask<CreateVendorResult> Handle(CreateVendorCommand request,
        CancellationToken cancellationToken = default)
    {
        var ct = cancellationToken;
        var existingNames = await _context.Set<Vendor>()
            .Select(v => v.Name)
            .ToListAsync(ct);

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
        _context.Add(vendor);
        await _context.SaveChangesAsync(cancellationToken);

        return new CreateVendorResult(true, $"Vendor '{vendor.Name}' created.", vendor.Id);
    }
}
