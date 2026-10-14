using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

[TestFixture]
public class WorkOrderSpecificationHandlerTests
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

        var employee1 = new Employee(TestData.UserName("employee1"), "1", "1", "1");
        var employee2 = new Employee(TestData.UserName("employee2"), "2", "2", "2");
        var order1 = new WorkOrder()
        {
            Creator = employee2,
            Assignee = employee1,
            Number = number123
        };
        var order2 = new WorkOrder()
        {
            Creator = employee1,
            Assignee = employee2,
            Number = number456
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee1);
            context.Add(employee2);
            context.Add(order1);
            context.Add(order2);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var repository = new WorkOrderSearchHandler(dataContext, TimeProvider.System);
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchAssignee(employee1);
        var orders = await repository.Handle(specification);

        orders.Length.ShouldBe(1);
        orders[0].Id.ShouldBe(order1.Id);
    }

    [Test]
    public async Task ShouldSearchBySpecificationWithCreator()
    {
        var number123 = TestData.Number("S");
        var number456 = TestData.Number("S");

        var creator1 = new Employee(TestData.UserName("employee1"), "1", "1", "1");
        var creator2 = new Employee(TestData.UserName("employee2"), "2", "2", "2");
        var order1 = new WorkOrder()
        {
            Creator = creator1,
            Number = number123
        };
        var order2 = new WorkOrder()
        {
            Creator = creator2,
            Number = number456
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator1);
            context.Add(creator2);
            context.Add(order1);
            context.Add(order2);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var repository = new WorkOrderSearchHandler(dataContext, TimeProvider.System);
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchCreator(creator1);
        var orders = await repository.Handle(specification);

        orders.Length.ShouldBe(1);
        orders[0].Id.ShouldBe(order1.Id);
    }

    [Test]
    public async Task ShouldSearchBySpecificationWithFullSpecification()
    {
        var number123 = TestData.Number("S");
        var number456 = TestData.Number("S");

        var employee1 = new Employee(TestData.UserName("employee1"), "1", "1", "1");
        var employee2 = new Employee(TestData.UserName("employee2"), "2", "2", "2");
        var order1 = new WorkOrder()
        {
            Creator = employee2,
            Assignee = employee1,
            Number = number123,
            Status = WorkOrderStatus.Assigned
        };
        var order2 = new WorkOrder()
        {
            Creator = employee1,
            Assignee = employee2,
            Number = number456,
            Status = WorkOrderStatus.Draft
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee1);
            context.Add(employee2);
            context.Add(order1);
            context.Add(order2);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var repository = new WorkOrderSearchHandler(dataContext, TimeProvider.System);
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchStatus(WorkOrderStatus.Assigned);
        specification.MatchCreator(employee2);
        specification.MatchAssignee(employee1);
        var orders = await repository.Handle(specification);

        orders.Length.ShouldBe(1);
        orders[0].Id.ShouldBe(order1.Id);
    }

    [Test]
    public async Task ShouldSearchBySpecificationWithStatus()
    {
        var number123 = TestData.Number("S");
        var number456 = TestData.Number("S");

        var employee1 = new Employee(TestData.UserName("employee1"), "1", "1", "1");
        var employee2 = new Employee(TestData.UserName("employee2"), "2", "2", "2");
        var order1 = new WorkOrder()
        {
            Creator = employee2,
            Assignee = employee1,
            Number = number123,
            Status = WorkOrderStatus.Assigned
        };
        var order2 = new WorkOrder()
        {
            Creator = employee1,
            Assignee = employee2,
            Number = number456,
            Status = WorkOrderStatus.Draft
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee1);
            context.Add(employee2);
            context.Add(order1);
            context.Add(order2);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var repository = new WorkOrderSearchHandler(dataContext, TimeProvider.System);
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchStatus(WorkOrderStatus.Assigned);
        var orders = (await repository.Handle(specification))
            .Where(o => o.Id == order1.Id || o.Id == order2.Id).ToArray();

        orders.Length.ShouldBe(1);
        orders[0].Id.ShouldBe(order1.Id);
    }

    [Test]
    public async Task ShouldSearchWithEmptySpecificationAndReturnAll()
    {
        var number123 = TestData.Number("S");
        var number456 = TestData.Number("S");

        var employee = new Employee(TestData.UserName("employee1"), "1", "1", "1");
        var order1 = new WorkOrder { Creator = employee, Assignee = employee, Number = number123 };
        var order2 = new WorkOrder { Creator = employee, Assignee = employee, Number = number456 };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(order1);
            context.Add(order2);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var repository = new WorkOrderSearchHandler(dataContext, TimeProvider.System);
        var orders = await repository.Handle(new WorkOrderSpecificationQuery());

        orders.Any(o => o.Id == order1.Id).ShouldBeTrue();
        orders.Any(o => o.Id == order2.Id).ShouldBeTrue();
    }

    [Test]
    public async Task SearchShouldReturnHydratedEmployeesWithWorkOrders()
    {
        var number123 = TestData.Number("S");

        var creator = new Employee(TestData.UserName("employee1"), "John", "Doe", "john.doe@example.com");
        var assignee = new Employee(TestData.UserName("employee2"), "Jane", "Smith", "jane.smith@example.com");

        var order1 = new WorkOrder
        {
            Creator = creator,
            Assignee = assignee,
            Number = number123,
            Title = "Fix plumbing",
            Description = "Fix the plumbing in room 101",
            RoomNumber = "101",
            Status = WorkOrderStatus.InProgress
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignee);
            context.Add(order1);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var repository = new WorkOrderSearchHandler(dataContext, TimeProvider.System);

        var specification = new WorkOrderSpecificationQuery();
        specification.MatchCreator(creator);

        var orders = await repository.Handle(specification);

        orders.Length.ShouldBe(1);

        var rehydratedOrder = orders.First(o => o.Number == number123);

        rehydratedOrder.Creator.ShouldNotBeNull();
        rehydratedOrder.Assignee.ShouldNotBeNull();
        rehydratedOrder.Creator.Id.ShouldBe(creator.Id);
        rehydratedOrder.Creator.FirstName.ShouldBe(creator.FirstName);
        rehydratedOrder.Creator.LastName.ShouldBe(creator.LastName);
        rehydratedOrder.Creator.EmailAddress.ShouldBe(creator.EmailAddress);
        rehydratedOrder.Assignee.Id.ShouldBe(assignee.Id);
        rehydratedOrder.Assignee.FirstName.ShouldBe(assignee.FirstName);
        rehydratedOrder.Assignee.LastName.ShouldBe(assignee.LastName);
        rehydratedOrder.Assignee.EmailAddress.ShouldBe(assignee.EmailAddress);
    }
    [Test]
    public async Task ShouldReturnOnlyOverdueWorkOrders_WhenOverdueOnlyFilterIsTrue()
    {
        var numberOvr001 = TestData.Number("S");
        var numberFut001 = TestData.Number("S");

        var employee = new Employee(TestData.UserName("employee1"), "1", "1", "1");
        var overdueOrder = new WorkOrder
        {
            Creator = employee,
            Number = numberOvr001,
            Status = WorkOrderStatus.InProgress,
            DueDate = new DateOnly(2000, 1, 1)
        };
        var futureOrder = new WorkOrder
        {
            Creator = employee,
            Number = numberFut001,
            Status = WorkOrderStatus.InProgress,
            DueDate = new DateOnly(2099, 12, 31)
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.Add(overdueOrder);
            context.Add(futureOrder);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var repository = new WorkOrderSearchHandler(dataContext, TimeProvider.System);
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchOverdueOnly(true);
        specification.MatchCreator(employee);
        var orders = await repository.Handle(specification);

        orders.Length.ShouldBe(1);
        orders[0].Number.ShouldBe(numberOvr001);
    }
    [Test]
    public async Task ShouldReturnEachWorkOrderOnce_WhenSearchingByCreatorWithSeveralRoles()
    {
        var number38173ba = TestData.Number("S");
        var number38173bb = TestData.Number("S");
        var number38173bc = TestData.Number("S");
        var number38173bd = TestData.Number("S");

        var creator = new Employee(TestData.UserName("creator"), "Timothy", "Lovejoy Jr", "creator@example.com");
        creator.AddRole(new Role("Pastor", true, false));
        creator.AddRole(new Role("Requester", true, false));
        var other = new Employee(TestData.UserName("other"), "Other", "Person", "other@example.com");
        var orders = new[]
        {
            new WorkOrder { Creator = creator, Number = number38173ba, Title = "One" },
            new WorkOrder { Creator = creator, Number = number38173bb, Title = "Two" },
            new WorkOrder { Creator = creator, Assignee = other, Number = number38173bc, Title = "Three" },
            new WorkOrder { Creator = other, Number = number38173bd, Title = "Not mine" }
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(other);
            context.AddRange(orders.Cast<object>());
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var repository = new WorkOrderSearchHandler(dataContext, TimeProvider.System);
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchCreator(creator);
        var result = await repository.Handle(specification);

        result.Length.ShouldBe(3);
        result.Select(o => o.Number).Distinct().Count().ShouldBe(3);
        result.Select(o => o.Number).ShouldBe([number38173ba, number38173bb, number38173bc], ignoreOrder: true);
    }
    [Test]
    public async Task ShouldOrderByCreatedDateDescendingThenNumber()
    {
        var creator = new Employee(TestData.UserName("order"), "Order", "Creator", "order@example.com");
        // Numbers ascend in index order, so the two orders created on the same day sort A before B.
        var numbers = TestData.NumberSeries(4);
        var oldest = new WorkOrder { Creator = creator, Number = numbers[3], CreatedDate = new DateTime(2026, 1, 1) };
        var newestB = new WorkOrder { Creator = creator, Number = numbers[1], CreatedDate = new DateTime(2026, 3, 1) };
        var newestA = new WorkOrder { Creator = creator, Number = numbers[0], CreatedDate = new DateTime(2026, 3, 1) };
        var middle = new WorkOrder { Creator = creator, Number = numbers[2], CreatedDate = new DateTime(2026, 2, 1) };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.AddRange(oldest, newestB, newestA, middle);
            await context.SaveChangesAsync();
        }

        var handler = new WorkOrderSearchHandler(TestHost.GetRequiredService<DataContext>(), TimeProvider.System);
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchCreator(creator);
        var orders = await handler.Handle(specification);

        orders.Select(o => o.Number).ShouldBe([newestA.Number, newestB.Number, middle.Number, oldest.Number]);
    }

    [Test]
    public async Task ShouldMatchSearchTextCaseInsensitively()
    {
        var numberSrch001 = TestData.Number("S");
        var numberSrch002 = TestData.Number("S");
        var numberSrch003 = TestData.Number("S");

        var creator = new Employee(TestData.UserName("requester"), "Ned", "Flanders", "ned@example.com");
        var assignee = new Employee(TestData.UserName("willie"), "Groundskeeper", "Willie", "willie@example.com");
        var byTitle = new WorkOrder { Creator = creator, Number = numberSrch001, Title = "Tune the ORGAN", RoomNumber = "Choir loft" };
        var byAssignee = new WorkOrder { Creator = creator, Assignee = assignee, Number = numberSrch002, Title = "Mow", RoomNumber = "Grounds" };
        var none = new WorkOrder { Creator = creator, Number = numberSrch003, Title = "Paint", RoomNumber = "Hall" };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.AddRange(creator, assignee);
            context.AddRange(byTitle, byAssignee, none);
            await context.SaveChangesAsync();
        }

        var handler = new WorkOrderSearchHandler(TestHost.GetRequiredService<DataContext>(), TimeProvider.System);
        var organ = new WorkOrderSpecificationQuery();
        organ.MatchSearchText("organ");
        organ.MatchCreator(creator);
        var willie = new WorkOrderSpecificationQuery();
        willie.MatchSearchText("WILL");
        willie.MatchCreator(creator);

        (await handler.Handle(organ)).Select(o => o.Number).ShouldBe([numberSrch001]);
        (await handler.Handle(willie)).Select(o => o.Number).ShouldBe([numberSrch002]);
    }

    [Test]
    public async Task ShouldFilterByRoomExactMatch()
    {
        var numberRm001 = TestData.Number("S");
        var numberRm002 = TestData.Number("S");
        var numberRm003 = TestData.Number("S");
        var numberRm004 = TestData.Number("S");

        var employee = new Employee(TestData.UserName("employee1"), "1", "1", "1");
        var room101 = new WorkOrder { Creator = employee, Number = numberRm001, RoomNumber = "101" };
        var room101Spaces = new WorkOrder { Creator = employee, Number = numberRm002, RoomNumber = " 101 " };
        var room102 = new WorkOrder { Creator = employee, Number = numberRm003, RoomNumber = "102" };
        var noRoom = new WorkOrder { Creator = employee, Number = numberRm004, RoomNumber = null };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.AddRange(room101, room101Spaces, room102, noRoom);
            await context.SaveChangesAsync();
        }

        var handler = new WorkOrderSearchHandler(TestHost.GetRequiredService<DataContext>(), TimeProvider.System);
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchRoom("101");
        specification.MatchCreator(employee);
        var orders = await handler.Handle(specification);

        orders.Select(o => o.Number).ShouldBe([numberRm001, numberRm002], ignoreOrder: true);
    }

    [Test]
    public async Task ShouldFilterByRoomCaseInsensitiveWhenDatabaseIsCaseInsensitive()
    {
        var numberCs001 = TestData.Number("S");
        var numberCs002 = TestData.Number("S");
        var numberCs003 = TestData.Number("S");
        var numberCs004 = TestData.Number("S");

        var employee = new Employee(TestData.UserName("employee1"), "1", "1", "1");
        var lowerRoom = new WorkOrder { Creator = employee, Number = numberCs001, RoomNumber = "sanctuary" };
        var upperRoom = new WorkOrder { Creator = employee, Number = numberCs002, RoomNumber = "SANCTUARY" };
        var mixedRoom = new WorkOrder { Creator = employee, Number = numberCs003, RoomNumber = "Sanctuary" };
        var differentRoom = new WorkOrder { Creator = employee, Number = numberCs004, RoomNumber = "kitchen" };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.AddRange(lowerRoom, upperRoom, mixedRoom, differentRoom);
            await context.SaveChangesAsync();
        }

        var handler = new WorkOrderSearchHandler(TestHost.GetRequiredService<DataContext>(), TimeProvider.System);
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchRoom("Sanctuary");
        specification.MatchCreator(employee);
        var orders = await handler.Handle(specification);

        // SQL Server is case-insensitive; SQLite is case-sensitive.
        // This test should find at least the exact match.
        orders.Select(o => o.Number).ShouldContain(numberCs003);
    }

    [Test]
    public async Task ShouldFilterByNoRoomWhenRoomIsNull()
    {
        var numberNr001 = TestData.Number("S");
        var numberNr002 = TestData.Number("S");
        var numberNr003 = TestData.Number("S");
        var numberNr004 = TestData.Number("S");

        var employee = new Employee(TestData.UserName("employee1"), "1", "1", "1");
        var withRoom = new WorkOrder { Creator = employee, Number = numberNr001, RoomNumber = "101" };
        var nullRoom = new WorkOrder { Creator = employee, Number = numberNr002, RoomNumber = null };
        var emptyRoom = new WorkOrder { Creator = employee, Number = numberNr003, RoomNumber = "" };
        var whitespaceRoom = new WorkOrder { Creator = employee, Number = numberNr004, RoomNumber = "   " };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.AddRange(withRoom, nullRoom, emptyRoom, whitespaceRoom);
            await context.SaveChangesAsync();
        }

        var handler = new WorkOrderSearchHandler(TestHost.GetRequiredService<DataContext>(), TimeProvider.System);
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchNoRoom(true);
        specification.MatchCreator(employee);
        var orders = await handler.Handle(specification);

        orders.Select(o => o.Number).ShouldBe([numberNr002, numberNr003, numberNr004], ignoreOrder: true);
    }

    [Test]
    public async Task ShouldFilterByNoBuilding()
    {
        var numberNb001 = TestData.Number("S");
        var numberNb002 = TestData.Number("S");
        var numberNb003 = TestData.Number("S");
        var numberNb004 = TestData.Number("S");

        var employee = new Employee(TestData.UserName("employee1"), "1", "1", "1");
        var withBuilding = new WorkOrder { Creator = employee, Number = numberNb001, Building = "Sanctuary" };
        var nullBuilding = new WorkOrder { Creator = employee, Number = numberNb002, Building = null };
        var emptyBuilding = new WorkOrder { Creator = employee, Number = numberNb003, Building = "" };
        var whitespaceBuilding = new WorkOrder { Creator = employee, Number = numberNb004, Building = "   " };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.AddRange(withBuilding, nullBuilding, emptyBuilding, whitespaceBuilding);
            await context.SaveChangesAsync();
        }

        var handler = new WorkOrderSearchHandler(TestHost.GetRequiredService<DataContext>(), TimeProvider.System);
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchNoBuilding(true);
        specification.MatchCreator(employee);
        var orders = await handler.Handle(specification);

        orders.Select(o => o.Number).ShouldBe([numberNb002, numberNb003, numberNb004], ignoreOrder: true);
    }

    [Test]
    public async Task ShouldMatchRoomExactWhenBothRoomAndNoBuildingAreSpecified()
    {
        var numberCb001 = TestData.Number("S");
        var numberCb002 = TestData.Number("S");
        var numberCb003 = TestData.Number("S");
        var numberCb004 = TestData.Number("S");

        var employee = new Employee(TestData.UserName("employee1"), "1", "1", "1");
        var sanctuaryRoom101 = new WorkOrder { Creator = employee, Number = numberCb001, Building = "Sanctuary", RoomNumber = "101" };
        var noRoomWithBuilding = new WorkOrder { Creator = employee, Number = numberCb002, Building = "Sanctuary", RoomNumber = null };
        var room101NoBuilding = new WorkOrder { Creator = employee, Number = numberCb003, Building = null, RoomNumber = "101" };
        var room101NoRoom = new WorkOrder { Creator = employee, Number = numberCb004, Building = null, RoomNumber = null };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.AddRange(sanctuaryRoom101, noRoomWithBuilding, room101NoBuilding, room101NoRoom);
            await context.SaveChangesAsync();
        }

        var handler = new WorkOrderSearchHandler(TestHost.GetRequiredService<DataContext>(), TimeProvider.System);
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchRoom("101");
        specification.MatchCreator(employee);
        var orders = await handler.Handle(specification);

        orders.Select(o => o.Number).ShouldBe([numberCb001, numberCb003], ignoreOrder: true);
    }

    [Test]
    public async Task ShouldCombineRoomFilterWithOpenOnlyFilter()
    {
        var numberRo001 = TestData.Number("S");
        var numberRo002 = TestData.Number("S");
        var numberRo003 = TestData.Number("S");
        var numberRo004 = TestData.Number("S");

        var employee = new Employee(TestData.UserName("employee1"), "1", "1", "1");
        var openRoom101 = new WorkOrder { Creator = employee, Number = numberRo001, RoomNumber = "101", Status = WorkOrderStatus.Assigned };
        var completeRoom101 = new WorkOrder { Creator = employee, Number = numberRo002, RoomNumber = "101", Status = WorkOrderStatus.Complete };
        var openRoom102 = new WorkOrder { Creator = employee, Number = numberRo003, RoomNumber = "102", Status = WorkOrderStatus.Draft };
        var draftRoom101 = new WorkOrder { Creator = employee, Number = numberRo004, RoomNumber = "101", Status = WorkOrderStatus.Draft };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.AddRange(openRoom101, completeRoom101, openRoom102, draftRoom101);
            await context.SaveChangesAsync();
        }

        var handler = new WorkOrderSearchHandler(TestHost.GetRequiredService<DataContext>(), TimeProvider.System);
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchRoom("101");
        specification.MatchOpenOnly(true);
        specification.MatchCreator(employee);
        var orders = await handler.Handle(specification);

        orders.Select(o => o.Number).ShouldBe([numberRo001, numberRo004], ignoreOrder: true);
    }
}
