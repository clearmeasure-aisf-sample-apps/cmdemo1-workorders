using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.WorkOrderSpecification;

[TestFixture]
public class BuildingAndRoomFilterTests
{
    [Test]
    public async Task ShouldFilterByRoomExactMatch()
    {
        var room101Number = TestData.Number("S");
        var room101SpacesNumber = TestData.Number("S");
        var room102Number = TestData.Number("S");
        var noRoomNumber = TestData.Number("S");
        var employee = EmployeeBuilder.Create(TestData.UserName("employee1"))
            .WithFirstName("1").WithLastName("1").WithEmail("1").Build();
        await WorkOrderQuerySteps.SaveOrdersAsync(employee,
            WorkOrderBuilder.For(employee, WorkOrderStatus.Draft).WithNumber(room101Number).InRoom("101").Build(),
            WorkOrderBuilder.For(employee, WorkOrderStatus.Draft).WithNumber(room101SpacesNumber).InRoom(" 101 ").Build(),
            WorkOrderBuilder.For(employee, WorkOrderStatus.Draft).WithNumber(room102Number).InRoom("102").Build(),
            WorkOrderBuilder.For(employee, WorkOrderStatus.Draft).WithNumber(noRoomNumber).InRoom(null).Build());

        var specification = new WorkOrderSpecificationQuery();
        specification.MatchRoom("101");
        specification.MatchCreator(employee);
        var orders = await WorkOrderQuerySteps.SearchAsync(specification);

        orders.Select(o => o.Number).ShouldBe([room101Number, room101SpacesNumber], ignoreOrder: true);
    }

    [Test]
    public async Task ShouldFilterByRoomCaseInsensitiveWhenDatabaseIsCaseInsensitive()
    {
        var lowerRoomNumber = TestData.Number("S");
        var upperRoomNumber = TestData.Number("S");
        var mixedRoomNumber = TestData.Number("S");
        var differentRoomNumber = TestData.Number("S");
        var employee = EmployeeBuilder.Create(TestData.UserName("employee1"))
            .WithFirstName("1").WithLastName("1").WithEmail("1").Build();
        await WorkOrderQuerySteps.SaveOrdersAsync(employee,
            WorkOrderBuilder.For(employee, WorkOrderStatus.Draft).WithNumber(lowerRoomNumber).InRoom("sanctuary").Build(),
            WorkOrderBuilder.For(employee, WorkOrderStatus.Draft).WithNumber(upperRoomNumber).InRoom("SANCTUARY").Build(),
            WorkOrderBuilder.For(employee, WorkOrderStatus.Draft).WithNumber(mixedRoomNumber).InRoom("Sanctuary").Build(),
            WorkOrderBuilder.For(employee, WorkOrderStatus.Draft).WithNumber(differentRoomNumber).InRoom("kitchen").Build());

        var specification = new WorkOrderSpecificationQuery();
        specification.MatchRoom("Sanctuary");
        specification.MatchCreator(employee);
        var orders = await WorkOrderQuerySteps.SearchAsync(specification);

        orders.Select(o => o.Number)
            .ShouldBe([lowerRoomNumber, upperRoomNumber, mixedRoomNumber], ignoreOrder: true);
    }

    [Test]
    public async Task ShouldFilterByNoRoomWhenRoomIsNull()
    {
        var withRoomNumber = TestData.Number("S");
        var nullRoomNumber = TestData.Number("S");
        var emptyRoomNumber = TestData.Number("S");
        var whitespaceRoomNumber = TestData.Number("S");
        var employee = EmployeeBuilder.Create(TestData.UserName("employee1"))
            .WithFirstName("1").WithLastName("1").WithEmail("1").Build();
        await WorkOrderQuerySteps.SaveOrdersAsync(employee,
            WorkOrderBuilder.For(employee, WorkOrderStatus.Draft).WithNumber(withRoomNumber).InRoom("101").Build(),
            WorkOrderBuilder.For(employee, WorkOrderStatus.Draft).WithNumber(nullRoomNumber).InRoom(null).Build(),
            WorkOrderBuilder.For(employee, WorkOrderStatus.Draft).WithNumber(emptyRoomNumber).InRoom("").Build(),
            WorkOrderBuilder.For(employee, WorkOrderStatus.Draft).WithNumber(whitespaceRoomNumber).InRoom("   ").Build());

        var specification = new WorkOrderSpecificationQuery();
        specification.MatchNoRoom(true);
        specification.MatchCreator(employee);
        var orders = await WorkOrderQuerySteps.SearchAsync(specification);

        orders.Select(o => o.Number)
            .ShouldBe([nullRoomNumber, emptyRoomNumber, whitespaceRoomNumber], ignoreOrder: true);
    }

    [Test]
    public async Task ShouldFilterByNoBuilding()
    {
        var withBuildingNumber = TestData.Number("S");
        var nullBuildingNumber = TestData.Number("S");
        var emptyBuildingNumber = TestData.Number("S");
        var whitespaceBuildingNumber = TestData.Number("S");
        var employee = EmployeeBuilder.Create(TestData.UserName("employee1"))
            .WithFirstName("1").WithLastName("1").WithEmail("1").Build();
        await WorkOrderQuerySteps.SaveOrdersAsync(employee,
            WorkOrderBuilder.For(employee, WorkOrderStatus.Draft).WithNumber(withBuildingNumber)
                .InBuilding("Sanctuary").Build(),
            WorkOrderBuilder.For(employee, WorkOrderStatus.Draft).WithNumber(nullBuildingNumber)
                .InBuilding(null).Build(),
            WorkOrderBuilder.For(employee, WorkOrderStatus.Draft).WithNumber(emptyBuildingNumber)
                .InBuilding("").Build(),
            WorkOrderBuilder.For(employee, WorkOrderStatus.Draft).WithNumber(whitespaceBuildingNumber)
                .InBuilding("   ").Build());

        var specification = new WorkOrderSpecificationQuery();
        specification.MatchNoBuilding(true);
        specification.MatchCreator(employee);
        var orders = await WorkOrderQuerySteps.SearchAsync(specification);

        orders.Select(o => o.Number)
            .ShouldBe([nullBuildingNumber, emptyBuildingNumber, whitespaceBuildingNumber], ignoreOrder: true);
    }

    [Test]
    public async Task ShouldMatchRoomExactWhenBothRoomAndNoBuildingAreSpecified()
    {
        var sanctuaryRoom101Number = TestData.Number("S");
        var noRoomWithBuildingNumber = TestData.Number("S");
        var room101NoBuildingNumber = TestData.Number("S");
        var room101NoRoomNumber = TestData.Number("S");
        var employee = EmployeeBuilder.Create(TestData.UserName("employee1"))
            .WithFirstName("1").WithLastName("1").WithEmail("1").Build();
        await WorkOrderQuerySteps.SaveOrdersAsync(employee,
            WorkOrderBuilder.For(employee, WorkOrderStatus.Draft).WithNumber(sanctuaryRoom101Number)
                .InBuilding("Sanctuary").InRoom("101").Build(),
            WorkOrderBuilder.For(employee, WorkOrderStatus.Draft).WithNumber(noRoomWithBuildingNumber)
                .InBuilding("Sanctuary").InRoom(null).Build(),
            WorkOrderBuilder.For(employee, WorkOrderStatus.Draft).WithNumber(room101NoBuildingNumber)
                .InBuilding(null).InRoom("101").Build(),
            WorkOrderBuilder.For(employee, WorkOrderStatus.Draft).WithNumber(room101NoRoomNumber)
                .InBuilding(null).InRoom(null).Build());

        var specification = new WorkOrderSpecificationQuery();
        specification.MatchRoom("101");
        specification.MatchCreator(employee);
        var orders = await WorkOrderQuerySteps.SearchAsync(specification);

        orders.Select(o => o.Number)
            .ShouldBe([sanctuaryRoom101Number, room101NoBuildingNumber], ignoreOrder: true);
    }

    [Test]
    public async Task ShouldCombineRoomFilterWithOpenOnlyFilter()
    {
        var openRoom101Number = TestData.Number("S");
        var completeRoom101Number = TestData.Number("S");
        var openRoom102Number = TestData.Number("S");
        var draftRoom101Number = TestData.Number("S");
        var employee = EmployeeBuilder.Create(TestData.UserName("employee1"))
            .WithFirstName("1").WithLastName("1").WithEmail("1").Build();
        await WorkOrderQuerySteps.SaveOrdersAsync(employee,
            WorkOrderBuilder.For(employee, WorkOrderStatus.Assigned).WithNumber(openRoom101Number)
                .InRoom("101").Build(),
            WorkOrderBuilder.For(employee, WorkOrderStatus.Complete).WithNumber(completeRoom101Number)
                .InRoom("101").Build(),
            WorkOrderBuilder.For(employee, WorkOrderStatus.Draft).WithNumber(openRoom102Number)
                .InRoom("102").Build(),
            WorkOrderBuilder.For(employee, WorkOrderStatus.Draft).WithNumber(draftRoom101Number)
                .InRoom("101").Build());

        var specification = new WorkOrderSpecificationQuery();
        specification.MatchRoom("101");
        specification.MatchOpenOnly(true);
        specification.MatchCreator(employee);
        var orders = await WorkOrderQuerySteps.SearchAsync(specification);

        orders.Select(o => o.Number)
            .ShouldBe([openRoom101Number, draftRoom101Number], ignoreOrder: true);
    }
}
