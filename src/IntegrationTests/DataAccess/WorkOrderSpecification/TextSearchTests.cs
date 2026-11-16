using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.WorkOrderSpecification;

[TestFixture]
public class TextSearchTests
{
    [Test]
    public async Task ShouldMatchSearchTextCaseInsensitively()
    {
        var byTitleNumber = TestData.Number("S");
        var byAssigneeNumber = TestData.Number("S");
        var noneNumber = TestData.Number("S");
        var creator = EmployeeBuilder.Create(TestData.UserName("requester"))
            .WithFirstName("Ned").WithLastName("Flanders").WithEmail("ned@example.com").Build();
        var assignee = EmployeeBuilder.Create(TestData.UserName("willie"))
            .WithFirstName("Groundskeeper").WithLastName("Willie").WithEmail("willie@example.com").Build();
        await WorkOrderQuerySteps.SaveOrdersAsync(creator,
            WorkOrderBuilder.For(creator, WorkOrderStatus.Draft).WithNumber(byTitleNumber)
                .WithTitle("Tune the ORGAN").InRoom("Choir loft").Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Draft).AssignedTo(assignee)
                .WithNumber(byAssigneeNumber).WithTitle("Mow").InRoom("Grounds").Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Draft).WithNumber(noneNumber)
                .WithTitle("Paint").InRoom("Hall").Build());

        var organ = new WorkOrderSpecificationQuery();
        organ.MatchSearchText("organ");
        organ.MatchCreator(creator);
        var willie = new WorkOrderSpecificationQuery();
        willie.MatchSearchText("WILL");
        willie.MatchCreator(creator);

        (await WorkOrderQuerySteps.SearchAsync(organ)).Select(o => o.Number).ShouldBe([byTitleNumber]);
        (await WorkOrderQuerySteps.SearchAsync(willie)).Select(o => o.Number).ShouldBe([byAssigneeNumber]);
    }
}
