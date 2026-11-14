using System.Net;
using ClearMeasure.Bootcamp.AcceptanceTests.TestSupport;
using ClearMeasure.Bootcamp.AcceptanceTests.TestUsers;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Builders;

/// <summary>
/// Creates acceptance work orders via test-support when available, otherwise SQL
/// (<see cref="AcceptanceTestWorkOrders.Add"/>) for tdd/uat/prod where the API returns 404.
/// </summary>
public static class AcceptanceWorkOrderSeed
{
    /// <summary>
    /// Probes test-support, then creates a work order through the API or SQL fallback.
    /// </summary>
    public static async Task<TestSupportWorkOrder> CreateAsync(
        string testTag,
        string creatorUserName,
        WorkOrderStatus status,
        WorkOrderSeedFields? fields = null)
    {
        fields ??= new WorkOrderSeedFields();
        var resolved = ResolveFields(testTag, status, fields);
        var client = AcceptanceTestSupportSettings.CreateClient();
        if (await client.ProbeStatusAsync() == HttpStatusCode.NotFound)
        {
            return AcceptanceTestWorkOrders.Add(testTag, creatorUserName, status, resolved);
        }

        return await client.CreateWorkOrderAsync(testTag, creatorUserName, status, resolved);
    }

    private static WorkOrderSeedFields ResolveFields(
        string testTag,
        WorkOrderStatus status,
        WorkOrderSeedFields fields) =>
        new()
        {
            Title = fields.Title ?? $"[{testTag}] {fields.Priority} {status.FriendlyName}",
            Description = fields.Description,
            RoomNumber = fields.RoomNumber,
            Instructions = fields.Instructions,
            Number = fields.Number,
            Priority = fields.Priority,
            DueDate = fields.DueDate,
            AssigneeUserName = fields.AssigneeUserName,
            Building = fields.Building,
            CreatedDate = fields.CreatedDate,
            CompletedDate = fields.CompletedDate
        };
}
