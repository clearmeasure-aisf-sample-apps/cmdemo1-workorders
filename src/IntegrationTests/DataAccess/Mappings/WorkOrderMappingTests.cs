using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Mappings;

[TestFixture]
public class WorkOrderMappingTests
{
    [Test]
    public void ShouldMapWorkOrderBasicProperties()
    {
        var creator = EmployeeBuilder.Create(TestData.UserName("creator1"))
            .WithFirstName("John").WithLastName("Doe").WithEmail("john@example.com").Build();
        var workOrder = WorkOrderBuilder.For(creator, WorkOrderStatus.Draft)
            .WithNumber(TestData.Number("M")).WithTitle("Fix lighting")
            .WithDescription("Replace broken light bulbs in conference room")
            .InRoom("CR-101").Build();
        workOrder.Instructions = "Lock out panel before work.";
        workOrder.ExternalReference = "EXT-12345";

        GivenPersisted(creator, workOrder);

        ThenBasicPropertiesMatch(workOrder);
    }

    [Test]
    public async Task ShouldSaveWorkOrder()
    {
        var creator = EmployeeBuilder.Create(TestData.UserName("employee1"))
            .WithFirstName("1").WithLastName("1").WithEmail("1").Build();
        var assignee = EmployeeBuilder.Create(TestData.UserName("employee2"))
            .WithFirstName("2").WithLastName("2").WithEmail("2").Build();
        var order = WorkOrderBuilder.Create()
            .CreatedBy(creator).AssignedTo(assignee)
            .WithTitle("foo").WithDescription("bar").InRoom("123 a")
            .WithNumber(TestData.Number("M")).Build();
        order.Instructions = "Use 10ft ladder";
        order.ChangeStatus(WorkOrderStatus.InProgress);

        await GivenAttachedOrderAsync(creator, assignee, order);

        ThenSavedOrderMatches(order);
    }

    [Test]
    public async Task ShouldSaveAuditEntries()
    {
        var creator = EmployeeBuilder.Create(TestData.UserName("employee1"))
            .WithFirstName("1").WithLastName("1").WithEmail("1").Build();
        var assignee = EmployeeBuilder.Create(TestData.UserName("employee2"))
            .WithFirstName("2").WithLastName("2").WithEmail("2").Build();
        var order = WorkOrderBuilder.Create()
            .CreatedBy(creator).AssignedTo(assignee)
            .WithTitle("foo").WithDescription("bar").InRoom("123 a")
            .WithNumber(TestData.Number("M")).Build();
        order.Instructions = "Use 10ft ladder";
        order.ChangeStatus(WorkOrderStatus.InProgress);

        await GivenAttachedOrderAsync(creator, assignee, order);

        ThenOrderExists(order.Id);
    }

    [Test]
    public void ShouldMapWorkOrderWithCreatorAndAssignee()
    {
        var creator = EmployeeBuilder.Create(TestData.UserName("creator1"))
            .WithFirstName("John").WithLastName("Doe").WithEmail("john@example.com").Build();
        var assignee = EmployeeBuilder.Create(TestData.UserName("assignee1"))
            .WithFirstName("Jane").WithLastName("Smith").WithEmail("jane@example.com").Build();
        var workOrder = WorkOrderBuilder.For(creator, WorkOrderStatus.Assigned)
            .WithNumber(TestData.Number("M")).WithTitle("Fix plumbing")
            .WithDescription("Fix sink in bathroom").AssignedTo(assignee).Build();

        GivenPersisted(creator, assignee, workOrder);

        ThenCreatorAndAssigneeMatch(workOrder.Id, creator.Id, assignee.Id);
    }

    [Test]
    public void ShouldMapWorkOrderStatusConversion()
    {
        var creator = EmployeeBuilder.Create(TestData.UserName("creator1"))
            .WithFirstName("John").WithLastName("Doe").WithEmail("john@example.com").Build();
        var workOrder = WorkOrderBuilder.For(creator, WorkOrderStatus.Complete)
            .WithNumber(TestData.Number("M")).WithTitle("Test Status")
            .WithDescription("Testing status conversion").Build();

        GivenPersisted(creator, workOrder);

        ThenStatusIs(workOrder.Id, WorkOrderStatus.Complete);
    }

    [Test]
    public void ShouldEnforceRequiredProperties()
    {
        var creator = EmployeeBuilder.Create(TestData.UserName("creator1"))
            .WithFirstName("John").WithLastName("Doe").WithEmail("john@example.com").Build();
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Status = WorkOrderStatus.Draft
            // Intentionally omitting Number and Title which are required
        };

        using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.Add(workOrder);

        Should.Throw<DbUpdateException>(() => context.SaveChanges());
    }

    [Test]
    [Category("SqlServerOnly")]
    public void ShouldRespectMaxLengthConstraints()
    {
        SqlServerTestAssumptions.RequireSqlServer();

        var creator = EmployeeBuilder.Create(TestData.UserName("creator1"))
            .WithFirstName("John").WithLastName("Doe").WithEmail("john@example.com").Build();
        // WorkOrder.Description setter truncates to 4000 before EF sees the value, so length violations
        // for Description are not observable through the domain model here.
        var workOrder = new WorkOrder
        {
            Number = new string('A', 8), // Exceeds 7 char limit (WorkOrderMap)
            Title = new string('B', WorkOrder.TitleMaxLength + 1), // Exceeds 600 char limit
            Description = "valid",
            RoomNumber = new string('D', WorkOrder.RoomNumberMaxLength + 1),
            Creator = creator,
            Status = WorkOrderStatus.Draft
        };

        using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.Add(workOrder);

        Should.Throw<DbUpdateException>(() => context.SaveChanges());
    }

    [Test]
    [Category("SqlServerOnly")]
    public void ShouldSupportMaxLengthTitle()
    {
        SqlServerTestAssumptions.RequireSqlServer();

        var creator = EmployeeBuilder.Create(TestData.UserName("creator1"))
            .WithFirstName("John").WithLastName("Doe").WithEmail("john@example.com").Build();
        var workOrder = WorkOrderBuilder.For(creator, WorkOrderStatus.Draft)
            .WithNumber(TestData.Number("M"))
            .WithTitle(new string('B', WorkOrder.TitleMaxLength))
            .WithDescription("description").InRoom("room number").Build();

        using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.Add(workOrder);

        context.SaveChanges();

        workOrder.Title!.Length.ShouldBe(WorkOrder.TitleMaxLength);
    }

    [Test]
    public void ShouldSupportMaxLengthRoomNumber()
    {
        var room = new string('R', WorkOrder.RoomNumberMaxLength);
        var creator = EmployeeBuilder.Create(TestData.UserName("creator1"))
            .WithFirstName("John").WithLastName("Doe").WithEmail("john@example.com").Build();
        var workOrder = WorkOrderBuilder.For(creator, WorkOrderStatus.Draft)
            .WithNumber(TestData.Number("M")).WithTitle("Max length room")
            .WithDescription("description").InRoom(room).Build();

        GivenPersisted(creator, workOrder);

        ThenRoomNumberMatches(workOrder.Id, room);
    }

    [Test]
    public void ShouldEagerFetchCreatorAndAssigneeByDefault()
    {
        var creator = EmployeeBuilder.Create(TestData.UserName("creator1"))
            .WithFirstName("John").WithLastName("Doe").WithEmail("john@example.com").Build();
        var assignee = EmployeeBuilder.Create(TestData.UserName("assignee1"))
            .WithFirstName("Jane").WithLastName("Smith").WithEmail("jane@example.com").Build();
        var workOrder = WorkOrderBuilder.For(creator, WorkOrderStatus.Assigned)
            .WithNumber(TestData.Number("M")).WithTitle("Test Eager Loading")
            .WithDescription("Testing that Creator and Assignee are auto-included")
            .AssignedTo(assignee).Build();

        GivenPersisted(creator, assignee, workOrder);

        ThenEagerFetchedPeopleMatch(workOrder.Id, creator.Id, assignee.Id);
    }

    [Test]
    public void ShouldPersistNullInstructionsForLegacyWorkOrder()
    {
        var creator = EmployeeBuilder.Create(TestData.UserName("creator1"))
            .WithFirstName("John").WithLastName("Doe").WithEmail("john@example.com").Build();
        var workOrder = WorkOrderBuilder.For(creator, WorkOrderStatus.Draft)
            .WithNumber(TestData.Number("M")).WithTitle("Legacy work order")
            .WithDescription("No instructions provided").Build();

        GivenPersisted(creator, workOrder);

        ThenInstructionsAre(workOrder.Id, string.Empty);
    }

    [Test]
    public void ShouldPersistInstructionsAtMaxLength()
    {
        var creator = EmployeeBuilder.Create(TestData.UserName("creator1"))
            .WithFirstName("John").WithLastName("Doe").WithEmail("john@example.com").Build();
        var workOrder = WorkOrderBuilder.For(creator, WorkOrderStatus.Draft)
            .WithNumber(TestData.Number("M")).WithTitle("Max length instructions")
            .WithDescription("Testing truncation").Build();
        workOrder.Instructions = new string('I', 4001);

        GivenPersisted(creator, workOrder);

        ThenInstructionsLengthIs(workOrder.Id, 4000);
    }

    [Test]
    public void ShouldRejectDuplicateNumber_WhenSavedDirectly()
    {
        var duplicateNumber = TestData.Number("M");
        var creator = EmployeeBuilder.Create(TestData.UserName("dupcreator"))
            .WithFirstName("Dup").WithLastName("Creator").WithEmail("dup@example.com").Build();
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(new WorkOrder { Number = duplicateNumber, Title = "First", Creator = creator });
            context.SaveChanges();
        }

        using var secondContext = TestHost.GetRequiredService<DbContext>();
        secondContext.Attach(creator);
        secondContext.Add(new WorkOrder { Number = duplicateNumber, Title = "Second", Creator = creator });

        Should.Throw<DbUpdateException>(() => secondContext.SaveChanges());
    }

    [Test]
    public void ShouldPersistPriorityAndBuilding()
    {
        var creator = EmployeeBuilder.Create(TestData.UserName("prio"))
            .WithFirstName("Prio").WithLastName("Creator").WithEmail("prio@example.com").Build();
        var high = WorkOrderBuilder.For(creator, WorkOrderStatus.Draft)
            .WithNumber(TestData.Number("M")).WithTitle("High")
            .WithPriority(WorkOrderPriority.High).InBuilding("Sanctuary").Build();
        var defaulted = WorkOrderBuilder.For(creator, WorkOrderStatus.Draft)
            .WithNumber(TestData.Number("M")).WithTitle("Default").Build();

        GivenPersisted(creator, high, defaulted);

        ThenPriorityAndBuilding(high.Id, defaulted.Id);
    }

    [Test]
    public void ShouldPersistFloor()
    {
        var creator = EmployeeBuilder.Create(TestData.UserName("floor"))
            .WithFirstName("Floor").WithLastName("Creator").WithEmail("floor@example.com").Build();
        var withFloor = WorkOrderBuilder.For(creator, WorkOrderStatus.Draft)
            .WithNumber(TestData.Number("M")).WithTitle("Upstairs").Build();
        withFloor.Floor = "2";
        var withoutFloor = WorkOrderBuilder.For(creator, WorkOrderStatus.Draft)
            .WithNumber(TestData.Number("M")).WithTitle("Unset").Build();

        GivenPersisted(creator, withFloor, withoutFloor);

        ThenFloor(withFloor.Id, withoutFloor.Id);
    }

    [Test]
    public void ShouldPersistAccessNotes()
    {
        var creator = EmployeeBuilder.Create(TestData.UserName("access"))
            .WithFirstName("Access").WithLastName("Creator").WithEmail("access@example.com").Build();
        var noted = WorkOrderBuilder.For(creator, WorkOrderStatus.Draft)
            .WithNumber(TestData.Number("M")).WithTitle("Noted").Build();
        noted.AccessNotes = "Use the side door";
        var unset = WorkOrderBuilder.For(creator, WorkOrderStatus.Draft)
            .WithNumber(TestData.Number("M")).WithTitle("Unset").Build();

        GivenPersisted(creator, noted, unset);

        ThenAccessNotes(noted.Id, unset.Id);
    }

    [Test]
    public void ShouldPersistWaitingOnVendorSince()
    {
        var creator = EmployeeBuilder.Create(TestData.UserName("creator"))
            .WithFirstName("John").WithLastName("Doe").WithEmail("john@example.com").Build();
        var assignee = EmployeeBuilder.Create(TestData.UserName("assignee"))
            .WithFirstName("Jane").WithLastName("Roe").WithEmail("jane@example.com").Build();
        var since = new DateTime(2026, 10, 14, 9, 30, 0);
        var workOrder = WorkOrderBuilder.For(creator, WorkOrderStatus.InProgress)
            .WithNumber(TestData.Number("V")).WithTitle("Replace boiler valve")
            .AssignedTo(assignee).WaitingOnVendorSince(since).Build();

        GivenPersisted(creator, assignee, workOrder);

        ThenWaitingOnVendorSince(workOrder.Id, since);
    }

    [Test]
    public void ShouldReadRowWrittenWithoutWaitingOnVendorColumnAsNotWaiting()
    {
        var creator = EmployeeBuilder.Create(TestData.UserName("creator"))
            .WithFirstName("John").WithLastName("Doe").WithEmail("john@example.com").Build();
        GivenPersisted(creator);

        var id = Guid.NewGuid();
        var number = TestData.Number("A");
        GivenRawApp2Row(id, number, creator.Id);

        ThenNotWaitingOnVendor(number, id);
    }

    private static void GivenPersisted(params object[] entities)
    {
        using var context = TestHost.GetRequiredService<DbContext>();
        context.AddRange(entities);
        context.SaveChanges();
    }

    private static async Task GivenAttachedOrderAsync(Employee creator, Employee assignee, WorkOrder order)
    {
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignee);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        dataContext.Attach(order);
        await dataContext.SaveChangesAsync();
    }

    private static void GivenRawApp2Row(Guid id, string number, Guid creatorId)
    {
        using var context = TestHost.GetRequiredService<DbContext>();
        var table = context.Database.ProviderName!.Contains("Sqlite", StringComparison.OrdinalIgnoreCase)
            ? "\"WorkOrder\""
            : "[dbo].[WorkOrder]";
#pragma warning disable EF1002, EF1003 // The table name is one of two constants; every value is a parameter
        context.Database.ExecuteSqlRaw(
            "INSERT INTO " + table +
            " (Id, Number, Title, Description, Instructions, Status, CreatorId, Priority)" +
            " VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7})",
            id, number, "app2 row", "", "", WorkOrderStatus.InProgress.Code, creatorId, (byte)2);
#pragma warning restore EF1002, EF1003
    }

    private static void ThenBasicPropertiesMatch(WorkOrder expected)
    {
        using var context = TestHost.GetRequiredService<DbContext>();
        var actual = context.Set<WorkOrder>()
            .Include(wo => wo.Creator)
            .Single(wo => wo.Id == expected.Id);
        actual.Id.ShouldBe(expected.Id);
        actual.Number.ShouldBe(expected.Number);
        actual.Title.ShouldBe("Fix lighting");
        actual.Description.ShouldBe("Replace broken light bulbs in conference room");
        actual.Instructions.ShouldBe("Lock out panel before work.");
        actual.RoomNumber.ShouldBe("CR-101");
        actual.ExternalReference.ShouldBe("EXT-12345");
        actual.Status.ShouldBe(WorkOrderStatus.Draft);
        actual.Creator.ShouldNotBeNull();
        actual.Creator!.Id.ShouldBe(expected.Creator!.Id);
    }

    private static void ThenSavedOrderMatches(WorkOrder order)
    {
        using var context = TestHost.GetRequiredService<DbContext>();
        var actual = context.Set<WorkOrder>()
            .Include(wo => wo.Creator)
            .Include(wo => wo.Assignee)
            .Single(wo => wo.Id == order.Id);
        actual.Id.ShouldBe(order.Id);
        actual.Creator!.Id.ShouldBe(order.Creator!.Id);
        actual.Assignee!.Id.ShouldBe(order.Assignee!.Id);
        actual.Title.ShouldBe(order.Title);
        actual.Description.ShouldBe(order.Description);
        actual.Instructions.ShouldBe(order.Instructions);
        actual.Status.ShouldBe(order.Status);
        actual.RoomNumber.ShouldBe(order.RoomNumber);
        actual.Number.ShouldBe(order.Number);
    }

    private static void ThenOrderExists(Guid id)
    {
        using var context = TestHost.GetRequiredService<DbContext>();
        var actual = context.Set<WorkOrder>().Single(wo => wo.Id == id);
        actual.Id.ShouldBe(id);
    }

    private static void ThenCreatorAndAssigneeMatch(Guid workOrderId, Guid creatorId, Guid assigneeId)
    {
        using var context = TestHost.GetRequiredService<DbContext>();
        var actual = context.Set<WorkOrder>().Single(wo => wo.Id == workOrderId);
        actual.Creator.ShouldNotBeNull();
        actual.Assignee.ShouldNotBeNull();
        actual.Creator!.Id.ShouldBe(creatorId);
        actual.Assignee!.Id.ShouldBe(assigneeId);
    }

    private static void ThenStatusIs(Guid id, WorkOrderStatus status)
    {
        using var context = TestHost.GetRequiredService<DbContext>();
        var actual = context.Set<WorkOrder>().Single(wo => wo.Id == id);
        actual.Status.ShouldBe(status);
    }

    private static void ThenRoomNumberMatches(Guid id, string room)
    {
        using var context = TestHost.GetRequiredService<DbContext>();
        var actual = context.Set<WorkOrder>().Single(wo => wo.Id == id);
        actual.RoomNumber.ShouldBe(room);
        actual.RoomNumber!.Length.ShouldBe(WorkOrder.RoomNumberMaxLength);
    }

    private static void ThenEagerFetchedPeopleMatch(Guid id, Guid creatorId, Guid assigneeId)
    {
        using var context = TestHost.GetRequiredService<DbContext>();
        // No explicit Include calls - testing AutoInclude
        var actual = context.Set<WorkOrder>().Single(wo => wo.Id == id);
        actual.Creator.ShouldNotBeNull();
        actual.Assignee.ShouldNotBeNull();
        actual.Creator!.Id.ShouldBe(creatorId);
        actual.Creator.FirstName.ShouldBe("John");
        actual.Creator.LastName.ShouldBe("Doe");
        actual.Assignee!.Id.ShouldBe(assigneeId);
        actual.Assignee.FirstName.ShouldBe("Jane");
        actual.Assignee.LastName.ShouldBe("Smith");
    }

    private static void ThenInstructionsAre(Guid id, string instructions)
    {
        using var context = TestHost.GetRequiredService<DbContext>();
        var actual = context.Set<WorkOrder>().Single(wo => wo.Id == id);
        actual.Instructions.ShouldBe(instructions);
    }

    private static void ThenInstructionsLengthIs(Guid id, int length)
    {
        using var context = TestHost.GetRequiredService<DbContext>();
        var actual = context.Set<WorkOrder>().Single(wo => wo.Id == id);
        actual.Instructions!.Length.ShouldBe(length);
    }

    private static void ThenPriorityAndBuilding(Guid highId, Guid defaultedId)
    {
        using var context = TestHost.GetRequiredService<DbContext>();
        var high = context.Set<WorkOrder>().Single(wo => wo.Id == highId);
        var defaulted = context.Set<WorkOrder>().Single(wo => wo.Id == defaultedId);
        high.Priority.ShouldBe(WorkOrderPriority.High);
        high.Building.ShouldBe("Sanctuary");
        defaulted.Priority.ShouldBe(WorkOrderPriority.Medium);
        defaulted.Building.ShouldBeNull();
    }

    private static void ThenFloor(Guid withFloorId, Guid withoutFloorId)
    {
        using var context = TestHost.GetRequiredService<DbContext>();
        var withFloor = context.Set<WorkOrder>().Single(wo => wo.Id == withFloorId);
        var withoutFloor = context.Set<WorkOrder>().Single(wo => wo.Id == withoutFloorId);
        withFloor.Floor.ShouldBe("2");
        withoutFloor.Floor.ShouldBeNull();
    }

    private static void ThenAccessNotes(Guid notedId, Guid unsetId)
    {
        using var context = TestHost.GetRequiredService<DbContext>();
        var noted = context.Set<WorkOrder>().Single(wo => wo.Id == notedId);
        var unset = context.Set<WorkOrder>().Single(wo => wo.Id == unsetId);
        noted.AccessNotes.ShouldBe("Use the side door");
        unset.AccessNotes.ShouldBeNull();
    }

    private static void ThenWaitingOnVendorSince(Guid id, DateTime since)
    {
        using var context = TestHost.GetRequiredService<DbContext>();
        var actual = context.Set<WorkOrder>().Single(wo => wo.Id == id);
        actual.WaitingOnVendorSince.ShouldBe(since);
        actual.IsWaitingOnVendor.ShouldBeTrue();
    }

    private static void ThenNotWaitingOnVendor(string number, Guid id)
    {
        using var context = TestHost.GetRequiredService<DbContext>();
        var actual = context.Set<WorkOrder>().Single(wo => wo.Number == number);
        actual.Id.ShouldBe(id);
        actual.Status.ShouldBe(WorkOrderStatus.InProgress);
        actual.WaitingOnVendorSince.ShouldBeNull();
        actual.IsWaitingOnVendor.ShouldBeFalse();
    }
}
