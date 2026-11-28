namespace ClearMeasure.Bootcamp.UI.Server.RateLimiting;

/// <summary>
/// What <see cref="LlmRequestThrottleGuard"/> decided for one chat request: allowed, 400 (too large, with the broken
/// rule as <see cref="Detail"/>) or 429 (over the chat rate).
/// </summary>
internal sealed record LlmThrottleVerdict(int StatusCode, string? Detail)
{
    /// <summary>The request may reach the LLM.</summary>
    public static LlmThrottleVerdict Allowed { get; } = new(StatusCodes.Status200OK, null);

    /// <summary>The client is over the chat rate limit.</summary>
    public static LlmThrottleVerdict RateLimited { get; } = new(StatusCodes.Status429TooManyRequests, null);

    /// <summary>The prompt or chat history breaks a size rule described by <paramref name="detail"/>.</summary>
    public static LlmThrottleVerdict TooLarge(string detail) => new(StatusCodes.Status400BadRequest, detail);

    /// <summary>True when the request may reach the LLM.</summary>
    public bool IsAllowed => StatusCode == StatusCodes.Status200OK;
}
