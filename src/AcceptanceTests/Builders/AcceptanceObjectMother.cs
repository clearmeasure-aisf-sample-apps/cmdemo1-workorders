using ClearMeasure.Bootcamp.AcceptanceTests.TestSupport;
using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Builders;

/// <summary>
/// Bogus-backed builders that create acceptance data through the app's test-support API, keyed by TestTag.
/// </summary>
public sealed class AcceptanceObjectMother(TestSupportClient client, string testTag)
{
    /// <summary>Creates an admin employee for this test tag.</summary>
    public Task<TestSupportEmployee> EmployeeAsync(string[]? additionalCreateRoleNames = null) =>
        client.CreateEmployeeAsync(testTag, additionalCreateRoleNames);

    /// <summary>Creates a draft work order for <paramref name="creatorUserName"/> titled with the test tag.</summary>
    public Task<TestSupportWorkOrder> DraftWorkOrderAsync(
        string creatorUserName,
        string? title = null,
        string? description = null,
        string? roomNumber = null) =>
        WorkOrderAsync(new TestSupportWorkOrderRequest
        {
            TestTag = testTag,
            CreatorUserName = creatorUserName,
            Title = title,
            Description = description,
            RoomNumber = roomNumber
        });

    /// <summary>Creates a work order with the given status and optional fields.</summary>
    public Task<TestSupportWorkOrder> WorkOrderAsync(
        string creatorUserName,
        WorkOrderStatus status,
        WorkOrderSeedFields? fields = null)
    {
        fields ??= new WorkOrderSeedFields();
        return WorkOrderAsync(new TestSupportWorkOrderRequest
        {
            TestTag = testTag,
            CreatorUserName = creatorUserName,
            Title = fields.Title ?? $"[{testTag}] {fields.Priority} {status.FriendlyName}",
            Description = fields.Description,
            RoomNumber = fields.RoomNumber,
            Instructions = fields.Instructions,
            Number = fields.Number,
            StatusKey = status.Key,
            Priority = fields.Priority.ToString(),
            DueDate = fields.DueDate?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            AssigneeUserName = fields.AssigneeUserName
        });
    }

    /// <summary>Creates a work order from a fully populated request.</summary>
    public Task<TestSupportWorkOrder> WorkOrderAsync(TestSupportWorkOrderRequest request) =>
        client.CreateWorkOrderAsync(request);
}

/// <summary>Optional fields for <see cref="AcceptanceObjectMother.WorkOrderAsync"/>.</summary>
public sealed class WorkOrderSeedFields
{
    /// <summary>Priority; defaults to Medium.</summary>
    public WorkOrderPriority Priority { get; init; } = WorkOrderPriority.Medium;

    /// <summary>Optional due date.</summary>
    public DateOnly? DueDate { get; init; }

    /// <summary>Optional assignee user name.</summary>
    public string? AssigneeUserName { get; init; }

    /// <summary>Optional title.</summary>
    public string? Title { get; init; }

    /// <summary>Optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Optional room number.</summary>
    public string? RoomNumber { get; init; }

    /// <summary>Optional instructions.</summary>
    public string? Instructions { get; init; }

    /// <summary>Optional explicit work-order number.</summary>
    public string? Number { get; init; }
}
