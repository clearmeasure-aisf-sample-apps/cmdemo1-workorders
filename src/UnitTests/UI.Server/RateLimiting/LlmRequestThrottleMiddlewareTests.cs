using System.Text;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Messaging;
using ClearMeasure.Bootcamp.Core.Model.Events;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UI.Server.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.RateLimiting;

[TestFixture]
public class LlmRequestThrottleMiddlewareTests
{
    private const string SingleApiPath = "/api/v1.0/blazor-wasm-single-api";

    [Test]
    public async Task InvokeAsync_WhenChatPromptIsOversize_ShouldReturn400AndNeverCallTheLlmFactory()
    {
        var factory = new CountingChatClientFactory();
        using var guard = CreateGuard(new LlmRequestThrottleOptions { MaxPromptChars = 3 });
        var context = CreateContext(MessageJson(new ApplicationChatQuery("far too long", "tlovejoy")));

        await CreateMiddleware(factory).InvokeAsync(context, guard);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        ResponseText(context).ShouldContain("Prompt exceeds 3 characters.");
        factory.Calls.ShouldBe(0);
    }

    [Test]
    public async Task InvokeAsync_WhenChatHistoryIsOversize_ShouldReturn400AndNeverCallTheLlmFactory()
    {
        var factory = new CountingChatClientFactory();
        using var guard = CreateGuard(new LlmRequestThrottleOptions { MaxChatHistoryEntries = 1 });
        var query = new ApplicationChatQuery("hi", "tlovejoy")
        {
            ChatHistory = [new ChatHistoryMessage("user", "a"), new ChatHistoryMessage("assistant", "b")]
        };
        var context = CreateContext(MessageJson(query));

        await CreateMiddleware(factory).InvokeAsync(context, guard);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        factory.Calls.ShouldBe(0);
    }

    [Test]
    public async Task InvokeAsync_WhenClientIsOverTheChatRate_ShouldReturn429AndNeverCallTheLlmFactoryAgain()
    {
        var factory = new CountingChatClientFactory();
        using var guard = CreateGuard(new LlmRequestThrottleOptions { PermitLimit = 1, WindowSeconds = 30 });
        var middleware = CreateMiddleware(factory);
        var json = MessageJson(new ApplicationChatQuery("hi", "tlovejoy"));
        await middleware.InvokeAsync(CreateContext(json), guard);
        var refused = CreateContext(json);

        await middleware.InvokeAsync(refused, guard);

        refused.Response.StatusCode.ShouldBe(StatusCodes.Status429TooManyRequests);
        refused.Response.Headers.RetryAfter.ToString().ShouldBe("30");
        factory.Calls.ShouldBe(1);
    }

    [Test]
    public async Task InvokeAsync_WhenChatRequestFits_ShouldCallTheHandlerAndLlmFactory()
    {
        var factory = new CountingChatClientFactory();
        using var guard = CreateGuard(new LlmRequestThrottleOptions());
        var context = CreateContext(MessageJson(new ApplicationChatQuery("hi", "tlovejoy")));

        await CreateMiddleware(factory).InvokeAsync(context, guard);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
        factory.Calls.ShouldBe(1);
    }

    [Test]
    public async Task InvokeAsync_WhenPayloadIsNotAChatQuery_ShouldCallNextWithoutSpendingAChatPermit()
    {
        var next = new NextRecorder();
        using var guard = CreateGuard(new LlmRequestThrottleOptions { PermitLimit = 1 });
        var middleware = new LlmRequestThrottleMiddleware(next.Invoke, ApiOptions());
        var json = MessageJson(new UserLoggedInEvent("tlovejoy"));

        await middleware.InvokeAsync(CreateContext(json), guard);
        await middleware.InvokeAsync(CreateContext(json), guard);

        next.Calls.ShouldBe(2);
    }

    [Test]
    public async Task ReadChatQueryAsync_WhenRequestIsNotASingleApiPost_ShouldReturnNull()
    {
        var context = CreateContext(MessageJson(new ApplicationChatQuery("hi", "tlovejoy")));
        context.Request.Method = HttpMethods.Get;

        (await LlmRequestThrottleMiddleware.ReadChatQueryAsync(context.Request)).ShouldBeNull();
    }

    [Test]
    public async Task ReadChatQueryAsync_WhenBodyIsNotJson_ShouldReturnNullAndRewindTheBody()
    {
        var context = CreateContext("{bad");

        (await LlmRequestThrottleMiddleware.ReadChatQueryAsync(context.Request)).ShouldBeNull();
        context.Request.Body.Position.ShouldBe(0);
    }

    private static LlmRequestThrottleMiddleware CreateMiddleware(CountingChatClientFactory factory)
    {
        var handler = new ApplicationChatHandler(factory, new EmptyToolProvider(), NullLogger<ApplicationChatHandler>.Instance);
        return new LlmRequestThrottleMiddleware(
            async context =>
            {
                var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
                var message = JsonSerializer.Deserialize<WebServiceMessage>(body)!;
                await handler.Handle((ApplicationChatQuery)message.GetBodyObject(), CancellationToken.None);
            },
            ApiOptions());
    }

    private static StubOptionsMonitor<ApiRateLimitingOptions> ApiOptions() => new(new ApiRateLimitingOptions());

    private static LlmRequestThrottleGuard CreateGuard(LlmRequestThrottleOptions options) => new(Options.Create(options));

    private static string MessageJson(object payload) => JsonSerializer.Serialize(new WebServiceMessage(payload));

    private static string ResponseText(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return new StreamReader(context.Response.Body).ReadToEnd();
    }

    private static DefaultHttpContext CreateContext(string body) =>
        new()
        {
            Request =
            {
                Method = HttpMethods.Post,
                Path = SingleApiPath,
                ContentType = "application/json",
                Body = new MemoryStream(Encoding.UTF8.GetBytes(body))
            },
            Response = { Body = new MemoryStream() }
        };

    private sealed class EmptyToolProvider : IToolProvider
    {
        public Task<IList<AITool>> GetToolsAsync() => Task.FromResult<IList<AITool>>([]);
    }

    private sealed class CountingChatClientFactory() : ChatClientFactory(null!)
    {
        public int Calls { get; private set; }

        public override Task<IChatClient> GetChatClient()
        {
            Calls++;
            return Task.FromResult<IChatClient>(new StubChatClient());
        }
    }
}
