using ClearMeasure.Bootcamp.LlmGateway;
using Microsoft.Extensions.AI;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.LlmGateway;

[TestFixture]
public class StubChatFunctionResultsTests
{
    [Test]
    public void TryEcho_WhenNoFunctionResults_ReturnsFalse()
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "hello")
        };

        StubChatFunctionResults.TryEcho(messages, out _).ShouldBeFalse();
    }

    [Test]
    public void TryEcho_WhenFunctionResultPresent_JoinsResultText()
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.Tool, [new FunctionResultContent("c1", "line-one")]),
            new(ChatRole.Tool, [new FunctionResultContent("c2", "line-two")])
        };

        StubChatFunctionResults.TryEcho(messages, out var response).ShouldBeTrue();
        response.Text.ShouldBe("line-one\nline-two");
    }
}
