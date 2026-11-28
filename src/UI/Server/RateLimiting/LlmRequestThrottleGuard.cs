using System.Threading.RateLimiting;
using ClearMeasure.Bootcamp.Core.Queries;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Server.RateLimiting;

/// <summary>
/// Decides whether an <see cref="ApplicationChatQuery"/> may reach the LLM: it finds an oversize prompt or chat
/// history, and spends permits from a chat-only sliding-window partition per client.
/// </summary>
internal sealed class LlmRequestThrottleGuard : IDisposable
{
    private const int SegmentsPerWindow = 4;

    private readonly LlmRequestThrottleOptions _options;
    private readonly PartitionedRateLimiter<string> _limiter;

    /// <summary>Builds the guard and its chat partitioned limiter from <paramref name="options"/>.</summary>
    public LlmRequestThrottleGuard(IOptions<LlmRequestThrottleOptions> options)
    {
        _options = options.Value;
        Rule = new SlidingWindowRule(_options.PermitLimit, _options.WindowSeconds, SegmentsPerWindow, queueLimit: 0);
        var limiterOptions = Rule.ToLimiterOptions();
        _limiter = PartitionedRateLimiter.Create<string, string>(partitionKey =>
            RateLimitPartition.GetSlidingWindowLimiter(partitionKey, _ => limiterOptions));
    }

    /// <summary>The chat sliding-window rule, also advertised on a chat 429.</summary>
    public SlidingWindowRule Rule { get; }

    /// <summary>The first size rule <paramref name="query"/> breaks, or null when it fits every limit.</summary>
    public string? FindSizeViolation(ApplicationChatQuery query)
    {
        if (LengthOf(query.Prompt) > _options.MaxPromptChars)
        {
            return $"Prompt exceeds {_options.MaxPromptChars} characters.";
        }

        return FindHistoryViolation(query.ChatHistory);
    }

    /// <summary>Spends one chat permit for <paramref name="partitionKey"/>; false when the client is over the chat rate.</summary>
    public bool TryAcquire(string partitionKey)
    {
        using var lease = _limiter.AttemptAcquire(partitionKey);
        return lease.IsAcquired;
    }

    /// <inheritdoc />
    public void Dispose() => _limiter.Dispose();

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

        var historyChars = history.Sum(entry => (long)LengthOf(entry.Content));
        return historyChars > _options.MaxChatHistoryChars
            ? $"Chat history exceeds {_options.MaxChatHistoryChars} characters."
            : null;
    }

    private static int LengthOf(string? text) => text?.Length ?? 0;
}
