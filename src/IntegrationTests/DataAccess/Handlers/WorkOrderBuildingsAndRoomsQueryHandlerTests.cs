using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class WorkOrderBuildingsAndRoomsQueryHandlerTests
{
    private readonly DateOnly _testStartDate = new(2000, 1, 1);

    [Test]
    public async Task ShouldReturnAllBuildingsAndRoomsWithCounts()
    {
        new DatabaseTests().Clean();
        var creator = new Employee("barcreator", "Building", "Room", "bar@example.com");
        var sanctuary = "Sanctuary";
        var kitchen = "Kitchen";

        var orders = new List<WorkOrder>
        {
            // Sanctuary, Room A - Open and Overdue
            new()
            {
                Creator = creator,
                Number = "O001",
                Title = "Sanctuary A Open",
                Building = sanctuary,
                RoomNumber = "Room A",
                Status = WorkOrderStatus.Draft,
                DueDate = _testStartDate.AddDays(-5) // Overdue
            },
            // Sanctuary, Room A - Open but not overdue
            new()
            {
                Creator = creator,
                Number = "O002",
                Title = "Sanctuary A Future",
                Building = sanctuary,
                RoomNumber = "Room A",
                Status = WorkOrderStatus.Assigned,
                DueDate = _testStartDate.AddDays(10) // Future
            },
            // Sanctuary, Room B - In Progress and Overdue
            new()
            {
                Creator = creator,
                Number = "O003",
                Title = "Sanctuary B Overdue",
                Building = sanctuary,
                RoomNumber = "Room B",
                Status = WorkOrderStatus.InProgress,
                DueDate = _testStartDate.AddDays(-10)
            },
            // Sanctuary, No Room
            new()
            {
                Creator = creator,
                Number = "O004",
                Title = "Sanctuary No Room",
                Building = sanctuary,
                RoomNumber = null,
                Status = WorkOrderStatus.Draft,
                DueDate = _testStartDate.AddDays(5)
            },
            // Kitchen, Room X - Completed within 30 days
            new()
            {
                Creator = creator,
                Number = "O005",
                Title = "Kitchen X Completed",
                Building = kitchen,
                RoomNumber = "Room X",
                Status = WorkOrderStatus.Complete,
                CompletedDate = _testStartDate.AddDays(-10).ToDateTime(TimeOnly.MinValue),
                DueDate = _testStartDate
            },
            // Kitchen, Room Y - Completed beyond 30 days (should not count)
            new()
            {
                Creator = creator,
                Number = "O006",
                Title = "Kitchen Y Old",
                Building = kitchen,
                RoomNumber = "Room Y",
                Status = WorkOrderStatus.Complete,
                CompletedDate = _testStartDate.AddDays(-40).ToDateTime(TimeOnly.MinValue),
                DueDate = _testStartDate
            },
            // No Building, Room Z - Open
            new()
            {
                Creator = creator,
                Number = "O007",
                Title = "No Building Z",
                Building = null,
                RoomNumber = "Room Z",
                Status = WorkOrderStatus.Assigned,
                DueDate = _testStartDate.AddDays(15)
            },
            // No Building, No Room - Overdue
            new()
            {
                Creator = creator,
                Number = "O008",
                Title = "No Building No Room",
                Building = null,
                RoomNumber = null,
                Status = WorkOrderStatus.Draft,
                DueDate = _testStartDate.AddDays(-3)
            }
        };

        await SaveAsync(creator, orders);

        var result = await Handle();

        result.ShouldNotBeNull();
        result.Length.ShouldBe(3); // Sanctuary, Kitchen, No building

        // Find buildings
        var sanctuaryData = result.First(b => b.BuildingName == sanctuary);
        var kitchenData = result.First(b => b.BuildingName == kitchen);
        var noBuildingData = result.First(b => b.BuildingName == "No building");

        // Sanctuary checks
        sanctuaryData.Rooms.Length.ShouldBe(3); // Room A, Room B, No room
        var sanctuaryRoomA = sanctuaryData.Rooms.First(r => r.RoomName == "Room A");
        sanctuaryRoomA.OpenCount.ShouldBe(2); // O001, O002
        sanctuaryRoomA.OverdueCount.ShouldBe(1); // O001
        sanctuaryRoomA.CompletedLast30DaysCount.ShouldBe(0);

        var sanctuaryRoomB = sanctuaryData.Rooms.First(r => r.RoomName == "Room B");
        sanctuaryRoomB.OpenCount.ShouldBe(1); // O003
        sanctuaryRoomB.OverdueCount.ShouldBe(1); // O003
        sanctuaryRoomB.CompletedLast30DaysCount.ShouldBe(0);

        var sanctuaryNoRoom = sanctuaryData.Rooms.First(r => r.RoomName == "No room");
        sanctuaryNoRoom.OpenCount.ShouldBe(1); // O004
        sanctuaryNoRoom.OverdueCount.ShouldBe(0);
        sanctuaryNoRoom.CompletedLast30DaysCount.ShouldBe(0);

        // Kitchen checks
        kitchenData.Rooms.Length.ShouldBe(2); // Room X, Room Y
        var kitchenRoomX = kitchenData.Rooms.First(r => r.RoomName == "Room X");
        kitchenRoomX.OpenCount.ShouldBe(0);
        kitchenRoomX.OverdueCount.ShouldBe(0);
        kitchenRoomX.CompletedLast30DaysCount.ShouldBe(1); // O005

        var kitchenRoomY = kitchenData.Rooms.First(r => r.RoomName == "Room Y");
        kitchenRoomY.OpenCount.ShouldBe(0);
        kitchenRoomY.OverdueCount.ShouldBe(0);
        kitchenRoomY.CompletedLast30DaysCount.ShouldBe(0); // O006 is too old

        // No Building checks
        noBuildingData.Rooms.Length.ShouldBe(2); // Room Z, No room
        var noBuildingRoomZ = noBuildingData.Rooms.First(r => r.RoomName == "Room Z");
        noBuildingRoomZ.OpenCount.ShouldBe(1); // O007
        noBuildingRoomZ.OverdueCount.ShouldBe(0);
        noBuildingRoomZ.CompletedLast30DaysCount.ShouldBe(0);

        var noBuildingNoRoom = noBuildingData.Rooms.First(r => r.RoomName == "No room");
        noBuildingNoRoom.OpenCount.ShouldBe(1); // O008
        noBuildingNoRoom.OverdueCount.ShouldBe(1); // O008
        noBuildingNoRoom.CompletedLast30DaysCount.ShouldBe(0);
    }

    [Test]
    public async Task ShouldCountOpenOrdersInDraftAssignedAndInProgressStatuses()
    {
        new DatabaseTests().Clean();
        var creator = new Employee("opencounter", "Open", "Counter", "oc@example.com");
        var building = "TestBuilding";
        var room = "TestRoom";

        var orders = new List<WorkOrder>
        {
            new()
            {
                Creator = creator,
                Number = "DRAFT001",
                Title = "Draft Order",
                Building = building,
                RoomNumber = room,
                Status = WorkOrderStatus.Draft,
                DueDate = _testStartDate.AddDays(5)
            },
            new()
            {
                Creator = creator,
                Number = "ASSIGN001",
                Title = "Assigned Order",
                Building = building,
                RoomNumber = room,
                Status = WorkOrderStatus.Assigned,
                DueDate = _testStartDate.AddDays(5)
            },
            new()
            {
                Creator = creator,
                Number = "INPROG001",
                Title = "In Progress Order",
                Building = building,
                RoomNumber = room,
                Status = WorkOrderStatus.InProgress,
                DueDate = _testStartDate.AddDays(5)
            },
            new()
            {
                Creator = creator,
                Number = "COMPLETE001",
                Title = "Completed Order",
                Building = building,
                RoomNumber = room,
                Status = WorkOrderStatus.Complete,
                DueDate = _testStartDate.AddDays(5),
                CompletedDate = _testStartDate.ToDateTime(TimeOnly.MinValue)
            },
            new()
            {
                Creator = creator,
                Number = "CANCEL001",
                Title = "Cancelled Order",
                Building = building,
                RoomNumber = room,
                Status = WorkOrderStatus.Cancelled,
                DueDate = _testStartDate.AddDays(5)
            }
        };

        await SaveAsync(creator, orders);

        var result = await Handle();

        var roomData = result.First(b => b.BuildingName == building).Rooms.First(r => r.RoomName == room);
        roomData.OpenCount.ShouldBe(3); // Draft, Assigned, InProgress
    }

    [Test]
    public async Task ShouldCountOverdueOrdersWithOpenStatusAndPastDueDate()
    {
        new DatabaseTests().Clean();
        var creator = new Employee("overduecounter", "Overdue", "Counter", "oc@example.com");
        var building = "TestBuilding";
        var room = "TestRoom";

        var orders = new List<WorkOrder>
        {
            // Overdue Draft
            new()
            {
                Creator = creator,
                Number = "OVER_DRAFT001",
                Title = "Overdue Draft",
                Building = building,
                RoomNumber = room,
                Status = WorkOrderStatus.Draft,
                DueDate = _testStartDate.AddDays(-5)
            },
            // Overdue Assigned
            new()
            {
                Creator = creator,
                Number = "OVER_ASSIGN001",
                Title = "Overdue Assigned",
                Building = building,
                RoomNumber = room,
                Status = WorkOrderStatus.Assigned,
                DueDate = _testStartDate.AddDays(-3)
            },
            // Overdue InProgress
            new()
            {
                Creator = creator,
                Number = "OVER_INPROG001",
                Title = "Overdue InProgress",
                Building = building,
                RoomNumber = room,
                Status = WorkOrderStatus.InProgress,
                DueDate = _testStartDate.AddDays(-1)
            },
            // Not overdue (future due date)
            new()
            {
                Creator = creator,
                Number = "FUTURE001",
                Title = "Future Due Date",
                Building = building,
                RoomNumber = room,
                Status = WorkOrderStatus.Draft,
                DueDate = _testStartDate.AddDays(5)
            },
            // Completed but was overdue (should not count as overdue)
            new()
            {
                Creator = creator,
                Number = "COMPLETE_LATE001",
                Title = "Completed Late",
                Building = building,
                RoomNumber = room,
                Status = WorkOrderStatus.Complete,
                DueDate = _testStartDate.AddDays(-10),
                CompletedDate = _testStartDate.AddDays(-5).ToDateTime(TimeOnly.MinValue)
            }
        };

        await SaveAsync(creator, orders);

        var result = await Handle();

        var roomData = result.First(b => b.BuildingName == building).Rooms.First(r => r.RoomName == room);
        roomData.OverdueCount.ShouldBe(3); // Three overdue open orders
    }

    [Test]
    public async Task ShouldCountCompletedOrdersWithinThirtyDays()
    {
        new DatabaseTests().Clean();
        var creator = new Employee("completedcounter", "Completed", "Counter", "cc@example.com");
        var building = "TestBuilding";
        var room = "TestRoom";

        var orders = new List<WorkOrder>
        {
            // Completed 5 days ago (within 30)
            new()
            {
                Creator = creator,
                Number = "COMP_RECENT001",
                Title = "Recently Completed",
                Building = building,
                RoomNumber = room,
                Status = WorkOrderStatus.Complete,
                CompletedDate = _testStartDate.AddDays(-5).ToDateTime(TimeOnly.MinValue),
                DueDate = _testStartDate.AddDays(-10)
            },
            // Completed 30 days ago (at boundary, should count with >= comparison)
            new()
            {
                Creator = creator,
                Number = "COMP_BOUNDARY001",
                Title = "Boundary Completed",
                Building = building,
                RoomNumber = room,
                Status = WorkOrderStatus.Complete,
                CompletedDate = _testStartDate.AddDays(-30).ToDateTime(TimeOnly.MinValue),
                DueDate = _testStartDate.AddDays(-35)
            },
            // Completed 31 days ago (outside window)
            new()
            {
                Creator = creator,
                Number = "COMP_OLD001",
                Title = "Old Completed",
                Building = building,
                RoomNumber = room,
                Status = WorkOrderStatus.Complete,
                CompletedDate = _testStartDate.AddDays(-31).ToDateTime(TimeOnly.MinValue),
                DueDate = _testStartDate.AddDays(-36)
            },
            // Completed without CompletedDate (should not count)
            new()
            {
                Creator = creator,
                Number = "COMP_NODATE001",
                Title = "No Completed Date",
                Building = building,
                RoomNumber = room,
                Status = WorkOrderStatus.Complete,
                CompletedDate = null,
                DueDate = _testStartDate.AddDays(-10)
            },
            // Open order (should not count)
            new()
            {
                Creator = creator,
                Number = "OPEN001",
                Title = "Still Open",
                Building = building,
                RoomNumber = room,
                Status = WorkOrderStatus.Draft,
                DueDate = _testStartDate.AddDays(5)
            }
        };

        await SaveAsync(creator, orders);

        var result = await Handle();

        var roomData = result.First(b => b.BuildingName == building).Rooms.First(r => r.RoomName == room);
        roomData.CompletedLast30DaysCount.ShouldBe(2); // Recent and Boundary
    }

    [Test]
    public async Task ShouldHandleEmptyDatabase()
    {
        new DatabaseTests().Clean();

        var result = await Handle();

        result.ShouldBeEmpty();
    }

    [Test]
    public async Task ShouldOrderBuildingsWithNoBuildingFirst()
    {
        new DatabaseTests().Clean();
        var creator = new Employee("ordertest", "Order", "Test", "ot@example.com");

        var orders = new List<WorkOrder>
        {
            new()
            {
                Creator = creator,
                Number = "Z001",
                Title = "Z Building",
                Building = "Z Building",
                RoomNumber = "Room1",
                Status = WorkOrderStatus.Draft,
                DueDate = _testStartDate
            },
            new()
            {
                Creator = creator,
                Number = "A001",
                Title = "A Building",
                Building = "A Building",
                RoomNumber = "Room1",
                Status = WorkOrderStatus.Draft,
                DueDate = _testStartDate
            },
            new()
            {
                Creator = creator,
                Number = "NONE001",
                Title = "No Building",
                Building = null,
                RoomNumber = "Room1",
                Status = WorkOrderStatus.Draft,
                DueDate = _testStartDate
            }
        };

        await SaveAsync(creator, orders);

        var result = await Handle();

        result[0].BuildingName.ShouldBe("No building");
        result[1].BuildingName.ShouldBe("A Building");
        result[2].BuildingName.ShouldBe("Z Building");
    }

    private static async Task<BuildingRoomData[]> Handle()
    {
        var handler = new WorkOrderBuildingsAndRoomsQueryHandler(
            TestHost.GetRequiredService<DataContext>(),
            TestHost.GetRequiredService<TimeProvider>());
        return await handler.Handle(new WorkOrderBuildingsAndRoomsQuery());
    }

    private static async Task SaveAsync(Employee creator, List<WorkOrder> orders)
    {
        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.AddRange(orders);
        await context.SaveChangesAsync();
    }
}
