using System.Threading.RateLimiting;
using ClearMeasure.Bootcamp.Core.Queries;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Server.RateLimiting;

/// <summary>
/// Decides whether an <see cref="ApplicationChatQuery"/> may reach the LLM: it rejects an oversize prompt or chat
/// history, then spends one permit from a chat-only sliding-window partition per client.
/// </summary>
internal sealed class LlmRequestThrottleGuard : IDisposable
{
    private readonly LlmRequestThrottleOptions _options;
    private readonly PartitionedRateLimiter<string> _limiter;

    /// <summary>Builds the guard and its chat partitioned limiter from <paramref name="options"/>.</summary>
    public LlmRequestThrottleGuard(IOptions<LlmRequestThrottleOptions> options)
    {
        _options = options.Value;
        Rule = _options.ToSlidingWindowRule();
        var limiterOptions = Rule.ToLimiterOptions();
        _limiter = PartitionedRateLimiter.Create<string, string>(partitionKey =>
            RateLimitPartition.GetSlidingWindowLimiter(partitionKey, _ => limiterOptions));
    }

    /// <summary>The chat sliding-window rule, also advertised on a 429.</summary>
    public SlidingWindowRule Rule { get; }

    /// <summary>
    /// Size rules first (an oversize request spends no permit), then the chat rate for <paramref name="partitionKey"/>.
    /// </summary>
    public LlmThrottleVerdict Evaluate(ApplicationChatQuery query, string partitionKey)
    {
        if (!_options.Enabled)
        {
            return LlmThrottleVerdict.Allowed;
        }

        var sizeViolation = FindSizeViolation(query);
        if (sizeViolation is not null)
        {
            return LlmThrottleVerdict.TooLarge(sizeViolation);
        }

        using var lease = _limiter.AttemptAcquire(partitionKey);
        return lease.IsAcquired ? LlmThrottleVerdict.Allowed : LlmThrottleVerdict.RateLimited;
    }

    /// <summary>The first size rule the request breaks, or null when it fits every limit.</summary>
    private string? FindSizeViolation(ApplicationChatQuery query)
    {
        if (LengthOf(query.Prompt) > _options.MaxPromptChars)
        {
            return $"Prompt exceeds {_options.MaxPromptChars} characters.";
        }

        return FindHistoryViolation(query.ChatHistory);
    }

    private string? FindHistoryViolation(List<ChatHistoryMessage>? history)
    {
        if (history is null)
        {
            return null;
        }

        if (history.Count > _options.MaxChatHistoryEntries)
        {
            return $"Chat history exceeds {_options.MaxChatHistoryEntries} entries.";
        }

        var historyChars = history.Sum(entry => (long)ContentLength(entry));
        return historyChars > _options.MaxChatHistoryChars
            ? $"Chat history exceeds {_options.MaxChatHistoryChars} characters."
            : null;
    }

    /// <inheritdoc />
    public void Dispose() => _limiter.Dispose();

    private static int LengthOf(string? text) => text?.Length ?? 0;

    private static int ContentLength(ChatHistoryMessage? entry) => LengthOf(entry?.Content);
}
