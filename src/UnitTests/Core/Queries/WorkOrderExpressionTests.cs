using System.Linq.Expressions;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Queries;

[TestFixture]
public class WorkOrderExpressionTests
{
    [Test]
    public void ShouldCombinePredicates_WhenAndAlsoApplied()
    {
        var today = new DateOnly(2026, 10, 13);
        var combined = WorkOrderExpression.AndAlso(
            WorkOrderSpecifications.Open,
            wo => wo.DueDate < today);

        combined.Compile()(new WorkOrder { Status = WorkOrderStatus.Draft, DueDate = today.AddDays(-1) })
            .ShouldBeTrue();
        combined.Compile()(new WorkOrder { Status = WorkOrderStatus.Draft, DueDate = today })
            .ShouldBeFalse();
    }

    [Test]
    public void ShouldCombinePredicates_WhenOrElseApplied()
    {
        var today = new DateOnly(2026, 10, 13);
        var combined = WorkOrderExpression.OrElse(
            WorkOrderSpecifications.Overdue(today),
            WorkOrderSpecifications.DueThisWeek(today));

        combined.Compile()(new WorkOrder { Status = WorkOrderStatus.Draft, DueDate = today.AddDays(-1) })
            .ShouldBeTrue();
        combined.Compile()(new WorkOrder { Status = WorkOrderStatus.Draft, DueDate = today.AddDays(2) })
            .ShouldBeTrue();
        combined.Compile()(new WorkOrder { Status = WorkOrderStatus.Draft, DueDate = today.AddDays(14) })
            .ShouldBeFalse();
    }

    [Test]
    public void ShouldRebindOntoSharedParameter_WhenRebindApplied()
    {
        var parameter = Expression.Parameter(typeof(WorkOrder), "wo");
        Expression<Func<WorkOrder, string?>> number = order => order.Number;
        var rebound = WorkOrderExpression.Rebind(number, parameter);

        Expression.Lambda<Func<WorkOrder, string?>>(rebound, parameter)
            .Compile()(new WorkOrder { Number = "42" })
            .ShouldBe("42");
    }
}
