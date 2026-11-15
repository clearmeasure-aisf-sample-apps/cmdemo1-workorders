using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class EmployeeWorkLoadQueryHandlerTests
{
    [Test]
    public async Task ShouldCountWorkLoadForEmployeeFromDatabase()
    {
        var names = await GivenWorkloadAsync();

        ThenCountsAre(await WhenQueried(names[0]), Expected(4, 2, 2, 1));
        ThenCountsAre(await WhenQueried(names[1]), Expected(0, 0, 0, 0));
    }

    private static async Task<string[]> GivenWorkloadAsync()
    {
        var today = ChurchTimeZone.Today(TimeProvider.System);
        var creator = EmployeeBuilder.Create(TestData.UserName("ewl_creator"))
            .WithFirstName("Cre").WithLastName("Ator").WithEmail("creator@test.com").Build();
        var worker = EmployeeBuilder.Create(TestData.UserName("ewl_worker"))
            .WithFirstName("Wor").WithLastName("Ker").WithEmail("worker@test.com").Build();
        var idle = EmployeeBuilder.Create(TestData.UserName("ewl_idle"))
            .WithFirstName("Id").WithLastName("Le").WithEmail("idle@test.com").Build();
        await PersistAsync(creator, worker, idle, EmployeeWorkLoadSteps.BuildWorkloadOrders(creator, worker, today));
        return [worker.UserName, idle.UserName];
    }

    private static async Task PersistAsync(
        Employee creator, Employee worker, Employee idle, WorkOrder[] orders)
    {
        await using var context = TestHost.GetRequiredService<DbContext>();
        context.AddRange(creator, worker, idle);
        context.AddRange(orders.Cast<object>());
        await context.SaveChangesAsync();
    }

    private static Task<EmployeeWorkLoad> WhenQueried(string userName)
    {
        var handler = new EmployeeWorkLoadQueryHandler(TestHost.GetRequiredService<DataContext>(), TimeProvider.System);
        return handler.Handle(new EmployeeWorkLoadQuery(userName));
    }

    private static EmployeeWorkLoad Expected(
        int assignedAndOpen, int inProgress, int overdue, int completedLast30Days) =>
        new()
        {
            AssignedAndOpen = assignedAndOpen,
            InProgress = inProgress,
            Overdue = overdue,
            CompletedLast30Days = completedLast30Days
        };

    private static void ThenCountsAre(EmployeeWorkLoad actual, EmployeeWorkLoad expected)
    {
        actual.AssignedAndOpen.ShouldBe(expected.AssignedAndOpen);
        actual.InProgress.ShouldBe(expected.InProgress);
        actual.Overdue.ShouldBe(expected.Overdue);
        actual.CompletedLast30Days.ShouldBe(expected.CompletedLast30Days);
    }
}
