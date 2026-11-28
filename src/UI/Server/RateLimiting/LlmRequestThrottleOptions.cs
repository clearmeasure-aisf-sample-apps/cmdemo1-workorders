namespace ClearMeasure.Bootcamp.UI.Server.RateLimiting;

/// <summary>
/// Configuration for <see cref="LlmRequestThrottleGuard"/>: the largest application-chat request accepted and the
/// chat-only rate limit, stricter than the generic API limit in <see cref="ApiRateLimitingOptions"/>.
/// </summary>
public sealed class LlmRequestThrottleOptions
{
    /// <summary>Configuration section bound to these options.</summary>
    public const string SectionName = "LlmRequestThrottle";

    /// <summary>Most characters allowed in the chat prompt.</summary>
    public int MaxPromptChars { get; init; } = 8_000;

    /// <summary>Most chat-history entries allowed on one chat request.</summary>
    public int MaxChatHistoryEntries { get; init; } = 50;

    /// <summary>Most characters allowed across the content of every chat-history entry.</summary>
    public int MaxChatHistoryChars { get; init; } = 64_000;

    /// <summary>Chat requests allowed per client per window.</summary>
    public int PermitLimit { get; init; } = 10;

    /// <summary>Sliding window length in seconds; also the <c>Retry-After</c> value on a chat 429.</summary>
    public int WindowSeconds { get; init; } = 60;
}
