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
        var overdueOrderNumber = TestData.Number("S");
        var futureOrderNumber = TestData.Number("S");

        var employee = new Employee(TestData.UserName("employee1"), "1", "1", "1");
        var overdueOrder = new WorkOrder
        {
            Creator = employee,
            Number = overdueOrderNumber,
            Status = WorkOrderStatus.InProgress,
            DueDate = new DateOnly(2000, 1, 1)
        };
        var futureOrder = new WorkOrder
        {
            Creator = employee,
            Number = futureOrderNumber,
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
        orders[0].Number.ShouldBe(overdueOrderNumber);
    }
    [Test]
    public async Task ShouldReturnEachWorkOrderOnce_WhenSearchingByCreatorWithSeveralRoles()
    {
        var firstNumber = TestData.Number("S");
        var secondNumber = TestData.Number("S");
        var thirdNumber = TestData.Number("S");
        var otherCreatorNumber = TestData.Number("S");

        var creator = new Employee(TestData.UserName("creator"), "Timothy", "Lovejoy Jr", "creator@example.com");
        creator.AddRole(new Role($"Pastor_{TestData.Tag()}", true, false));
        creator.AddRole(new Role($"Requester_{TestData.Tag()}", true, false));
        var other = new Employee(TestData.UserName("other"), "Other", "Person", "other@example.com");
        var orders = new[]
        {
            new WorkOrder { Creator = creator, Number = firstNumber, Title = "One" },
            new WorkOrder { Creator = creator, Number = secondNumber, Title = "Two" },
            new WorkOrder { Creator = creator, Assignee = other, Number = thirdNumber, Title = "Three" },
            new WorkOrder { Creator = other, Number = otherCreatorNumber, Title = "Not mine" }
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
        result.Select(o => o.Number).ShouldBe([firstNumber, secondNumber, thirdNumber], ignoreOrder: true);
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
        var byTitleNumber = TestData.Number("S");
        var byAssigneeNumber = TestData.Number("S");
        var noneNumber = TestData.Number("S");

        var creator = new Employee(TestData.UserName("requester"), "Ned", "Flanders", "ned@example.com");
        var assignee = new Employee(TestData.UserName("willie"), "Groundskeeper", "Willie", "willie@example.com");
        var byTitle = new WorkOrder { Creator = creator, Number = byTitleNumber, Title = "Tune the ORGAN", RoomNumber = "Choir loft" };
        var byAssignee = new WorkOrder { Creator = creator, Assignee = assignee, Number = byAssigneeNumber, Title = "Mow", RoomNumber = "Grounds" };
        var none = new WorkOrder { Creator = creator, Number = noneNumber, Title = "Paint", RoomNumber = "Hall" };

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

        (await handler.Handle(organ)).Select(o => o.Number).ShouldBe([byTitleNumber]);
        (await handler.Handle(willie)).Select(o => o.Number).ShouldBe([byAssigneeNumber]);
    }

    [Test]
    public async Task ShouldFilterByRoomExactMatch()
    {
        var room101Number = TestData.Number("S");
        var room101SpacesNumber = TestData.Number("S");
        var room102Number = TestData.Number("S");
        var noRoomNumber = TestData.Number("S");

        var employee = new Employee(TestData.UserName("employee1"), "1", "1", "1");
        var room101 = new WorkOrder { Creator = employee, Number = room101Number, RoomNumber = "101" };
        var room101Spaces = new WorkOrder { Creator = employee, Number = room101SpacesNumber, RoomNumber = " 101 " };
        var room102 = new WorkOrder { Creator = employee, Number = room102Number, RoomNumber = "102" };
        var noRoom = new WorkOrder { Creator = employee, Number = noRoomNumber, RoomNumber = null };

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

        orders.Select(o => o.Number).ShouldBe([room101Number, room101SpacesNumber], ignoreOrder: true);
    }

    [Test]
    public async Task ShouldFilterByRoomCaseInsensitiveWhenDatabaseIsCaseInsensitive()
    {
        var lowerRoomNumber = TestData.Number("S");
        var upperRoomNumber = TestData.Number("S");
        var mixedRoomNumber = TestData.Number("S");
        var differentRoomNumber = TestData.Number("S");

        var employee = new Employee(TestData.UserName("employee1"), "1", "1", "1");
        var lowerRoom = new WorkOrder { Creator = employee, Number = lowerRoomNumber, RoomNumber = "sanctuary" };
        var upperRoom = new WorkOrder { Creator = employee, Number = upperRoomNumber, RoomNumber = "SANCTUARY" };
        var mixedRoom = new WorkOrder { Creator = employee, Number = mixedRoomNumber, RoomNumber = "Sanctuary" };
        var differentRoom = new WorkOrder { Creator = employee, Number = differentRoomNumber, RoomNumber = "kitchen" };

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

        orders.Select(o => o.Number).ShouldBe([lowerRoomNumber, upperRoomNumber, mixedRoomNumber], ignoreOrder: true);
    }

    [Test]
    public async Task ShouldFilterByNoRoomWhenRoomIsNull()
    {
        var withRoomNumber = TestData.Number("S");
        var nullRoomNumber = TestData.Number("S");
        var emptyRoomNumber = TestData.Number("S");
        var whitespaceRoomNumber = TestData.Number("S");

        var employee = new Employee(TestData.UserName("employee1"), "1", "1", "1");
        var withRoom = new WorkOrder { Creator = employee, Number = withRoomNumber, RoomNumber = "101" };
        var nullRoom = new WorkOrder { Creator = employee, Number = nullRoomNumber, RoomNumber = null };
        var emptyRoom = new WorkOrder { Creator = employee, Number = emptyRoomNumber, RoomNumber = "" };
        var whitespaceRoom = new WorkOrder { Creator = employee, Number = whitespaceRoomNumber, RoomNumber = "   " };

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

        orders.Select(o => o.Number).ShouldBe([nullRoomNumber, emptyRoomNumber, whitespaceRoomNumber], ignoreOrder: true);
    }

    [Test]
    public async Task ShouldFilterByNoBuilding()
    {
        var withBuildingNumber = TestData.Number("S");
        var nullBuildingNumber = TestData.Number("S");
        var emptyBuildingNumber = TestData.Number("S");
        var whitespaceBuildingNumber = TestData.Number("S");

        var employee = new Employee(TestData.UserName("employee1"), "1", "1", "1");
        var withBuilding = new WorkOrder { Creator = employee, Number = withBuildingNumber, Building = "Sanctuary" };
        var nullBuilding = new WorkOrder { Creator = employee, Number = nullBuildingNumber, Building = null };
        var emptyBuilding = new WorkOrder { Creator = employee, Number = emptyBuildingNumber, Building = "" };
        var whitespaceBuilding = new WorkOrder { Creator = employee, Number = whitespaceBuildingNumber, Building = "   " };

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

        orders.Select(o => o.Number).ShouldBe([nullBuildingNumber, emptyBuildingNumber, whitespaceBuildingNumber], ignoreOrder: true);
    }

    [Test]
    public async Task ShouldMatchRoomExactWhenBothRoomAndNoBuildingAreSpecified()
    {
        var sanctuaryRoom101Number = TestData.Number("S");
        var noRoomWithBuildingNumber = TestData.Number("S");
        var room101NoBuildingNumber = TestData.Number("S");
        var room101NoRoomNumber = TestData.Number("S");

        var employee = new Employee(TestData.UserName("employee1"), "1", "1", "1");
        var sanctuaryRoom101 = new WorkOrder { Creator = employee, Number = sanctuaryRoom101Number, Building = "Sanctuary", RoomNumber = "101" };
        var noRoomWithBuilding = new WorkOrder { Creator = employee, Number = noRoomWithBuildingNumber, Building = "Sanctuary", RoomNumber = null };
        var room101NoBuilding = new WorkOrder { Creator = employee, Number = room101NoBuildingNumber, Building = null, RoomNumber = "101" };
        var room101NoRoom = new WorkOrder { Creator = employee, Number = room101NoRoomNumber, Building = null, RoomNumber = null };

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

        orders.Select(o => o.Number).ShouldBe([sanctuaryRoom101Number, room101NoBuildingNumber], ignoreOrder: true);
    }

    [Test]
    public async Task ShouldCombineRoomFilterWithOpenOnlyFilter()
    {
        var openRoom101Number = TestData.Number("S");
        var completeRoom101Number = TestData.Number("S");
        var openRoom102Number = TestData.Number("S");
        var draftRoom101Number = TestData.Number("S");

        var employee = new Employee(TestData.UserName("employee1"), "1", "1", "1");
        var openRoom101 = new WorkOrder { Creator = employee, Number = openRoom101Number, RoomNumber = "101", Status = WorkOrderStatus.Assigned };
        var completeRoom101 = new WorkOrder { Creator = employee, Number = completeRoom101Number, RoomNumber = "101", Status = WorkOrderStatus.Complete };
        var openRoom102 = new WorkOrder { Creator = employee, Number = openRoom102Number, RoomNumber = "102", Status = WorkOrderStatus.Draft };
        var draftRoom101 = new WorkOrder { Creator = employee, Number = draftRoom101Number, RoomNumber = "101", Status = WorkOrderStatus.Draft };

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

        orders.Select(o => o.Number).ShouldBe([openRoom101Number, draftRoom101Number], ignoreOrder: true);
    }

    private static async Task<(Employee Employee, string[] Numbers)> SeedWaitingOnVendorOrdersAsync()
    {
        var employee = new Employee(TestData.UserName("vendor"), "1", "1", "1");
        var numbers = TestData.NumberSeries(3);
        var since = new DateTime(2026, 10, 14, 9, 0, 0);
        var waiting = new WorkOrder { Creator = employee, Assignee = employee, Number = numbers[0], Status = WorkOrderStatus.InProgress, WaitingOnVendorSince = since };
        var inProgress = new WorkOrder { Creator = employee, Assignee = employee, Number = numbers[1], Status = WorkOrderStatus.InProgress };
        var completeWithStaleField = new WorkOrder { Creator = employee, Assignee = employee, Number = numbers[2], Status = WorkOrderStatus.Complete, WaitingOnVendorSince = since };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(employee);
        context.AddRange(waiting, inProgress, completeWithStaleField);
        await context.SaveChangesAsync();
        return (employee, numbers);
    }

    [Test]
    public async Task ShouldReturnWaitingOnVendorOrder_WhenOpenOnly()
    {
        var (employee, numbers) = await SeedWaitingOnVendorOrdersAsync();

        var handler = new WorkOrderSearchHandler(TestHost.GetRequiredService<DataContext>(), TimeProvider.System);
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchOpenOnly(true);
        specification.MatchCreator(employee);
        var orders = await handler.Handle(specification);

        orders.Select(o => o.Number).ShouldBe([numbers[0], numbers[1]], ignoreOrder: true);
    }

    [Test]
    public async Task ShouldReturnWaitingOnVendorOrder_WhenStatusIsInProgress()
    {
        var (employee, numbers) = await SeedWaitingOnVendorOrdersAsync();

        var handler = new WorkOrderSearchHandler(TestHost.GetRequiredService<DataContext>(), TimeProvider.System);
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchStatus(WorkOrderStatus.InProgress);
        specification.MatchCreator(employee);
        var orders = await handler.Handle(specification);

        orders.Select(o => o.Number).ShouldBe([numbers[0], numbers[1]], ignoreOrder: true);
    }

    [Test]
    public async Task ShouldReturnOnlyWaitingOnVendorOrders_WhenWaitingOnVendorOnly()
    {
        var (employee, numbers) = await SeedWaitingOnVendorOrdersAsync();

        var handler = new WorkOrderSearchHandler(TestHost.GetRequiredService<DataContext>(), TimeProvider.System);
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchWaitingOnVendorOnly(true);
        specification.MatchCreator(employee);
        var orders = await handler.Handle(specification);

        orders.Select(o => o.Number).ShouldBe([numbers[0]]);
    }
}
