using ClearMeasure.Bootcamp.AcceptanceTests.TestSupport;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Builders;

/// <summary>
/// Bogus-backed builders that create acceptance data through the app's test-support API, keyed by TestTag.
/// </summary>
public sealed class AcceptanceObjectMother(TestSupportClient client, string testTag)
{
    /// <summary>Creates an admin employee for this test tag.</summary>
    public Task<TestSupportEmployee> EmployeeAsync() => client.CreateEmployeeAsync(testTag);

    /// <summary>Creates a draft work order for <paramref name="creatorUserName"/> titled with the test tag.</summary>
    public Task<TestSupportWorkOrder> DraftWorkOrderAsync(
        string creatorUserName,
        string? title = null,
        string? description = null,
        string? roomNumber = null) =>
        client.CreateWorkOrderAsync(new TestSupportWorkOrderRequest
        {
            TestTag = testTag,
            CreatorUserName = creatorUserName,
            Title = title,
            Description = description,
            RoomNumber = roomNumber
        });
}
