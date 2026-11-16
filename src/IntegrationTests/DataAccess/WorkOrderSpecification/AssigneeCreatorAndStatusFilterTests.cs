using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.WorkOrderSpecification;

[TestFixture]
public class AssigneeCreatorAndStatusFilterTests
{
    [Test]
    public void ShouldHandleRemotedQuery()
    {
        var query = new WorkOrderSpecificationQuery();
        query.ShouldNotBeNull();
    }

    [Test]
    public async Task ShouldSearchBySpecificationWithAssignee()
    {
        var number123 = TestData.Number("S");
        var number456 = TestData.Number("S");
        var employee1 = EmployeeBuilder.Create(TestData.UserName("employee1"))
            .WithFirstName("1").WithLastName("1").WithEmail("1").Build();
        var employee2 = EmployeeBuilder.Create(TestData.UserName("employee2"))
            .WithFirstName("2").WithLastName("2").WithEmail("2").Build();
        var order1 = WorkOrderBuilder.For(employee2, WorkOrderStatus.Draft)
            .AssignedTo(employee1).WithNumber(number123).Build();
        var order2 = WorkOrderBuilder.For(employee1, WorkOrderStatus.Draft)
            .AssignedTo(employee2).WithNumber(number456).Build();
        await WorkOrderQuerySteps.SaveOrdersAsync(employee2, order1, order2);

        var specification = new WorkOrderSpecificationQuery();
        specification.MatchAssignee(employee1);
        var orders = await WorkOrderQuerySteps.SearchAsync(specification);

        orders.Length.ShouldBe(1);
        orders[0].Id.ShouldBe(order1.Id);
    }

    [Test]
    public async Task ShouldSearchBySpecificationWithCreator()
    {
        var number123 = TestData.Number("S");
        var number456 = TestData.Number("S");
        var creator1 = EmployeeBuilder.Create(TestData.UserName("employee1"))
            .WithFirstName("1").WithLastName("1").WithEmail("1").Build();
        var creator2 = EmployeeBuilder.Create(TestData.UserName("employee2"))
            .WithFirstName("2").WithLastName("2").WithEmail("2").Build();
        var order1 = WorkOrderBuilder.For(creator1, WorkOrderStatus.Draft).WithNumber(number123).Build();
        var order2 = WorkOrderBuilder.For(creator2, WorkOrderStatus.Draft).WithNumber(number456).Build();
        await WorkOrderQuerySteps.SaveOrdersAsync(creator1, order1);
        await WorkOrderQuerySteps.SaveOrdersAsync(creator2, order2);

        var specification = new WorkOrderSpecificationQuery();
        specification.MatchCreator(creator1);
        var orders = await WorkOrderQuerySteps.SearchAsync(specification);

        orders.Length.ShouldBe(1);
        orders[0].Id.ShouldBe(order1.Id);
    }

    [Test]
    public async Task ShouldSearchBySpecificationWithFullSpecification()
    {
        var number123 = TestData.Number("S");
        var number456 = TestData.Number("S");
        var employee1 = EmployeeBuilder.Create(TestData.UserName("employee1"))
            .WithFirstName("1").WithLastName("1").WithEmail("1").Build();
        var employee2 = EmployeeBuilder.Create(TestData.UserName("employee2"))
            .WithFirstName("2").WithLastName("2").WithEmail("2").Build();
        var order1 = WorkOrderBuilder.For(employee2, WorkOrderStatus.Assigned)
            .AssignedTo(employee1).WithNumber(number123).Build();
        var order2 = WorkOrderBuilder.For(employee1, WorkOrderStatus.Draft)
            .AssignedTo(employee2).WithNumber(number456).Build();
        await WorkOrderQuerySteps.SaveOrdersAsync(employee2, order1, order2);

        var specification = new WorkOrderSpecificationQuery();
        specification.MatchStatus(WorkOrderStatus.Assigned);
        specification.MatchCreator(employee2);
        specification.MatchAssignee(employee1);
        var orders = await WorkOrderQuerySteps.SearchAsync(specification);

        orders.Length.ShouldBe(1);
        orders[0].Id.ShouldBe(order1.Id);
    }

    [Test]
    public async Task ShouldSearchBySpecificationWithStatus()
    {
        var number123 = TestData.Number("S");
        var number456 = TestData.Number("S");
        var employee1 = EmployeeBuilder.Create(TestData.UserName("employee1"))
            .WithFirstName("1").WithLastName("1").WithEmail("1").Build();
        var employee2 = EmployeeBuilder.Create(TestData.UserName("employee2"))
            .WithFirstName("2").WithLastName("2").WithEmail("2").Build();
        var order1 = WorkOrderBuilder.For(employee2, WorkOrderStatus.Assigned)
            .AssignedTo(employee1).WithNumber(number123).Build();
        var order2 = WorkOrderBuilder.For(employee1, WorkOrderStatus.Draft)
            .AssignedTo(employee2).WithNumber(number456).Build();
        await WorkOrderQuerySteps.SaveOrdersAsync(employee2, order1, order2);

        var specification = new WorkOrderSpecificationQuery();
        specification.MatchStatus(WorkOrderStatus.Assigned);
        var orders = (await WorkOrderQuerySteps.SearchAsync(specification))
            .Where(o => o.Id == order1.Id || o.Id == order2.Id).ToArray();

        orders.Length.ShouldBe(1);
        orders[0].Id.ShouldBe(order1.Id);
    }

    [Test]
    public async Task ShouldSearchWithEmptySpecificationAndReturnAll()
    {
        var number123 = TestData.Number("S");
        var number456 = TestData.Number("S");
        var employee = EmployeeBuilder.Create(TestData.UserName("employee1"))
            .WithFirstName("1").WithLastName("1").WithEmail("1").Build();
        var order1 = WorkOrderBuilder.For(employee, WorkOrderStatus.Draft)
            .AssignedTo(employee).WithNumber(number123).Build();
        var order2 = WorkOrderBuilder.For(employee, WorkOrderStatus.Draft)
            .AssignedTo(employee).WithNumber(number456).Build();
        await WorkOrderQuerySteps.SaveOrdersAsync(employee, order1, order2);

        var orders = await WorkOrderQuerySteps.SearchAsync(new WorkOrderSpecificationQuery());

        orders.Any(o => o.Id == order1.Id).ShouldBeTrue();
        orders.Any(o => o.Id == order2.Id).ShouldBeTrue();
    }
}
