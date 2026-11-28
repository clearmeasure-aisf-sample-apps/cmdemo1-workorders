using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UI.Server.RateLimiting;
using Microsoft.Extensions.Options;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.RateLimiting;

[TestFixture]
public class LlmRequestThrottleGuardTests
{
    private const string Client = "user:tlovejoy";

    [Test]
    public void FindSizeViolation_WhenPromptIsAtTheLimit_ShouldReturnNull()
    {
        using var guard = CreateGuard(new LlmRequestThrottleOptions { MaxPromptChars = 5 });

        guard.FindSizeViolation(new ApplicationChatQuery("12345", "tlovejoy")).ShouldBeNull();
    }

    [Test]
    public void FindSizeViolation_WhenPromptIsOverTheLimit_ShouldDescribeThePromptRule()
    {
        using var guard = CreateGuard(new LlmRequestThrottleOptions { MaxPromptChars = 5 });

        guard.FindSizeViolation(new ApplicationChatQuery("123456", "tlovejoy"))
            .ShouldBe("Prompt exceeds 5 characters.");
    }

    [Test]
    public void FindSizeViolation_WhenChatHistoryHasTooManyEntries_ShouldDescribeTheEntryRule()
    {
        using var guard = CreateGuard(new LlmRequestThrottleOptions { MaxChatHistoryEntries = 2 });

        guard.FindSizeViolation(ChatWithHistory("a", "b", "c")).ShouldBe("Chat history exceeds 2 entries.");
    }

    [Test]
    public void FindSizeViolation_WhenChatHistoryHasTooManyCharacters_ShouldDescribeTheCharacterRule()
    {
        using var guard = CreateGuard(new LlmRequestThrottleOptions { MaxChatHistoryChars = 5 });

        guard.FindSizeViolation(ChatWithHistory("abc", "def")).ShouldBe("Chat history exceeds 5 characters.");
    }

    [Test]
    public void FindSizeViolation_WhenChatHistoryFitsBothLimits_ShouldReturnNull()
    {
        using var guard = CreateGuard(new LlmRequestThrottleOptions { MaxChatHistoryEntries = 2, MaxChatHistoryChars = 6 });

        guard.FindSizeViolation(ChatWithHistory("abc", "def")).ShouldBeNull();
    }

    [Test]
    public void FindSizeViolation_WhenChatHistoryIsNull_ShouldReturnNull()
    {
        using var guard = CreateGuard(new LlmRequestThrottleOptions());
        var query = new ApplicationChatQuery("hi", "tlovejoy") { ChatHistory = null! };

        guard.FindSizeViolation(query).ShouldBeNull();
    }

    [Test]
    public void TryAcquire_WhenClientIsOverTheChatRate_ShouldReturnFalse()
    {
        using var guard = CreateGuard(new LlmRequestThrottleOptions { PermitLimit = 2 });

        guard.TryAcquire(Client).ShouldBeTrue();
        guard.TryAcquire(Client).ShouldBeTrue();
        guard.TryAcquire(Client).ShouldBeFalse();
    }

    [Test]
    public void TryAcquire_WhenAnotherClientIsOverTheChatRate_ShouldStillAllowThisClient()
    {
        using var guard = CreateGuard(new LlmRequestThrottleOptions { PermitLimit = 1 });
        guard.TryAcquire("user:other");

        guard.TryAcquire("user:other").ShouldBeFalse();
        guard.TryAcquire(Client).ShouldBeTrue();
    }

    [Test]
    public void Rule_ShouldAdvertiseTheConfiguredChatWindow()
    {
        using var guard = CreateGuard(new LlmRequestThrottleOptions { PermitLimit = 3, WindowSeconds = 15 });

        guard.Rule.PermitLimit.ShouldBe(3);
        guard.Rule.WindowSeconds.ShouldBe(15);
        guard.Rule.QueueLimit.ShouldBe(0);
    }

    [Test]
    public void Defaults_ShouldBeStricterThanTheGenericApiLimit()
    {
        var chat = new LlmRequestThrottleOptions();

        chat.PermitLimit.ShouldBeLessThan(new ApiRateLimitingOptions().PermitLimit);
        chat.MaxPromptChars.ShouldBe(8_000);
        chat.MaxChatHistoryEntries.ShouldBe(50);
        chat.MaxChatHistoryChars.ShouldBe(64_000);
        chat.WindowSeconds.ShouldBe(60);
    }

    private static LlmRequestThrottleGuard CreateGuard(LlmRequestThrottleOptions options) =>
        new(Options.Create(options));

    private static ApplicationChatQuery ChatWithHistory(params string[] contents) =>
        new("hi", "tlovejoy") { ChatHistory = contents.Select(content => new ChatHistoryMessage("user", content)).ToList() };
}
