namespace ClearMeasure.Bootcamp.UI.Server.RateLimiting;

/// <summary>
/// Configuration for <see cref="LlmRequestThrottleGuard"/>: the largest application-chat request accepted and the
/// chat-only rate limit, stricter than the generic API limit in <see cref="ApiRateLimitingOptions"/>.
/// </summary>
public sealed class LlmRequestThrottleOptions
{
    /// <summary>Configuration section bound to these options.</summary>
    public const string SectionName = "LlmRequestThrottle";

    /// <summary>When false, chat requests are neither size-checked nor chat rate limited.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Most characters allowed in the chat prompt.</summary>
    public int MaxPromptChars { get; init; } = 8_000;

    /// <summary>Most chat-history entries allowed on one chat request.</summary>
    public int MaxChatHistoryEntries { get; init; } = 100;

    /// <summary>Most characters allowed across the content of every chat-history entry.</summary>
    public int MaxChatHistoryChars { get; init; } = 64_000;

    /// <summary>Chat requests allowed per client per window.</summary>
    public int PermitLimit { get; init; } = 20;

    /// <summary>Sliding window length in seconds.</summary>
    public int WindowSeconds { get; init; } = 60;

    /// <summary>Number of segments the window is divided into.</summary>
    public int SegmentsPerWindow { get; init; } = 4;

    /// <summary>The normalized sliding-window rule for the chat partition (no queueing: over the limit fails at once).</summary>
    internal SlidingWindowRule ToSlidingWindowRule() => new(PermitLimit, WindowSeconds, SegmentsPerWindow, queueLimit: 0);
}
