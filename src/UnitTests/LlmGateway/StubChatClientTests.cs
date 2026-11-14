using System.ClientModel;
using ClearMeasure.Bootcamp.LlmGateway;
using Microsoft.Extensions.AI;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.LlmGateway;

[TestFixture]
public class StubChatClientTests
{
    [Test]
    public async Task GetResponseAsync_WhenHealthProbe_ReturnsOk()
    {
        var client = new StubChatClient();

        var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "Reply with OK")]);

        response.Text.ShouldBe("OK");
    }

    [Test]
    public async Task GetResponseAsync_WhenRefusalMarker_ThrowsClientResultException()
    {
        var client = new StubChatClient();
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, $"Test prompt that {StubChatClient.RefusalPromptMarker}")
        };

        var act = () => client.GetResponseAsync(messages);

        await act.ShouldThrowAsync<ClientResultException>();
    }

    [Test]
    public async Task GetResponseAsync_WhenSaturdaySchedule_ReturnsCreateDatedWorkOrdersCall()
    {
        var client = new StubChatClient();
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "Currently logged in user is tlovejoy"),
            new(ChatRole.User,
                "Create 10 work orders for test_abc_user to mow the grass, " +
                "one per week for the next 10 Saturdays. Put [tag1] in every title.")
        };

        var response = await client.GetResponseAsync(messages);
        var call = response.Messages.Single().Contents.OfType<FunctionCallContent>().Single();

        call.Name.ShouldBe("create-dated-work-orders");
        call.Arguments!["creatorUsername"].ShouldBe("tlovejoy");
        call.Arguments["assigneeUsername"].ShouldBe("test_abc_user");
        call.Arguments["title"].ShouldBe("Mow the grass [tag1]");
        call.Arguments["saturdayCount"].ShouldBe(10);
    }

    [Test]
    public async Task GetResponseAsync_WhenFunctionResultPresent_EchoesResultText()
    {
        var client = new StubChatClient();
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "schedule Saturdays"),
            new(ChatRole.Assistant, [new FunctionCallContent("c1", "create-dated-work-orders")]),
            new(ChatRole.Tool, [new FunctionResultContent("c1", "WO-1 due 2026-11-15")])
        };

        var response = await client.GetResponseAsync(messages);

        response.Text.ShouldContain("WO-1 due 2026-11-15");
    }

    [Test]
    public async Task GetResponseAsync_WhenReformatPrompt_ReturnsTitleAndDescriptionLines()
    {
        var client = new StubChatClient();
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "You are an AI agent responsible for reformatting work order fields."),
            new(ChatRole.User, "Title: [t1] lowercase needs fixing\nDescription: this is bad grammer")
        };

        var response = await client.GetResponseAsync(messages);

        response.Text.ShouldContain("TITLE:");
        response.Text.ShouldContain("DESCRIPTION:");
        response.Text.ShouldContain("grammar");
        response.Text.ShouldStartWith("TITLE: [t1]");
    }
}
