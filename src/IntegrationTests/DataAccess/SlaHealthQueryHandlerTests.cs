using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

[TestFixture]
public class SlaHealthQueryHandlerTests
{
    [Test]
    public async Task Should_ReturnNullPercentage_When_NoCompletedOrders()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("testuser", "Test", "User", "test@test.com");
        var order = new WorkOrder
        {
            Creator = creator,
            Number = "WO-001",
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = WorkOrderStatus.Draft
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(order);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new SlaHealthQueryHandler(dataContext, TimeProvider.System);
        var result = await handler.Handle(new SlaHealthQuery());

        result.CurrentMonthPercentage.ShouldBeNull();
    }

    [Test]
    public async Task Should_ExcludeOrders_Without_DueDate()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("testuser", "Test", "User", "test@test.com");
        var today = ChurchTimeZone.Today(TimeProvider.System);
        var completedOrder = new WorkOrder
        {
            Creator = creator,
            Number = "WO-001",
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
        var handler = new SlaHealthQueryHandler(dataContext, TimeProvider.System);
        var result = await handler.Handle(new SlaHealthQuery());

        result.CurrentMonthPercentage.ShouldBeNull();
    }

    [Test]
    public async Task Should_CountOnTimeCompletion_When_CompletedDateEqualsOrBeforeDueDate()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("testuser", "Test", "User", "test@test.com");
        var today = ChurchTimeZone.Today(TimeProvider.System);
        var dueDate = today;

        var onTimeOrder = new WorkOrder
        {
            Creator = creator,
            Number = "WO-001",
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
        var handler = new SlaHealthQueryHandler(dataContext, TimeProvider.System);
        var result = await handler.Handle(new SlaHealthQuery());

        result.CurrentMonthPercentage.ShouldBe(100m);
        result.CurrentMonthOrderCount.ShouldBe(1);
    }

    [Test]
    public async Task Should_CountLateCompletion_As_NotOnTime()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("testuser", "Test", "User", "test@test.com");
        var today = ChurchTimeZone.Today(TimeProvider.System);
        var dueDate = today.AddDays(-1);

        var lateOrder = new WorkOrder
        {
            Creator = creator,
            Number = "WO-001",
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
        var handler = new SlaHealthQueryHandler(dataContext, TimeProvider.System);
        var result = await handler.Handle(new SlaHealthQuery());

        result.CurrentMonthPercentage.ShouldBe(0m);
        result.CurrentMonthOrderCount.ShouldBe(1);
    }

    [Test]
    public async Task Should_CalculatePercentage_With_MixedOnTimeAndLate()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("testuser", "Test", "User", "test@test.com");
        var today = ChurchTimeZone.Today(TimeProvider.System);

        var onTimeOrder = new WorkOrder
        {
            Creator = creator,
            Number = "WO-001",
            DueDate = today,
            Status = WorkOrderStatus.Complete,
            CompletedDate = today.ToDateTime(new TimeOnly(12, 0))
        };

        var lateOrder = new WorkOrder
        {
            Creator = creator,
            Number = "WO-002",
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
        var handler = new SlaHealthQueryHandler(dataContext, TimeProvider.System);
        var result = await handler.Handle(new SlaHealthQuery());

        result.CurrentMonthPercentage.ShouldBe(50m);
        result.CurrentMonthOrderCount.ShouldBe(2);
    }

    [Test]
    public async Task Should_SeparateMetrics_For_CurrentAndLastMonth()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("testuser", "Test", "User", "test@test.com");
        var today = ChurchTimeZone.Today(TimeProvider.System);
        var lastMonth = today.AddMonths(-1);

        var currentMonthOrder = new WorkOrder
        {
            Creator = creator,
            Number = "WO-001",
            DueDate = today,
            Status = WorkOrderStatus.Complete,
            CompletedDate = today.ToDateTime(new TimeOnly(12, 0))
        };

        var lastMonthOrder = new WorkOrder
        {
            Creator = creator,
            Number = "WO-002",
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
        var handler = new SlaHealthQueryHandler(dataContext, TimeProvider.System);
        var result = await handler.Handle(new SlaHealthQuery());

        result.CurrentMonthPercentage.ShouldBe(100m);
        result.CurrentMonthOrderCount.ShouldBe(1);
        result.LastMonthPercentage.ShouldBe(100m);
        result.LastMonthOrderCount.ShouldBe(1);
    }

    [Test]
    public async Task Should_FilterByCompletionMonth_NotDueMonth()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("testuser", "Test", "User", "test@test.com");
        var today = ChurchTimeZone.Today(TimeProvider.System);
        var nextMonth = today.AddMonths(1);

        // Order due next month but completed today
        var order = new WorkOrder
        {
            Creator = creator,
            Number = "WO-001",
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
        var handler = new SlaHealthQueryHandler(dataContext, TimeProvider.System);
        var result = await handler.Handle(new SlaHealthQuery());

        // Should be counted in current month (when it was completed), and it's on time
        result.CurrentMonthPercentage.ShouldBe(100m);
        result.CurrentMonthOrderCount.ShouldBe(1);
    }
}
