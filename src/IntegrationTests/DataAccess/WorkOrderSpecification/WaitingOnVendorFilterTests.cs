using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.WorkOrderSpecification;

[TestFixture]
public class WaitingOnVendorFilterTests
{
    [Test]
    public async Task ShouldReturnWaitingOnVendorOrder_WhenOpenOnly()
    {
        var (employee, numbers) = await WorkOrderSpecificationSteps.SeedWaitingOnVendorOrdersAsync();

        var specification = new WorkOrderSpecificationQuery();
        specification.MatchOpenOnly(true);
        specification.MatchCreator(employee);
        var orders = await WorkOrderQuerySteps.SearchAsync(specification);

        orders.Select(o => o.Number).ShouldBe([numbers[0], numbers[1]], ignoreOrder: true);
    }

    [Test]
    public async Task ShouldReturnWaitingOnVendorOrder_WhenStatusIsInProgress()
    {
        var (employee, numbers) = await WorkOrderSpecificationSteps.SeedWaitingOnVendorOrdersAsync();

        var specification = new WorkOrderSpecificationQuery();
        specification.MatchStatus(WorkOrderStatus.InProgress);
        specification.MatchCreator(employee);
        var orders = await WorkOrderQuerySteps.SearchAsync(specification);

        orders.Select(o => o.Number).ShouldBe([numbers[0], numbers[1]], ignoreOrder: true);
    }

    [Test]
    public async Task ShouldReturnOnlyWaitingOnVendorOrders_WhenWaitingOnVendorOnly()
    {
        var (employee, numbers) = await WorkOrderSpecificationSteps.SeedWaitingOnVendorOrdersAsync();

        var specification = new WorkOrderSpecificationQuery();
        specification.MatchWaitingOnVendorOnly(true);
        specification.MatchCreator(employee);
        var orders = await WorkOrderQuerySteps.SearchAsync(specification);

        orders.Select(o => o.Number).ShouldBe([numbers[0]]);
    }
}
