using System.Threading.RateLimiting;
using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UI.Server.RateLimiting;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.RateLimiting;

[TestFixture]
public class SlidingWindowRuleTests
{
    [Test]
    public void ToSlidingWindowRule_WithDefaultOptions_ShouldKeepConfiguredValues()
    {
        var rule = new ApiRateLimitingOptions().ToSlidingWindowRule();

        rule.PermitLimit.ShouldBe(100);
        rule.WindowSeconds.ShouldBe(60);
        rule.Window.ShouldBe(TimeSpan.FromSeconds(60));
        rule.SegmentsPerWindow.ShouldBe(4);
        rule.QueueLimit.ShouldBe(0);
        rule.AdvertisedPermitLimit.ShouldBe("100");
    }

    [Test]
    public void ToSlidingWindowRule_WithZeroValues_ShouldClampToTheSmallestValidRule()
    {
        var options = new ApiRateLimitingOptions { PermitLimit = 0, WindowSeconds = 0, SegmentsPerWindow = 0, QueueLimit = 0 };

        var rule = options.ToSlidingWindowRule();

        rule.PermitLimit.ShouldBe(1);
        rule.WindowSeconds.ShouldBe(1);
        rule.Window.ShouldBe(TimeSpan.FromSeconds(1));
        rule.SegmentsPerWindow.ShouldBe(1);
        rule.QueueLimit.ShouldBe(0);
    }

    [Test]
    public void ToSlidingWindowRule_WithNegativeValues_ShouldClampEveryValue()
    {
        var options = new ApiRateLimitingOptions { PermitLimit = -5, WindowSeconds = -10, SegmentsPerWindow = -2, QueueLimit = -3 };

        var rule = options.ToSlidingWindowRule();

        rule.PermitLimit.ShouldBe(1);
        rule.WindowSeconds.ShouldBe(1);
        rule.SegmentsPerWindow.ShouldBe(1);
        rule.QueueLimit.ShouldBe(0);
    }

    [TestCase(0, "0")]
    [TestCase(-5, "-5")]
    [TestCase(250, "250")]
    public void AdvertisedPermitLimit_WithAnyConfiguredLimit_ShouldAdvertiseTheConfiguredValue(int configured, string expected)
    {
        new ApiRateLimitingOptions { PermitLimit = configured }.ToSlidingWindowRule().AdvertisedPermitLimit.ShouldBe(expected);
    }

    [TestCase(-3L, "0")]
    [TestCase(0L, "0")]
    [TestCase(42L, "42")]
    public void AdvertisedRemaining_WithAvailablePermits_ShouldNeverBeNegative(long available, string expected)
    {
        SlidingWindowRule.AdvertisedRemaining(available).ShouldBe(expected);
    }

    [Test]
    public void ToLimiterOptions_WithClampedRule_ShouldCarryTheNormalizedValues()
    {
        var rule = new ApiRateLimitingOptions { PermitLimit = 0, WindowSeconds = 0, SegmentsPerWindow = 0, QueueLimit = 7 }
            .ToSlidingWindowRule();

        var limiterOptions = rule.ToLimiterOptions();

        limiterOptions.PermitLimit.ShouldBe(1);
        limiterOptions.Window.ShouldBe(TimeSpan.FromSeconds(1));
        limiterOptions.SegmentsPerWindow.ShouldBe(1);
        limiterOptions.QueueLimit.ShouldBe(7);
        limiterOptions.QueueProcessingOrder.ShouldBe(QueueProcessingOrder.OldestFirst);
        limiterOptions.AutoReplenishment.ShouldBeTrue();
    }
}
