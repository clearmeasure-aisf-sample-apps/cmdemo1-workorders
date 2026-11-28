using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UI.Server.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.RateLimiting;

[TestFixture]
public class LlmRequestThrottleGuardTests
{
    private const string Client = "user:tlovejoy";

    [Test]
    public void Evaluate_WhenPromptIsAtTheLimit_ShouldAllow()
    {
        using var guard = CreateGuard(new LlmRequestThrottleOptions { MaxPromptChars = 5 });

        guard.Evaluate(new ApplicationChatQuery("12345", "tlovejoy"), Client).IsAllowed.ShouldBeTrue();
    }

    [Test]
    public void Evaluate_WhenPromptIsOverTheLimit_ShouldReturn400()
    {
        using var guard = CreateGuard(new LlmRequestThrottleOptions { MaxPromptChars = 5 });

        var verdict = guard.Evaluate(new ApplicationChatQuery("123456", "tlovejoy"), Client);

        verdict.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        verdict.Detail.ShouldBe("Prompt exceeds 5 characters.");
    }

    [Test]
    public void Evaluate_WhenChatHistoryHasTooManyEntries_ShouldReturn400()
    {
        using var guard = CreateGuard(new LlmRequestThrottleOptions { MaxChatHistoryEntries = 2 });

        var verdict = guard.Evaluate(ChatWithHistory("a", "b", "c"), Client);

        verdict.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        verdict.Detail.ShouldBe("Chat history exceeds 2 entries.");
    }

    [Test]
    public void Evaluate_WhenChatHistoryHasTooManyCharacters_ShouldReturn400()
    {
        using var guard = CreateGuard(new LlmRequestThrottleOptions { MaxChatHistoryChars = 5 });

        var verdict = guard.Evaluate(ChatWithHistory("abc", "def"), Client);

        verdict.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        verdict.Detail.ShouldBe("Chat history exceeds 5 characters.");
    }

    [Test]
    public void Evaluate_WhenChatHistoryFitsBothLimits_ShouldAllow()
    {
        using var guard = CreateGuard(new LlmRequestThrottleOptions { MaxChatHistoryEntries = 2, MaxChatHistoryChars = 6 });

        guard.Evaluate(ChatWithHistory("abc", "def"), Client).IsAllowed.ShouldBeTrue();
    }

    [Test]
    public void Evaluate_WhenChatHistoryIsNull_ShouldAllow()
    {
        using var guard = CreateGuard(new LlmRequestThrottleOptions());
        var query = new ApplicationChatQuery("hi", "tlovejoy") { ChatHistory = null! };

        guard.Evaluate(query, Client).IsAllowed.ShouldBeTrue();
    }

    [Test]
    public void Evaluate_WhenClientIsOverTheChatRate_ShouldReturn429()
    {
        using var guard = CreateGuard(new LlmRequestThrottleOptions { PermitLimit = 2 });
        var query = new ApplicationChatQuery("hi", "tlovejoy");

        guard.Evaluate(query, Client).IsAllowed.ShouldBeTrue();
        guard.Evaluate(query, Client).IsAllowed.ShouldBeTrue();
        guard.Evaluate(query, Client).ShouldBe(LlmThrottleVerdict.RateLimited);
    }

    [Test]
    public void Evaluate_WhenAnotherClientIsOverTheChatRate_ShouldStillAllowThisClient()
    {
        using var guard = CreateGuard(new LlmRequestThrottleOptions { PermitLimit = 1 });
        var query = new ApplicationChatQuery("hi", "tlovejoy");
        guard.Evaluate(query, "user:other");

        guard.Evaluate(query, "user:other").StatusCode.ShouldBe(StatusCodes.Status429TooManyRequests);
        guard.Evaluate(query, Client).IsAllowed.ShouldBeTrue();
    }

    [Test]
    public void Evaluate_WhenRequestIsOversize_ShouldNotSpendAChatPermit()
    {
        using var guard = CreateGuard(new LlmRequestThrottleOptions { PermitLimit = 1, MaxPromptChars = 2 });

        guard.Evaluate(new ApplicationChatQuery("too long", "tlovejoy"), Client).StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        guard.Evaluate(new ApplicationChatQuery("ok", "tlovejoy"), Client).IsAllowed.ShouldBeTrue();
    }

    [Test]
    public void Evaluate_WhenDisabled_ShouldAllowOversizeAndOverRateRequests()
    {
        using var guard = CreateGuard(new LlmRequestThrottleOptions { Enabled = false, PermitLimit = 1, MaxPromptChars = 1 });
        var query = new ApplicationChatQuery("too long", "tlovejoy");

        guard.Evaluate(query, Client).IsAllowed.ShouldBeTrue();
        guard.Evaluate(query, Client).IsAllowed.ShouldBeTrue();
    }

    [Test]
    public void Defaults_ShouldBeStricterThanTheGenericApiLimit()
    {
        var chat = new LlmRequestThrottleOptions();

        chat.PermitLimit.ShouldBeLessThan(new ApiRateLimitingOptions().PermitLimit);
        chat.Enabled.ShouldBeTrue();
    }

    private static LlmRequestThrottleGuard CreateGuard(LlmRequestThrottleOptions options) =>
        new(Options.Create(options));

    private static ApplicationChatQuery ChatWithHistory(params string[] contents) =>
        new("hi", "tlovejoy") { ChatHistory = contents.Select(content => new ChatHistoryMessage("user", content)).ToList() };
}
