using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.WorkOrderSpecification;

[TestFixture]
public class CreatorRolesAndSortTests
{
    [Test]
    public async Task ShouldReturnEachWorkOrderOnce_WhenSearchingByCreatorWithSeveralRoles()
    {
        var firstNumber = TestData.Number("S");
        var secondNumber = TestData.Number("S");
        var thirdNumber = TestData.Number("S");
        var otherCreatorNumber = TestData.Number("S");
        var creator = EmployeeBuilder.Create(TestData.UserName("creator"))
            .WithFirstName("Timothy").WithLastName("Lovejoy Jr").WithEmail("creator@example.com")
            .WithRole($"Pastor_{TestData.Tag()}", true, false)
            .WithRole($"Requester_{TestData.Tag()}", true, false).Build();
        var other = EmployeeBuilder.Create(TestData.UserName("other"))
            .WithFirstName("Other").WithLastName("Person").WithEmail("other@example.com").Build();
        await WorkOrderQuerySteps.SaveOrdersAsync(creator,
            WorkOrderBuilder.For(creator, WorkOrderStatus.Draft).WithNumber(firstNumber).WithTitle("One").Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Draft).WithNumber(secondNumber).WithTitle("Two").Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Draft).AssignedTo(other)
                .WithNumber(thirdNumber).WithTitle("Three").Build(),
            WorkOrderBuilder.For(other, WorkOrderStatus.Draft).WithNumber(otherCreatorNumber).WithTitle("Not mine").Build());

        var specification = new WorkOrderSpecificationQuery();
        specification.MatchCreator(creator);
        var result = await WorkOrderQuerySteps.SearchAsync(specification);

        result.Length.ShouldBe(3);
        result.Select(o => o.Number).Distinct().Count().ShouldBe(3);
        result.Select(o => o.Number).ShouldBe([firstNumber, secondNumber, thirdNumber], ignoreOrder: true);
    }

    [Test]
    public async Task ShouldOrderByCreatedDateDescendingThenNumber()
    {
        var creator = EmployeeBuilder.Create(TestData.UserName("order"))
            .WithFirstName("Order").WithLastName("Creator").WithEmail("order@example.com").Build();
        var numbers = TestData.NumberSeries(4);
        var oldest = WorkOrderBuilder.For(creator, WorkOrderStatus.Draft)
            .WithNumber(numbers[3]).CreatedAt(new DateTime(2026, 1, 1)).Build();
        var newestB = WorkOrderBuilder.For(creator, WorkOrderStatus.Draft)
            .WithNumber(numbers[1]).CreatedAt(new DateTime(2026, 3, 1)).Build();
        var newestA = WorkOrderBuilder.For(creator, WorkOrderStatus.Draft)
            .WithNumber(numbers[0]).CreatedAt(new DateTime(2026, 3, 1)).Build();
        var middle = WorkOrderBuilder.For(creator, WorkOrderStatus.Draft)
            .WithNumber(numbers[2]).CreatedAt(new DateTime(2026, 2, 1)).Build();
        await WorkOrderQuerySteps.SaveOrdersAsync(creator, oldest, newestB, newestA, middle);

        var specification = new WorkOrderSpecificationQuery();
        specification.MatchCreator(creator);
        var orders = await WorkOrderQuerySteps.SearchAsync(specification);

        orders.Select(o => o.Number).ShouldBe([newestA.Number, newestB.Number, middle.Number, oldest.Number]);
    }

    [Test]
    public async Task ShouldOrderByDueDateAscendingWithNullsLast()
    {
        var creator = EmployeeBuilder.Create(TestData.UserName("dueasc"))
            .WithFirstName("Due").WithLastName("Ascending").WithEmail("dueasc@example.com").Build();
        var numbers = TestData.NumberSeries(3);
        var early = WorkOrderBuilder.For(creator, WorkOrderStatus.Draft)
            .WithNumber(numbers[0]).DueOn(new DateOnly(2026, 1, 15)).CreatedAt(new DateTime(2026, 1, 1)).Build();
        var later = WorkOrderBuilder.For(creator, WorkOrderStatus.Draft)
            .WithNumber(numbers[1]).DueOn(new DateOnly(2026, 6, 15)).CreatedAt(new DateTime(2026, 2, 1)).Build();
        var undated = WorkOrderBuilder.For(creator, WorkOrderStatus.Draft)
            .WithNumber(numbers[2]).DueOn(null).CreatedAt(new DateTime(2026, 3, 1)).Build();
        await WorkOrderQuerySteps.SaveOrdersAsync(creator, early, later, undated);

        var specification = new WorkOrderSpecificationQuery();
        specification.MatchCreator(creator);
        specification.MatchDueDateOrder(true);
        var orders = await WorkOrderQuerySteps.SearchAsync(specification);

        orders.Select(o => o.Number).ShouldBe([early.Number, later.Number, undated.Number]);
    }

    [Test]
    public async Task ShouldOrderByDueDateDescendingWithNullsLast()
    {
        var creator = EmployeeBuilder.Create(TestData.UserName("duedesc"))
            .WithFirstName("Due").WithLastName("Descending").WithEmail("duedesc@example.com").Build();
        var numbers = TestData.NumberSeries(3);
        var early = WorkOrderBuilder.For(creator, WorkOrderStatus.Draft)
            .WithNumber(numbers[0]).DueOn(new DateOnly(2026, 1, 15)).CreatedAt(new DateTime(2026, 3, 1)).Build();
        var later = WorkOrderBuilder.For(creator, WorkOrderStatus.Draft)
            .WithNumber(numbers[1]).DueOn(new DateOnly(2026, 6, 15)).CreatedAt(new DateTime(2026, 2, 1)).Build();
        var undated = WorkOrderBuilder.For(creator, WorkOrderStatus.Draft)
            .WithNumber(numbers[2]).DueOn(null).CreatedAt(new DateTime(2026, 1, 1)).Build();
        await WorkOrderQuerySteps.SaveOrdersAsync(creator, early, later, undated);

        var specification = new WorkOrderSpecificationQuery();
        specification.MatchCreator(creator);
        specification.MatchDueDateOrder(true, true);
        var orders = await WorkOrderQuerySteps.SearchAsync(specification);

        orders.Select(o => o.Number).ShouldBe([later.Number, early.Number, undated.Number]);
    }
}
