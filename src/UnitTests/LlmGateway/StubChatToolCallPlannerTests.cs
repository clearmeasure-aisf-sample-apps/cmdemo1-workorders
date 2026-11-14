using ClearMeasure.Bootcamp.LlmGateway;
using Microsoft.Extensions.AI;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.LlmGateway;

[TestFixture]
public class StubChatToolCallPlannerTests
{
    [Test]
    public void TryPlan_WhenNotSaturdayPrompt_ReturnsFalse()
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "List open work orders")
        };

        var planned = StubChatToolCallPlanner.TryPlan(messages, out _);

        planned.ShouldBeFalse();
    }

    [Test]
    public void TryPlan_WhenSaturdayPrompt_BuildsDatedWorkOrdersCall()
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "Currently logged in user is alice"),
            new(ChatRole.User, "for bob to mow on Saturdays [batch9]")
        };

        var planned = StubChatToolCallPlanner.TryPlan(messages, out var response);

        planned.ShouldBeTrue();
        var call = response.Messages.Single().Contents.OfType<FunctionCallContent>().Single();
        call.Arguments!["creatorUsername"].ShouldBe("alice");
        call.Arguments["assigneeUsername"].ShouldBe("bob");
        call.Arguments["title"].ShouldBe("Mow the grass [batch9]");
    }
}
