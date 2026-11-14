using ClearMeasure.Bootcamp.AcceptanceTests.Builders;
using ClearMeasure.Bootcamp.AcceptanceTests.TestSupport;
using ClearMeasure.Bootcamp.Core.Services.Impl;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.AcceptanceTests.TestUsers;

/// <summary>
/// Work orders an acceptance run can add when test-support is unavailable (tdd/uat/prod).
/// Pool slots create work orders through <see cref="TestSupport.TestSupportClient"/> instead.
/// </summary>
public static class AcceptanceTestWorkOrders
{
    /// <summary>
    /// Inserts a work order for <paramref name="creatorUserName"/> with fields matching the
    /// test-support create shape. Used when <c>POST /api/test-support/work-orders</c> returns 404.
    /// </summary>
    public static TestSupportWorkOrder Add(
        string testTag,
        string creatorUserName,
        WorkOrderStatus status,
        WorkOrderSeedFields fields)
    {
        using var context = TestHost.NewDbContext();
        var creator = context.Set<Employee>()
            .Single(employee => employee.UserName == creatorUserName);
        var parts = new SqlWorkOrderParts
        {
            TestTag = testTag,
            Creator = creator,
            Assignee = ResolveAssignee(context, fields.AssigneeUserName),
            Status = status,
            Fields = fields
        };
        var workOrder = BuildWorkOrder(parts);
        context.Add(workOrder);
        context.SaveChanges();
        return ToDto(workOrder, creator.UserName);
    }

    private static Employee? ResolveAssignee(DbContext context, string? userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return null;
        }

        return context.Set<Employee>().Single(employee => employee.UserName == userName);
    }

    private static WorkOrder BuildWorkOrder(SqlWorkOrderParts parts)
    {
        var now = DateTime.UtcNow;
        var fields = parts.Fields;
        var workOrder = new WorkOrder
        {
            Number = ResolveNumber(fields.Number),
            Creator = parts.Creator,
            Assignee = parts.Assignee,
            Status = parts.Status,
            Priority = fields.Priority,
            DueDate = fields.DueDate,
            Title = fields.Title ?? $"[{parts.TestTag}] {fields.Priority} {parts.Status.FriendlyName}",
            Description = fields.Description ?? $"[{parts.TestTag}] description",
            Instructions = fields.Instructions ?? string.Empty,
            RoomNumber = fields.RoomNumber ?? "1",
            Building = fields.Building,
            CreatedDate = fields.CreatedDate ?? now
        };
        ApplyStatusDates(workOrder, parts.Status, now, fields.CompletedDate);
        return workOrder;
    }

    private sealed class SqlWorkOrderParts
    {
        public required string TestTag { get; init; }
        public required Employee Creator { get; init; }
        public Employee? Assignee { get; init; }
        public required WorkOrderStatus Status { get; init; }
        public required WorkOrderSeedFields Fields { get; init; }
    }

    private static string ResolveNumber(string? number) =>
        string.IsNullOrWhiteSpace(number)
            ? new WorkOrderNumberGenerator().GenerateNumber()
            : number.Trim();

    private static void ApplyStatusDates(
        WorkOrder workOrder,
        WorkOrderStatus status,
        DateTime now,
        DateTime? completedDate)
    {
        if (status == WorkOrderStatus.Draft)
        {
            if (completedDate.HasValue)
            {
                workOrder.CompletedDate = completedDate;
            }

            return;
        }

        workOrder.AssignedDate ??= now.AddDays(-1);
        if (status == WorkOrderStatus.Complete)
        {
            workOrder.CompletedDate = completedDate ?? now;
        }
    }

    private static TestSupportWorkOrder ToDto(WorkOrder workOrder, string creatorUserName) => new()
    {
        Id = workOrder.Id,
        Number = workOrder.Number!,
        Title = workOrder.Title!,
        Description = workOrder.Description,
        RoomNumber = workOrder.RoomNumber,
        CreatorUserName = creatorUserName
    };
}
