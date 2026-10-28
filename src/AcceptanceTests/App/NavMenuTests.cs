using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class NavMenuTests : AcceptanceTestBase
{
    // Opening the page needs no chat client, unlike the chat tests in AiAgentPageTests, which skip
    // without one: this is the test that opens the AI Agent screen in every environment.
    [Test, Retry(2)]
    [Category("Smoke")]
    public async Task AiAgent_NavLink_ShouldOpenAiAgentPage()
    {
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.AiAgent));
        await Page.WaitForURLAsync("**/ai-agent");

        await Expect(Page.GetByTestId(nameof(ApplicationChat.Elements.ChatInput))).ToBeVisibleAsync();
        await Expect(Page.GetByTestId(nameof(ApplicationChat.Elements.SendButton))).ToBeVisibleAsync();
    }
}
