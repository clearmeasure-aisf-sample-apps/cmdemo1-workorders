namespace ClearMeasure.Bootcamp.AcceptanceTests.TestUsers;

/// <summary>
/// Employees saved to the test database the way an earlier acceptance run leaves them: with a role, and either
/// referenced by nothing or referenced by one kind of row that must keep them. <c>creator</c> is an employee that is
/// already saved and creates the work order the row belongs to.
/// </summary>
public static class LeftoverEmployeeMother
{
    public static Employee Unreferenced(string userName)
    {
        var employee = NewEmployee(userName);
        Save(employee, null);
        return employee;
    }

    public static Employee WithSavedSearch(string userName)
    {
        var employee = NewEmployee(userName);
        Save(employee, null, new SavedSearchView
        {
            EmployeeId = employee.Id,
            Name = "Mine",
            Query = "status=Draft",
            CreatedDate = DateTime.UtcNow
        });
        return employee;
    }

    public static Employee WorkOrderCreator(string userName)
    {
        var employee = NewEmployee(userName);
        Save(employee, null, NewWorkOrder(employee, null));
        return employee;
    }

    public static Employee WorkOrderAssignee(string userName, Employee creator)
    {
        var employee = NewEmployee(userName);
        Save(employee, creator, NewWorkOrder(creator, employee));
        return employee;
    }

    public static Employee StatusChanger(string userName, Employee creator)
    {
        var employee = NewEmployee(userName);
        var order = NewWorkOrder(creator, null);
        Save(employee, creator, order, new WorkOrderStatusChange
        {
            Id = Guid.NewGuid(),
            WorkOrderId = order.Id,
            FromStatus = WorkOrderStatus.Draft,
            ToStatus = WorkOrderStatus.Assigned,
            ChangedById = employee.Id,
            ChangedDate = DateTime.UtcNow
        });
        return employee;
    }

    public static Employee AttachmentUploader(string userName, Employee creator)
    {
        var employee = NewEmployee(userName);
        var order = NewWorkOrder(creator, null);
        Save(employee, creator, order, new WorkOrderAttachment
        {
            WorkOrderId = order.Id,
            FileName = "leftover.txt",
            ContentType = "text/plain",
            FileSize = 1,
            UploadedById = employee.Id,
            UploadedDate = DateTime.UtcNow
        });
        return employee;
    }

    public static Employee TeamMember(string userName)
    {
        var employee = NewEmployee(userName);
        var team = new Team($"Team {userName}", null);
        team.AddMember(employee);
        Save(employee, null, team);
        return employee;
    }

    private static Employee NewEmployee(string userName)
    {
        var employee = new Employee(userName, "Left", "Over", $"{userName}@example.com") { Id = Guid.NewGuid() };
        employee.AddRole(new Role("admin", true, true));
        return employee;
    }

    private static WorkOrder NewWorkOrder(Employee creator, Employee? assignee)
    {
        var order = TestHost.Faker<WorkOrder>();
        order.Id = Guid.NewGuid();
        order.Title = $"Leftover work order of {creator.UserName}";
        order.Creator = creator;
        order.Assignee = assignee;
        order.Status = assignee is null ? WorkOrderStatus.Draft : WorkOrderStatus.Assigned;
        return order;
    }

    private static void Save(Employee employee, Employee? savedCreator, params object[] rows)
    {
        using var context = TestHost.NewDbContext();
        if (savedCreator is not null)
        {
            context.Attach(savedCreator);
        }

        context.Add(employee);
        context.AddRange(rows);
        context.SaveChanges();
    }
}
