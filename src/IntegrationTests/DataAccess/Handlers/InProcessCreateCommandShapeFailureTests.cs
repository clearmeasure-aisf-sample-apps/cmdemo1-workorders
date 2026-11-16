using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.IntegrationTests.TestSupport;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

/// <summary>
/// #1824 option (b): in-process creates through <see cref="IBus"/> return Failed for shape
/// violations (same FluentValidation messages as HTTP) and write nothing.
/// </summary>
[TestFixture]
public class InProcessCreateCommandShapeFailureTests
{
    private string _tag = "";

    [SetUp]
    public void SetUp() => _tag = TestData.Tag();

    [Test]
    public async Task ShouldFailWithoutWriting_WhenVendorNameExceedsMaxLength()
    {
        var name = new string('a', Vendor.NameMaxLength + 1) + " " + _tag;
        var result = await TestHost.GetRequiredService<IBus>()
            .Send(new CreateVendorCommand(name, null, null, null, null));

        result.Succeeded.ShouldBeFalse();
        result.Id.ShouldBeNull();
        result.Message.ShouldContain("100 characters or fewer");
        await AssertNoVendorNamedAsync(name);
    }

    [Test]
    public async Task ShouldFailWithoutWriting_WhenVendorEmailIsInvalid()
    {
        var name = VendorName("Acme");
        var result = await TestHost.GetRequiredService<IBus>()
            .Send(new CreateVendorCommand(name, null, null, "not-an-email", null));

        result.Succeeded.ShouldBeFalse();
        result.Id.ShouldBeNull();
        result.Message.ShouldContain("Email address is not valid.");
        await AssertNoVendorNamedAsync(name);
    }

    [Test]
    public async Task ShouldFailWithoutWriting_WhenVendorPhoneIsInvalid()
    {
        var name = VendorName("Acme");
        var result = await TestHost.GetRequiredService<IBus>()
            .Send(new CreateVendorCommand(name, null, null, null, "123"));

        result.Succeeded.ShouldBeFalse();
        result.Id.ShouldBeNull();
        result.Message.ShouldContain("Phone number is not valid.");
        await AssertNoVendorNamedAsync(name);
    }

    [Test]
    public async Task ShouldFailWithoutWriting_WhenTeamDescriptionExceedsMaxLength()
    {
        var name = TeamName("Grounds");
        var result = await TestHost.GetRequiredService<IBus>()
            .Send(new CreateTeamCommand(name, new string('d', Team.DescriptionMaxLength + 1), []));

        result.Succeeded.ShouldBeFalse();
        result.Id.ShouldBeNull();
        result.Message.ShouldContain("500 characters or fewer");
        await AssertNoTeamNamedAsync(name);
    }

    [Test]
    public async Task ShouldFailWithoutWriting_WhenDatedTitleIsBlank()
    {
        var creator = TestData.UserName("tl");
        var assignee = TestData.UserName("gw");
        await PersistEmployeesAsync(creator, assignee);

        var result = await TestHost.GetRequiredService<IBus>().Send(new CreateDatedWorkOrdersCommand(
            creator, assignee, "", "Weekly Saturday mow", [new DateOnly(2026, 8, 29)]));

        result.Success.ShouldBeFalse();
        result.Message.ShouldContain("'Title' must not be empty.");
        result.WorkOrders.Count.ShouldBe(0);
        await AssertNoWorkOrdersForAsync(creator, assignee);
    }

    private string VendorName(string name) => $"{name} {_tag}";

    private string TeamName(string name) => $"{name} {_tag}";

    private static async Task PersistEmployeesAsync(string creatorUserName, string assigneeUserName)
    {
        await using var context = TestHost.GetRequiredService<DbContext>();
        context.AddRange(
            EmployeeBuilder.Create(creatorUserName).WithFirstName("Dated").WithLastName("Creator")
                .WithEmail("t@test.com").Build(),
            EmployeeBuilder.Create(assigneeUserName).WithFirstName("Dated").WithLastName("Assignee")
                .WithEmail("w@test.com").Build());
        await context.SaveChangesAsync();
    }

    private static async Task AssertNoVendorNamedAsync(string name)
    {
        await using var context = TestHost.GetRequiredService<DataContext>();
        (await context.Set<Vendor>().CountAsync(v => v.Name == name)).ShouldBe(0);
    }

    private static async Task AssertNoTeamNamedAsync(string name)
    {
        await using var context = TestHost.GetRequiredService<DataContext>();
        (await context.Set<Team>().CountAsync(t => t.Name == name)).ShouldBe(0);
    }

    private static async Task AssertNoWorkOrdersForAsync(string creator, string assignee)
    {
        await using var context = TestHost.GetRequiredService<DbContext>();
        (await context.Set<WorkOrder>().CountAsync(w =>
            w.Creator!.UserName == creator || w.Assignee!.UserName == assignee)).ShouldBe(0);
    }
}
