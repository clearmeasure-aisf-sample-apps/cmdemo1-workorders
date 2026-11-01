using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.IntegrationTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

[TestFixture]
public class SlaHealthQueryHandlerTests
{
    [Test]
    public async Task Should_ReturnNullPercentage_When_NoCompletedOrders()
    {
        var clock = TestData.FutureClock();

        var userName = TestData.UserName("sla");
        var creator = new Employee(userName, "Test", "User", TestData.Email(userName));
        var order = new WorkOrder
        {
            Creator = creator,
            Number = TestData.Number(),
            DueDate = ChurchTimeZone.Today(clock),
            Status = WorkOrderStatus.Draft
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(order);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new SlaHealthQueryHandler(dataContext, clock);
        var result = await handler.Handle(new SlaHealthQuery());

        result.CurrentMonthPercentage.ShouldBeNull();
    }

    [Test]
    public async Task Should_ExcludeOrders_Without_DueDate()
    {
        var clock = TestData.FutureClock();

        var userName = TestData.UserName("sla");
        var creator = new Employee(userName, "Test", "User", TestData.Email(userName));
        var today = ChurchTimeZone.Today(clock);
        var completedOrder = new WorkOrder
        {
            Creator = creator,
            Number = TestData.Number(),
            DueDate = null,
            Status = WorkOrderStatus.Complete,
            CompletedDate = today.ToDateTime(new TimeOnly(12, 0))
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(completedOrder);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new SlaHealthQueryHandler(dataContext, clock);
        var result = await handler.Handle(new SlaHealthQuery());

        result.CurrentMonthPercentage.ShouldBeNull();
    }

    [Test]
    public async Task Should_CountOnTimeCompletion_When_CompletedDateEqualsOrBeforeDueDate()
    {
        var clock = TestData.FutureClock();

        var userName = TestData.UserName("sla");
        var creator = new Employee(userName, "Test", "User", TestData.Email(userName));
        var today = ChurchTimeZone.Today(clock);
        var dueDate = today;

        var onTimeOrder = new WorkOrder
        {
            Creator = creator,
            Number = TestData.Number(),
            DueDate = dueDate,
            Status = WorkOrderStatus.Complete,
            CompletedDate = dueDate.ToDateTime(new TimeOnly(12, 0))
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(onTimeOrder);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new SlaHealthQueryHandler(dataContext, clock);
        var result = await handler.Handle(new SlaHealthQuery());

        result.CurrentMonthPercentage.ShouldBe(100m);
        result.CurrentMonthOrderCount.ShouldBe(1);
    }

    [Test]
    public async Task Should_CountLateCompletion_As_NotOnTime()
    {
        var clock = TestData.FutureClock();

        var userName = TestData.UserName("sla");
        var creator = new Employee(userName, "Test", "User", TestData.Email(userName));
        var today = ChurchTimeZone.Today(clock);
        var dueDate = today.AddDays(-1);

        var lateOrder = new WorkOrder
        {
            Creator = creator,
            Number = TestData.Number(),
            DueDate = dueDate,
            Status = WorkOrderStatus.Complete,
            CompletedDate = today.ToDateTime(new TimeOnly(12, 0))
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(lateOrder);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new SlaHealthQueryHandler(dataContext, clock);
        var result = await handler.Handle(new SlaHealthQuery());

        result.CurrentMonthPercentage.ShouldBe(0m);
        result.CurrentMonthOrderCount.ShouldBe(1);
    }

    [Test]
    public async Task Should_CalculatePercentage_With_MixedOnTimeAndLate()
    {
        var clock = TestData.FutureClock();

        var userName = TestData.UserName("sla");
        var creator = new Employee(userName, "Test", "User", TestData.Email(userName));
        var today = ChurchTimeZone.Today(clock);

        var onTimeOrder = new WorkOrder
        {
            Creator = creator,
            Number = TestData.Number(),
            DueDate = today,
            Status = WorkOrderStatus.Complete,
            CompletedDate = today.ToDateTime(new TimeOnly(12, 0))
        };

        var lateOrder = new WorkOrder
        {
            Creator = creator,
            Number = TestData.Number(),
            DueDate = today.AddDays(-1),
            Status = WorkOrderStatus.Complete,
            CompletedDate = today.ToDateTime(new TimeOnly(12, 0))
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(onTimeOrder);
            context.Add(lateOrder);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new SlaHealthQueryHandler(dataContext, clock);
        var result = await handler.Handle(new SlaHealthQuery());

        result.CurrentMonthPercentage.ShouldBe(50m);
        result.CurrentMonthOrderCount.ShouldBe(2);
    }

    [Test]
    public async Task Should_SeparateMetrics_For_CurrentAndLastMonth()
    {
        var clock = TestData.FutureClock();

        var userName = TestData.UserName("sla");
        var creator = new Employee(userName, "Test", "User", TestData.Email(userName));
        var today = ChurchTimeZone.Today(clock);
        var lastMonth = today.AddMonths(-1);

        var currentMonthOrder = new WorkOrder
        {
            Creator = creator,
            Number = TestData.Number(),
            DueDate = today,
            Status = WorkOrderStatus.Complete,
            CompletedDate = today.ToDateTime(new TimeOnly(12, 0))
        };

        var lastMonthOrder = new WorkOrder
        {
            Creator = creator,
            Number = TestData.Number(),
            DueDate = lastMonth,
            Status = WorkOrderStatus.Complete,
            CompletedDate = lastMonth.ToDateTime(new TimeOnly(12, 0))
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(currentMonthOrder);
            context.Add(lastMonthOrder);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new SlaHealthQueryHandler(dataContext, clock);
        var result = await handler.Handle(new SlaHealthQuery());

        result.CurrentMonthPercentage.ShouldBe(100m);
        result.CurrentMonthOrderCount.ShouldBe(1);
        result.LastMonthPercentage.ShouldBe(100m);
        result.LastMonthOrderCount.ShouldBe(1);
    }

    [Test]
    public async Task Should_FilterByCompletionMonth_NotDueMonth()
    {
        var clock = TestData.FutureClock();

        var userName = TestData.UserName("sla");
        var creator = new Employee(userName, "Test", "User", TestData.Email(userName));
        var today = ChurchTimeZone.Today(clock);
        var nextMonth = today.AddMonths(1);

        // Order due next month but completed today
        var order = new WorkOrder
        {
            Creator = creator,
            Number = TestData.Number(),
            DueDate = nextMonth,
            Status = WorkOrderStatus.Complete,
            CompletedDate = today.ToDateTime(new TimeOnly(12, 0))
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(order);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new SlaHealthQueryHandler(dataContext, clock);
        var result = await handler.Handle(new SlaHealthQuery());

        // Should be counted in current month (when it was completed), and it's on time
        result.CurrentMonthPercentage.ShouldBe(100m);
        result.CurrentMonthOrderCount.ShouldBe(1);
    }
}
