using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class WorkOrderSearchHandlerTakeTests
{
    [Test]
    public async Task Handle_WhenTakeIsSet_ReturnsAtMostTakeNewestFirst()
    {
        var creator = await GivenThreeOrdersAsync();
        var query = new WorkOrderSpecificationQuery { Take = 2 };
        query.MatchCreator(creator);

        var result = await WorkOrderQuerySteps.SearchAsync(query);

        result.Length.ShouldBe(2);
        result.Select(order => order.Title).ShouldBe(["third", "second"]);
    }

    [Test]
    public async Task Handle_WhenTakeIsNull_ReturnsEveryMatch()
    {
        var creator = await GivenThreeOrdersAsync();
        var query = new WorkOrderSpecificationQuery();
        query.MatchCreator(creator);

        var result = await WorkOrderQuerySteps.SearchAsync(query);

        result.Length.ShouldBe(3);
    }

    private static async Task<Employee> GivenThreeOrdersAsync()
    {
        var userName = TestData.UserName("pagecap");
        var creator = EmployeeBuilder.Create(userName)
            .WithFirstName("Page").WithLastName("Cap").WithEmail(TestData.Email(userName)).Build();
        var created = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var orders = new[] { "first", "second", "third" }
            .Select((title, index) => WorkOrderBuilder.For(creator, WorkOrderStatus.Draft)
                .WithTitle(title).CreatedAt(created.AddMinutes(index)).Build())
            .ToArray();
        await WorkOrderQuerySteps.SaveOrdersAsync(creator, orders);
        return creator;
    }
}
