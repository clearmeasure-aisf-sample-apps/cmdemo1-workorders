using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using MediatR;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.TeamsAndVendors;

/// <summary>
/// Stub bus for TeamsAndVendors tests. The page loads every assignable employee's workload
/// with one <see cref="EmployeeWorkLoadSummaryQuery"/>.
/// </summary>
public sealed class TeamsAndVendorsBus : StubBus
{
    /// <summary>Captured <see cref="EmployeeWorkLoadQuery"/> sends.</summary>
    public List<EmployeeWorkLoadQuery> EmployeeWorkLoadQueriesSent { get; } = new();

    /// <summary>How many <see cref="EmployeeWorkLoadSummaryQuery"/> hits occurred.</summary>
    public int SummaryQueriesSent { get; private set; }

    /// <summary>How many <see cref="EmployeeGetAllQuery"/> hits occurred.</summary>
    public int EmployeeGetAllQueriesSent { get; private set; }

    /// <summary>How many <see cref="VendorsQuery"/> hits occurred.</summary>
    public int VendorsQueriesSent { get; private set; }

    /// <summary>Assignable employees returned by get-all and summary queries.</summary>
    public Employee[] EmployeesToReturn
    {
        get => field ??= CreateDefaultEmployees();
        set;
    }

    /// <summary>Teams returned by <see cref="TeamsQuery"/>.</summary>
    public Team[] TeamsToReturn { get; init; } = [];

    /// <summary>Vendors returned by <see cref="VendorsQuery"/>.</summary>
    public Vendor[] VendorsToReturn { get; init; } = [];

    /// <summary>Optional gate awaited before handling any request (loading-state tests).</summary>
    public Task? Pending { get; init; }

    /// <inheritdoc />
    public override async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        if (Pending is not null)
        {
            await Pending;
        }

        return request switch
        {
            EmployeeGetAllQuery => HandleEmployeeGetAll<TResponse>(),
            EmployeeWorkLoadSummaryQuery => HandleWorkLoadSummary<TResponse>(),
            EmployeeWorkLoadQuery workLoadQuery => HandleWorkLoad<TResponse>(workLoadQuery),
            TeamsQuery => HandleTeams<TResponse>(),
            VendorsQuery => HandleVendors<TResponse>(),
            _ => await base.Send(request)
        };
    }

    private TResponse HandleEmployeeGetAll<TResponse>()
    {
        EmployeeGetAllQueriesSent++;
        return (TResponse)(object)EmployeesToReturn;
    }

    private TResponse HandleWorkLoadSummary<TResponse>()
    {
        SummaryQueriesSent++;
        var summaries = EmployeesToReturn
            .Select(employee => new EmployeeWorkLoadSummary
            {
                Employee = employee,
                Load = SampleLoad()
            })
            .ToArray();
        return (TResponse)(object)summaries;
    }

    private TResponse HandleWorkLoad<TResponse>(EmployeeWorkLoadQuery workLoadQuery)
    {
        EmployeeWorkLoadQueriesSent.Add(workLoadQuery);
        return (TResponse)(object)SampleLoad();
    }

    private TResponse HandleTeams<TResponse>() => (TResponse)(object)TeamsToReturn;

    private TResponse HandleVendors<TResponse>()
    {
        VendorsQueriesSent++;
        return (TResponse)(object)VendorsToReturn;
    }

    private static EmployeeWorkLoad SampleLoad() =>
        new() { AssignedAndOpen = 5, InProgress = 2, Overdue = 1, CompletedLast30Days = 10 };

    private static Employee[] CreateDefaultEmployees() =>
    [
        EmployeeBuilder.Create("hsimpson")
            .WithFirstName("HOMER")
            .WithLastName("SIMPSON")
            .WithEmail("homer@springfield.com")
            .WithRole("Worker", canCreate: false, canFulfill: true)
            .Build(),
        EmployeeBuilder.Create("mburns")
            .WithFirstName("Montgomery")
            .WithLastName("Burns")
            .WithEmail("burns@plant.com")
            .WithRole("Manager", canCreate: true, canFulfill: true)
            .Build(),
        EmployeeBuilder.Create("nflanders")
            .WithFirstName("Ned")
            .WithLastName("Flanders")
            .WithEmail("ned@flanders.com")
            .WithRole("Worker", canCreate: false, canFulfill: true)
            .Build()
    ];
}
