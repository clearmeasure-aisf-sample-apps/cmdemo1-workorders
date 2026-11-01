using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class WorkOrderPastDueDateTests
{
    private static readonly DateOnly Today = new(2026, 8, 29);

    [Test]
    public void ShouldReturnTrue_WhenDueDateIsBeforeTodayAndDiffersFromPrevious()
    {
        WorkOrder.IsNewlySetPastDueDate(Today.AddDays(-1), null, Today).ShouldBeTrue();
    }

    [Test]
    public void ShouldReturnFalse_WhenDueDateIsToday()
    {
        WorkOrder.IsNewlySetPastDueDate(Today, null, Today).ShouldBeFalse();
    }

    [Test]
    public void ShouldReturnFalse_WhenDueDateIsInTheFuture()
    {
        WorkOrder.IsNewlySetPastDueDate(Today.AddDays(1), null, Today).ShouldBeFalse();
    }

    [Test]
    public void ShouldReturnFalse_WhenDueDateIsNull()
    {
        WorkOrder.IsNewlySetPastDueDate(null, null, Today).ShouldBeFalse();
    }

    [Test]
    public void ShouldReturnFalse_WhenPastDueDateIsUnchanged()
    {
        var past = Today.AddDays(-3);

        WorkOrder.IsNewlySetPastDueDate(past, past, Today).ShouldBeFalse();
    }

    [Test]
    public void ShouldReturnTrue_WhenPastDueDateChangesToAnotherPastDate()
    {
        WorkOrder.IsNewlySetPastDueDate(Today.AddDays(-1), Today.AddDays(-4), Today).ShouldBeTrue();
    }
}
