using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

[TestFixture]
public class DueDateUrgencyCalculatorTests
{
    [Test]
    public void Calculate_WhenDueDateNull_ReturnsNone()
    {
        var workOrder = new WorkOrder { Status = WorkOrderStatus.Assigned };
        var clock = ChurchClock.NoonUtcOn(2026, 8, 29);

        DueDateUrgencyCalculator.Calculate(workOrder, clock).ShouldBe(DueDateUrgency.None);
    }

    [Test]
    public void Calculate_WhenOpenAndDueToday_ReturnsDueToday()
    {
        var today = ChurchTimeZone.Today(ChurchClock.NoonUtcOn(2026, 8, 29));
        var workOrder = new WorkOrder
        {
            Status = WorkOrderStatus.Assigned,
            DueDate = today
        };

        var urgency = DueDateUrgencyCalculator.Calculate(workOrder, ChurchClock.NoonUtcOn(2026, 8, 29));

        urgency.ShouldBe(DueDateUrgency.DueToday);
    }

    [TestCase("Draft")]
    [TestCase("Assigned")]
    [TestCase("InProgress")]
    public void Calculate_WhenOpenAndPastDue_ReturnsOverdue(string statusKey)
    {
        var clock = ChurchClock.NoonUtcOn(2026, 8, 29);
        var workOrder = new WorkOrder
        {
            Status = WorkOrderStatus.FromKey(statusKey),
            DueDate = ChurchTimeZone.Today(clock).AddDays(-1)
        };

        DueDateUrgencyCalculator.Calculate(workOrder, clock).ShouldBe(DueDateUrgency.Overdue);
    }

    [TestCase("Complete")]
    [TestCase("Cancelled")]
    public void Calculate_WhenTerminalStatus_ReturnsNoneEvenIfPastDue(string statusKey)
    {
        var clock = ChurchClock.NoonUtcOn(2026, 8, 29);
        var workOrder = new WorkOrder
        {
            Status = WorkOrderStatus.FromKey(statusKey),
            DueDate = ChurchTimeZone.Today(clock).AddDays(-3)
        };

        DueDateUrgencyCalculator.Calculate(workOrder, clock).ShouldBe(DueDateUrgency.None);
    }

    [Test]
    public void Calculate_WhenOpenAndFutureDue_ReturnsNone()
    {
        var clock = ChurchClock.NoonUtcOn(2026, 8, 29);
        var workOrder = new WorkOrder
        {
            Status = WorkOrderStatus.Draft,
            DueDate = ChurchTimeZone.Today(clock).AddDays(7)
        };

        DueDateUrgencyCalculator.Calculate(workOrder, clock).ShouldBe(DueDateUrgency.None);
    }

    [Test]
    public void Calculate_WhenEditableDueDateChanges_UpdatesUrgency()
    {
        var clock = ChurchClock.NoonUtcOn(2026, 8, 29);
        var today = ChurchTimeZone.Today(clock);

        var futureUrgency = DueDateUrgencyCalculator.Calculate(
            today.AddDays(1),
            WorkOrderStatus.Draft,
            clock);
        var todayUrgency = DueDateUrgencyCalculator.Calculate(
            today,
            WorkOrderStatus.Draft,
            clock);

        futureUrgency.ShouldBe(DueDateUrgency.None);
        todayUrgency.ShouldBe(DueDateUrgency.DueToday);
    }

    [Test]
    public void Calculate_WhenChurchCalendarSupplied_UsesItsToday()
    {
        var clock = ChurchClock.NoonUtcOn(2026, 8, 29);
        var calendar = new ChurchCalendar(clock);
        var workOrder = WorkOrderBuilder.Create()
            .WithStatus(WorkOrderStatus.Assigned)
            .DueOn(calendar.Today.AddDays(-1))
            .Build();

        DueDateUrgencyCalculator.Calculate(workOrder, calendar).ShouldBe(DueDateUrgency.Overdue);
    }

    [Test]
    public void Calculate_WhenChurchTodayGivenDirectly_UpdatesUrgency()
    {
        var clock = ChurchClock.NoonUtcOn(2026, 8, 29);
        var today = ChurchTimeZone.Today(clock);

        var futureUrgency = DueDateUrgencyCalculator.Calculate(today.AddDays(1), WorkOrderStatus.Draft, today);
        var todayUrgency = DueDateUrgencyCalculator.Calculate(today, WorkOrderStatus.Draft, today);
        var overdueUrgency = DueDateUrgencyCalculator.Calculate(today.AddDays(-1), WorkOrderStatus.Draft, today);

        futureUrgency.ShouldBe(DueDateUrgency.None);
        todayUrgency.ShouldBe(DueDateUrgency.DueToday);
        overdueUrgency.ShouldBe(DueDateUrgency.Overdue);
    }
}
