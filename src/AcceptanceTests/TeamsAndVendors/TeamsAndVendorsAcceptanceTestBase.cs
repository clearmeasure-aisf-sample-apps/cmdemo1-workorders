using ClearMeasure.Bootcamp.AcceptanceTests.Builders;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.TeamsAndVendors;

/// <summary>
/// Teams &amp; Vendors acceptance base: API-backed seeding keyed by <see cref="AcceptanceTestBase.TestTag"/>.
/// </summary>
public abstract class TeamsAndVendorsAcceptanceTestBase : AcceptanceApiTestBase
{
    /// <summary>Opens the Teams &amp; Vendors admin page and waits for network idle.</summary>
    protected async Task GotoTeamsPageAsync()
    {
        await Page.GotoAsync("/admin/teams");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>Seeds two Assigned open work orders for <see cref="AcceptanceTestBase.CurrentUser"/>.</summary>
    protected async Task SeedTwoAssignedOpenOrdersAsync()
    {
        await SeedWorkOrderAsync(CurrentUser, WorkOrderStatus.Assigned, new WorkOrderSeedFields
        {
            Title = $"[{TestTag}] teams open a",
            AssigneeUserName = CurrentUser.UserName
        });
        await SeedWorkOrderAsync(CurrentUser, WorkOrderStatus.Assigned, new WorkOrderSeedFields
        {
            Title = $"[{TestTag}] teams open b",
            AssigneeUserName = CurrentUser.UserName
        });
        await SeedWorkOrderAsync(CurrentUser, WorkOrderStatus.Complete, new WorkOrderSeedFields
        {
            Title = $"[{TestTag}] teams complete",
            AssigneeUserName = CurrentUser.UserName,
            CompletedDate = DateTime.UtcNow.AddDays(-1)
        });
    }

    /// <summary>Creates a team whose name and description include <see cref="AcceptanceTestBase.TestTag"/>.</summary>
    protected async Task<CreateTeamResult> CreateTaggedTeamAsync(
        string nameSuffix,
        string? description,
        IReadOnlyList<string> memberUserNames)
    {
        var result = await Bus.Send(new CreateTeamCommand(
            $"{nameSuffix} {TestTag}",
            description is null ? null : $"{description} {TestTag}",
            memberUserNames));
        result.Succeeded.ShouldBeTrue();
        return result;
    }

    /// <summary>Creates a vendor whose name includes <see cref="AcceptanceTestBase.TestTag"/>.</summary>
    protected async Task<CreateVendorResult> CreateTaggedVendorAsync(TaggedVendorSpec spec)
    {
        var result = await Bus.Send(new CreateVendorCommand(
            $"{spec.NameSuffix} {TestTag}",
            spec.Trade,
            spec.ContactName,
            spec.EmailAddress,
            spec.PhoneNumber,
            spec.IsActive));
        result.Succeeded.ShouldBeTrue();
        return result;
    }

    /// <summary>Locator for a team card by create result id.</summary>
    protected ILocator TeamCard(Guid teamId) =>
        Page.Locator($"[data-testid*='TeamCard'][data-testid*='{teamId}']");

    /// <summary>Locator for a vendor card by create result id.</summary>
    protected ILocator VendorCard(Guid vendorId) =>
        Page.Locator($"[data-testid*='VendorCard'][data-testid*='{vendorId}']");

    /// <summary>Assigned-open count cell for <paramref name="userName"/>.</summary>
    protected ILocator AssignedOpenCell(string userName) =>
        Page.GetByTestId($"{UI.Shared.Pages.TeamsAndVendors.Elements.TableRowAssignedOpen}{userName}");
}
