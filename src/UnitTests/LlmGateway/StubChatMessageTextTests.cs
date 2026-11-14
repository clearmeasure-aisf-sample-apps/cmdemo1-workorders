using ClearMeasure.Bootcamp.LlmGateway;
using Microsoft.Extensions.AI;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.LlmGateway;

[TestFixture]
public class StubChatMessageTextTests
{
    [Test]
    public void LastUserText_WhenNoUserMessage_ReturnsEmpty()
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "system only")
        };

        StubChatMessageText.LastUserText(messages).ShouldBeEmpty();
    }

    [Test]
    public void LastUserText_WhenMultipleUsers_ReturnsLast()
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "first"),
            new(ChatRole.Assistant, "reply"),
            new(ChatRole.User, "second")
        };

        StubChatMessageText.LastUserText(messages).ShouldBe("second");
    }

    [Test]
    public void CombinedSystemText_JoinsSystemMessages()
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "a"),
            new(ChatRole.User, "u"),
            new(ChatRole.System, "b")
        };

        StubChatMessageText.CombinedSystemText(messages).ShouldBe("a\nb");
    }
}
