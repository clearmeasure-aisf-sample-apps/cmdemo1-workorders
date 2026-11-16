using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.TeamsAndVendors;

/// <summary>
/// Shared arrange data for TeamsAndVendors page tests. No assertions.
/// </summary>
public static class TeamsAndVendorsSteps
{
    /// <summary>Fulfillable Homer Simpson used for team membership scenarios.</summary>
    public static Employee HomerFulfiller() =>
        EmployeeBuilder.Create("hsimpson")
            .WithFirstName("Homer")
            .WithLastName("Simpson")
            .WithEmail("homer@springfield.com")
            .WithRole("Worker", canCreate: false, canFulfill: true)
            .Build();

    /// <summary>Creator who cannot fulfill work orders (filtered from the assignable table).</summary>
    public static Employee NonAssignableCreator() =>
        EmployeeBuilder.Create("cannotassign")
            .WithFirstName("Cannot")
            .WithLastName("Assign")
            .WithEmail("cannot@example.com")
            .WithRole("Creator", canCreate: true, canFulfill: false)
            .Build();

    /// <summary>Maintenance team with a fulfillable member.</summary>
    public static Team MaintenanceTeamWithMember(Employee? member = null)
    {
        var team = new Team("Maintenance", "Maintenance team") { Id = Guid.NewGuid() };
        team.AddMember(member ?? HomerFulfiller());
        return team;
    }

    /// <summary>Named team with an optional description and one member.</summary>
    public static Team TeamWithMember(string name, string? description, Employee member)
    {
        var team = new Team(name, description) { Id = Guid.NewGuid() };
        team.AddMember(member);
        return team;
    }

    /// <summary>Active vendor with a display name only.</summary>
    public static Vendor ActiveVendor(string name = "Joe's HVAC") =>
        new() { Id = Guid.NewGuid(), Name = name, IsActive = true };

    /// <summary>Inactive vendor with a display name only.</summary>
    public static Vendor InactiveVendor(string name = "Inactive Vendor") =>
        new() { Id = Guid.NewGuid(), Name = name, IsActive = false };

    /// <summary>Active HVAC vendor with contact details filled in.</summary>
    public static Vendor DetailedHvacVendor() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Joe's HVAC",
            Trade = "HVAC",
            ContactName = "Joe Smith",
            EmailAddress = "joe@hvac.com",
            PhoneNumber = "555-1234",
            IsActive = true
        };

    /// <summary>Active vendor with an e-mail address for mailto assertions.</summary>
    public static Vendor VendorWithEmail(string email = "test@vendor.com") =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Test Vendor",
            EmailAddress = email,
            IsActive = true
        };

    /// <summary>Active vendor used when VendorManagement is disabled (should stay hidden).</summary>
    public static Vendor HiddenVendor() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Hidden Vendor",
            Trade = "HVAC",
            IsActive = true
        };
}
