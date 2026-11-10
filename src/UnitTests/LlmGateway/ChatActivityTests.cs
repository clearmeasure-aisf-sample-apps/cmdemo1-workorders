using System.Diagnostics;
using ClearMeasure.Bootcamp.LlmGateway;
using Microsoft.Extensions.AI;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.LlmGateway;

[TestFixture]
[NonParallelizable]
public class ChatActivityTests
{
    private readonly List<Activity> _recorded = [];
    private ActivityListener _listener = null!;

    [SetUp]
    public void ListenToTheLlmGatewaySource()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ChatActivity.SourceName,
            Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                lock (_recorded)
                {
                    _recorded.Add(activity);
                }
            }
        };
        ActivitySource.AddActivityListener(_listener);
    }

    [TearDown]
    public void StopListening() => _listener.Dispose();

    [Test]
    public void ShouldTagTheProviderAndTheLastUserPrompt_WhenTheCallStarts()
    {
        ChatMessage[] messages = [new(ChatRole.System, "rules"), new(ChatRole.User, "first"), new(ChatRole.User, "latest")];

        ChatActivity.Start("ChatActivityTests.Start", messages).Dispose();

        var span = Recorded("ChatActivityTests.Start");
        span.GetTagItem("chat.provider").ShouldBe("OpenAI");
        span.GetTagItem("chat.prompt").ShouldBe("latest");
        span.Events.Select(e => e.Name).ShouldBe(["request.sent"]);
    }

    [Test]
    public void ShouldTagTheModelAndResponse_WhenTheCallCompletes()
    {
        using (var activity = ChatActivity.Start("ChatActivityTests.Completed", [new ChatMessage(ChatRole.User, "hi")]))
        {
            activity.Completed("gpt-test", "hello");
        }

        var span = Recorded("ChatActivityTests.Completed");
        span.GetTagItem("chat.model").ShouldBe("gpt-test");
        span.GetTagItem("chat.response").ShouldBe("hello");
        span.Events.Select(e => e.Name).ShouldBe(["request.sent", "response.received"]);
    }

    [Test]
    public void ShouldMarkTheSpanAsAnError_WhenTheCallFails()
    {
        using (var activity = ChatActivity.Start("ChatActivityTests.Failed", [new ChatMessage(ChatRole.User, "hi")]))
        {
            activity.Failed(new HttpRequestException("refused"));
        }

        var span = Recorded("ChatActivityTests.Failed");
        span.Status.ShouldBe(ActivityStatusCode.Error);
        span.StatusDescription.ShouldBe("refused");
        span.Events.Last().Tags.ShouldContain(tag => tag.Key == "exception.type" && (string?)tag.Value == typeof(HttpRequestException).FullName);
    }

    private Activity Recorded(string operationName)
    {
        lock (_recorded)
        {
            return _recorded.Single(activity => activity.OperationName == operationName);
        }
    }
}
