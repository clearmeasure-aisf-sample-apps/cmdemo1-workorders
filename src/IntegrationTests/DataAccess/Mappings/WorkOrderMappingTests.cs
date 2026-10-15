using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Mappings;

[TestFixture]
public class WorkOrderMappingTests
{
    [Test]
    public void ShouldMapWorkOrderBasicProperties()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        var workOrder = new WorkOrder
        {
            Number = "WO-01",
            Title = "Fix lighting",
            Description = "Replace broken light bulbs in conference room",
            Instructions = "Lock out panel before work.",
            RoomNumber = "CR-101",
            Status = WorkOrderStatus.Draft,
            Creator = creator
        };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(workOrder);
            context.SaveChanges();
        }

        WorkOrder rehydratedWorkOrder;
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            rehydratedWorkOrder = context.Set<WorkOrder>()
                .Include(wo => wo.Creator)
                .Single(wo => wo.Id == workOrder.Id);
        }

        rehydratedWorkOrder.Id.ShouldBe(workOrder.Id);
        rehydratedWorkOrder.Number.ShouldBe("WO-01");
        rehydratedWorkOrder.Title.ShouldBe("Fix lighting");
        rehydratedWorkOrder.Description.ShouldBe("Replace broken light bulbs in conference room");
        rehydratedWorkOrder.Instructions.ShouldBe("Lock out panel before work.");
        rehydratedWorkOrder.RoomNumber.ShouldBe("CR-101");
        rehydratedWorkOrder.Status.ShouldBe(WorkOrderStatus.Draft);
        rehydratedWorkOrder.Creator.ShouldNotBeNull();
        rehydratedWorkOrder.Creator!.Id.ShouldBe(creator.Id);
    }

    [Test]
    public async Task ShouldSaveWorkOrder()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("1", "1", "1", "1");
        var assignee = new Employee("2", "2", "2", "2");
        var order = new WorkOrder
        {
            Creator = creator,
            Assignee = assignee,
            Title = "foo",
            Description = "bar",
            Instructions = "Use 10ft ladder",
            RoomNumber = "123 a"
        };
        order.ChangeStatus(WorkOrderStatus.InProgress);
        order.Number = "123";

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignee);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        dataContext.Attach(order);
        await dataContext.SaveChangesAsync();

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var rehydratedWorkOrder = context.Set<WorkOrder>()
                .Include(wo => wo.Creator)
                .Include(wo => wo.Assignee)
                .Single(wo => wo.Id == order.Id);
            rehydratedWorkOrder.Id.ShouldBe(order.Id);
            rehydratedWorkOrder.Creator!.Id.ShouldBe(order.Creator.Id);
            rehydratedWorkOrder.Assignee!.Id.ShouldBe(order.Assignee.Id);
            rehydratedWorkOrder.Title.ShouldBe(order.Title);
            rehydratedWorkOrder.Description.ShouldBe(order.Description);
            rehydratedWorkOrder.Instructions.ShouldBe(order.Instructions);
            rehydratedWorkOrder.Status.ShouldBe(order.Status);
            rehydratedWorkOrder.RoomNumber.ShouldBe(order.RoomNumber);
            rehydratedWorkOrder.Number.ShouldBe(order.Number);
        }
    }

    [Test]
    public async Task ShouldSaveAuditEntries()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("1", "1", "1", "1");
        var assignee = new Employee("2", "2", "2", "2");
        var order = new WorkOrder
        {
            Creator = creator,
            Assignee = assignee,
            Title = "foo",
            Description = "bar",
            Instructions = "Use 10ft ladder",
            RoomNumber = "123 a"
        };
        order.ChangeStatus(WorkOrderStatus.InProgress);
        order.Number = "123";

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignee);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        dataContext.Attach(order);
        await dataContext.SaveChangesAsync();

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var rehydratedWorkOrder = context.Set<WorkOrder>()
                .Single(wo => wo.Id == order.Id);
            rehydratedWorkOrder.Id.ShouldBe(order.Id);
        }
    }


    [Test]
    public void ShouldMapWorkOrderWithCreatorAndAssignee()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        var assignee = new Employee("assignee1", "Jane", "Smith", "jane@example.com");
        var workOrder = new WorkOrder
        {
            Number = "WO-02",
            Title = "Fix plumbing",
            Description = "Fix sink in bathroom",
            Creator = creator,
            Assignee = assignee,
            Status = WorkOrderStatus.Assigned
        };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignee);
            context.Add(workOrder);
            context.SaveChanges();
        }

        WorkOrder rehydratedWorkOrder;
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            rehydratedWorkOrder = context.Set<WorkOrder>()
                .Single(wo => wo.Id == workOrder.Id);
        }

        rehydratedWorkOrder.Creator.ShouldNotBeNull();
        rehydratedWorkOrder.Assignee.ShouldNotBeNull();
        rehydratedWorkOrder.Creator!.Id.ShouldBe(creator.Id);
        rehydratedWorkOrder.Assignee!.Id.ShouldBe(assignee.Id);
    }

    [Test]
    public void ShouldMapWorkOrderStatusConversion()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        var workOrder = new WorkOrder
        {
            Number = "WO-04",
            Title = "Test Status",
            Description = "Testing status conversion",
            Creator = creator,
            Status = WorkOrderStatus.Complete
        };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(workOrder);
            context.SaveChanges();
        }

        WorkOrder rehydratedWorkOrder;
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            rehydratedWorkOrder = context.Set<WorkOrder>()
                .Single(wo => wo.Id == workOrder.Id);
        }

        rehydratedWorkOrder.Status.ShouldBe(WorkOrderStatus.Complete);
    }

    [Test]
    public void ShouldEnforceRequiredProperties()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
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

        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        // WorkOrder.Description setter truncates to 4000 before EF sees the value, so length violations
        // for Description are not observable through the domain model here.
        var workOrder = new WorkOrder
        {
            Number = new string('A', 8), // Exceeds 7 char limit (WorkOrderMap)
            Title = new string('B', 301), // Exceeds 300 char limit
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

        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        var workOrder = new WorkOrder
        {
            Number = "number",
            Title = new string('B', 300),
            Description = "description",
            RoomNumber = "room number",
            Creator = creator,
            Status = WorkOrderStatus.Draft
        };

        using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.Add(workOrder);

        context.SaveChanges();

        workOrder.Title.Length.ShouldBe(300);
    }

    [Test]
    public void ShouldSupportMaxLengthRoomNumber()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        var room = new string('R', WorkOrder.RoomNumberMaxLength);
        var workOrder = new WorkOrder
        {
            Number = "WO-R9",
            Title = "Max length room",
            Description = "description",
            RoomNumber = room,
            Creator = creator,
            Status = WorkOrderStatus.Draft
        };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(workOrder);
            context.SaveChanges();
        }

        WorkOrder rehydratedWorkOrder;
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            rehydratedWorkOrder = context.Set<WorkOrder>()
                .Single(wo => wo.Id == workOrder.Id);
        }

        rehydratedWorkOrder.RoomNumber.ShouldBe(room);
        rehydratedWorkOrder.RoomNumber!.Length.ShouldBe(WorkOrder.RoomNumberMaxLength);
    }

    [Test]
    public void ShouldEagerFetchCreatorAndAssigneeByDefault()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        var assignee = new Employee("assignee1", "Jane", "Smith", "jane@example.com");
        var workOrder = new WorkOrder
        {
            Number = "WO-06",
            Title = "Test Eager Loading",
            Description = "Testing that Creator and Assignee are auto-included",
            Creator = creator,
            Assignee = assignee,
            Status = WorkOrderStatus.Assigned
        };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignee);
            context.Add(workOrder);
            context.SaveChanges();
        }

        WorkOrder rehydratedWorkOrder;
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            // No explicit Include calls - testing AutoInclude
            rehydratedWorkOrder = context.Set<WorkOrder>()
                .Single(wo => wo.Id == workOrder.Id);
        }

        // Creator and Assignee should be loaded automatically
        rehydratedWorkOrder.Creator.ShouldNotBeNull();
        rehydratedWorkOrder.Assignee.ShouldNotBeNull();
        rehydratedWorkOrder.Creator!.Id.ShouldBe(creator.Id);
        rehydratedWorkOrder.Creator.FirstName.ShouldBe("John");
        rehydratedWorkOrder.Creator.LastName.ShouldBe("Doe");
        rehydratedWorkOrder.Assignee!.Id.ShouldBe(assignee.Id);
        rehydratedWorkOrder.Assignee.FirstName.ShouldBe("Jane");
        rehydratedWorkOrder.Assignee.LastName.ShouldBe("Smith");
    }

    [Test]
    public void ShouldPersistNullInstructionsForLegacyWorkOrder()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        var workOrder = new WorkOrder
        {
            Number = "WO-07",
            Title = "Legacy work order",
            Description = "No instructions provided",
            Creator = creator,
            Status = WorkOrderStatus.Draft
        };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(workOrder);
            context.SaveChanges();
        }

        WorkOrder rehydratedWorkOrder;
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            rehydratedWorkOrder = context.Set<WorkOrder>()
                .Single(wo => wo.Id == workOrder.Id);
        }

        rehydratedWorkOrder.Instructions.ShouldBe(string.Empty);
    }

    [Test]
    public void ShouldPersistInstructionsAtMaxLength()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        var longInstructions = new string('I', 4001);
        var workOrder = new WorkOrder
        {
            Number = "WO-08",
            Title = "Max length instructions",
            Description = "Testing truncation",
            Creator = creator,
            Status = WorkOrderStatus.Draft,
            Instructions = longInstructions
        };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(workOrder);
            context.SaveChanges();
        }

        WorkOrder rehydratedWorkOrder;
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            rehydratedWorkOrder = context.Set<WorkOrder>()
                .Single(wo => wo.Id == workOrder.Id);
        }

        rehydratedWorkOrder.Instructions!.Length.ShouldBe(4000);
    }
    [Test]
    public void ShouldRejectDuplicateNumber_WhenSavedDirectly()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("dupcreator", "Dup", "Creator", "dup@example.com");
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(new WorkOrder { Number = "DUP0002", Title = "First", Creator = creator });
            context.SaveChanges();
        }

        using var secondContext = TestHost.GetRequiredService<DbContext>();
        secondContext.Attach(creator);
        secondContext.Add(new WorkOrder { Number = "DUP0002", Title = "Second", Creator = creator });

        Should.Throw<DbUpdateException>(() => secondContext.SaveChanges());
    }
    [Test]
    public void ShouldPersistPriorityAndBuilding()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("prio", "Prio", "Creator", "prio@example.com");
        var high = new WorkOrder { Number = "PRI0001", Title = "High", Creator = creator, Priority = WorkOrderPriority.High, Building = "Sanctuary" };
        var defaulted = new WorkOrder { Number = "PRI0002", Title = "Default", Creator = creator };
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.AddRange(high, defaulted);
            context.SaveChanges();
        }

        using var readContext = TestHost.GetRequiredService<DbContext>();
        var rehydratedHigh = readContext.Set<WorkOrder>().Single(wo => wo.Id == high.Id);
        var rehydratedDefault = readContext.Set<WorkOrder>().Single(wo => wo.Id == defaulted.Id);

        rehydratedHigh.Priority.ShouldBe(WorkOrderPriority.High);
        rehydratedHigh.Building.ShouldBe("Sanctuary");
        rehydratedDefault.Priority.ShouldBe(WorkOrderPriority.Medium);
        rehydratedDefault.Building.ShouldBeNull();
    }

    [Test]
    public void ShouldPersistWaitingOnVendorSince()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        var assignee = new Employee("assignee1", "Jane", "Roe", "jane@example.com");
        var since = new DateTime(2026, 10, 14, 9, 30, 0);
        var workOrder = new WorkOrder
        {
            Number = "WV-01",
            Title = "Replace boiler valve",
            Status = WorkOrderStatus.InProgress,
            Creator = creator,
            Assignee = assignee,
            WaitingOnVendorSince = since
        };

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignee);
            context.Add(workOrder);
            context.SaveChanges();
        }

        WorkOrder rehydrated;
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            rehydrated = context.Set<WorkOrder>().Single(wo => wo.Id == workOrder.Id);
        }

        rehydrated.WaitingOnVendorSince.ShouldBe(since);
        rehydrated.IsWaitingOnVendor.ShouldBeTrue();
    }

    [Test]
    public void ShouldReadRowWrittenWithoutWaitingOnVendorColumnAsNotWaiting()
    {
        new DatabaseTests().Clean();

        // A row as app2 writes it: app2 knows nothing of WaitingOnVendorSince, so the column is left out.
        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.SaveChanges();
        }

        var id = Guid.NewGuid();
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var table = context.Database.ProviderName!.Contains("Sqlite", StringComparison.OrdinalIgnoreCase)
                ? "\"WorkOrder\""
                : "[dbo].[WorkOrder]";
#pragma warning disable EF1002, EF1003 // The table name is one of two constants; every value is a parameter
            context.Database.ExecuteSqlRaw(
                "INSERT INTO " + table +
                " (Id, Number, Title, Description, Instructions, Status, CreatorId, Priority)" +
                " VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7})",
                id, "APP2001", "app2 row", "", "", WorkOrderStatus.InProgress.Code, creator.Id, (byte)2);
#pragma warning restore EF1002, EF1003
        }

        WorkOrder rehydrated;
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            rehydrated = context.Set<WorkOrder>().Single(wo => wo.Number == "APP2001");
        }

        rehydrated.Id.ShouldBe(id);
        rehydrated.Status.ShouldBe(WorkOrderStatus.InProgress);
        rehydrated.WaitingOnVendorSince.ShouldBeNull();
        rehydrated.IsWaitingOnVendor.ShouldBeFalse();
    }
}
