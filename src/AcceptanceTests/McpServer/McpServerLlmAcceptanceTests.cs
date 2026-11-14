namespace ClearMeasure.Bootcamp.AcceptanceTests.McpServer;

[TestFixture]
public class McpServerLlmAcceptanceTests : McpAcceptanceTestBase
{
    protected override bool RequiresBrowser => false;

    [SetUp]
    public async Task EnsureChatClientAsync()
    {
        await SkipIfNoChatClient();
    }

    [Test, LlmTest]
    public async Task ShouldListWorkOrdersViaLlm()
    {
        var creator = await SeedEmployeeAsync();
        var seeded = await SeedWorkOrderAsync(creator);
        seeded.Number.ShouldNotBeNullOrWhiteSpace();

        var response = await Helper.SendPrompt(
            $"Use the list-work-orders tool with creatorUsername='{creator.UserName}' to list work orders. " +
            $"Return the work order numbers you find, including '{seeded.Number}'.");

        response.Text.ShouldNotBeNullOrEmpty();
        response.Text.ShouldContain(seeded.Number!);
    }

    [Test, LlmTest]
    public async Task ShouldGetWorkOrderByNumberViaLlm()
    {
        var creator = await SeedEmployeeAsync();
        var knownOrder = await SeedWorkOrderAsync(creator);

        var response = await Helper.SendPrompt(
            $"Use the get-work-order tool to get the details of work order number '{knownOrder.Number}'. " +
            "Return the title and status.");

        response.Text.ShouldNotBeNullOrEmpty();
        response.Text.ShouldContain(knownOrder.Title!);
    }

    [Test, LlmTest]
    public async Task ShouldCreateWorkOrderViaLlm()
    {
        var creator = await SeedEmployeeAsync();
        var title = $"[{TestTag}] Repair sanctuary roof";

        var response = await Helper.SendPrompt(
            $"Call the create-work-order tool with these exact parameters: " +
            $"title='{title}', description='Roof tiles need replacement', " +
            $"creatorUsername='{creator.UserName}'.");

        response.Text.ShouldNotBeNullOrEmpty();
        var responseText = response.Text.ToLowerInvariant();
        (responseText.Contains("repair") || responseText.Contains("draft") || responseText.Contains("created") ||
         responseText.Contains(TestTag.ToLowerInvariant()) || responseText.Contains("wo-"))
            .ShouldBeTrue($"Expected creation confirmation in response: {response.Text}");
    }

    [Test, LlmTest]
    public async Task ShouldListEmployeesViaLlm()
    {
        var known = await SeedEmployeeAsync();

        var response = await Helper.SendPrompt(
            "Use the list-employees tool to list employees in the system. " +
            $"Return their usernames, and include '{known.UserName}' if present.");

        response.Text.ShouldNotBeNullOrEmpty();
        response.Text.ShouldContain(known.UserName);
    }
}
