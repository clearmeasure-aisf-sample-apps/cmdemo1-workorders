using ClearMeasure.Bootcamp.LlmGateway;
using Microsoft.Extensions.AI;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.LlmGateway;

[TestFixture]
public class StubChatReformatTests
{
    [Test]
    public void TryReformat_WhenNotReformatSystemPrompt_ReturnsFalse()
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "You are a helpful assistant"),
            new(ChatRole.User, "Title: x\nDescription: y")
        };

        StubChatReformat.TryReformat(messages, out _).ShouldBeFalse();
    }

    [Test]
    public void TryReformat_WhenReformatPrompt_CapitalizesAndFixesGrammar()
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "responsible for reformatting work order fields"),
            new(ChatRole.User, "Title: lowercase title\nDescription: bad grammer here")
        };

        StubChatReformat.TryReformat(messages, out var response).ShouldBeTrue();
        response.Text.ShouldContain("TITLE: Lowercase title");
        response.Text.ShouldContain("grammar");
        response.Text.ShouldEndWith(".");
    }
}
