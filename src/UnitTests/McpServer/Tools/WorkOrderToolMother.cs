using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer.Tools;

/// <summary>
/// Employees and work orders for the MCP tool fixtures.
/// </summary>
public static class WorkOrderToolMother
{
    public static Employee Creator() => new("jsmith", "Jane", "Smith", "jane@example.com");

    public static Employee OtherEmployee() => new("bwayne", "Bruce", "Wayne", "bruce@example.com");

    public static WorkOrder Draft(Employee creator) => new()
    {
        Number = "WO-1",
        Title = "Fix the boiler",
        Description = "The boiler is cold.",
        Instructions = "Call first.",
        RoomNumber = "101",
        DueDate = new DateOnly(2026, 12, 1),
        Building = "Main Hall",
        Priority = WorkOrderPriority.Medium,
        Status = WorkOrderStatus.Draft,
        Creator = creator
    };

    public static WorkOrder Assigned(Employee creator)
    {
        var workOrder = Draft(creator);
        workOrder.Status = WorkOrderStatus.Assigned;
        return workOrder;
    }
}
