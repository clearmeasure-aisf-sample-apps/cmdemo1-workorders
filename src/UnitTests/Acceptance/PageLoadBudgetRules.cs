namespace ClearMeasure.Bootcamp.AcceptanceTests;

/// <summary>
/// Counts application API calls in a recorded browser trace and checks that count against a min/max.
/// The live page-load measurement uses this same origin and <c>/api/</c> rule.
/// Static files, <c>_framework</c>, documents, and websockets fall outside that rule and are not counted.
/// Defined in the unit-test assembly, which the acceptance tests already reference, so both call one type.
/// </summary>
internal static class PageLoadBudgetRules
{
    /// <summary>
    /// One browser request. The resource type is the Playwright type
    /// (fetch, stylesheet, websocket, and so on). The count uses the URL.
    /// </summary>
    internal sealed record RecordedRequest(string Url, string? ResourceType);

    /// <summary>
    /// API-call count for recorded requests and whether it sits inside the budget.
    /// </summary>
    internal sealed record Verdict(int ApiCallCount, bool Passed, string Detail);

    /// <summary>
    /// A same-origin request whose path contains <c>/api/</c>.
    /// </summary>
    internal static bool IsAppApiRequest(string requestUrl, string appBaseUrl)
    {
        if (!Uri.TryCreate(requestUrl, UriKind.Absolute, out var request)
            || !Uri.TryCreate(appBaseUrl, UriKind.Absolute, out var app))
        {
            return false;
        }

        if (!string.Equals(request.Scheme, app.Scheme, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(request.Host, app.Host, StringComparison.OrdinalIgnoreCase)
            || request.Port != app.Port)
        {
            return false;
        }

        return request.AbsolutePath.Contains("/api/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// How many recorded requests are application API calls.
    /// </summary>
    internal static int CountApiCalls(IReadOnlyList<RecordedRequest> requests, string appBaseUrl)
    {
        var count = 0;
        foreach (var request in requests)
        {
            if (IsAppApiRequest(request.Url, appBaseUrl))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Counts API calls and fails when the count is below <paramref name="minimumCalls"/>
    /// or above <paramref name="maximumCalls"/>.
    /// </summary>
    internal static Verdict Evaluate(
        IReadOnlyList<RecordedRequest> requests,
        string appBaseUrl,
        int minimumCalls,
        int maximumCalls)
    {
        var counted = new List<string>();
        foreach (var request in requests)
        {
            if (IsAppApiRequest(request.Url, appBaseUrl))
            {
                counted.Add(Describe(request));
            }
        }

        var count = counted.Count;
        if (count < minimumCalls)
        {
            return new Verdict(count, false, Outside("below the minimum", count, minimumCalls, counted));
        }

        if (count > maximumCalls)
        {
            return new Verdict(count, false, Outside("above the maximum", count, maximumCalls, counted));
        }

        return new Verdict(count, true, $"{count} API calls is within {minimumCalls}..{maximumCalls}.");
    }

    private static string Describe(RecordedRequest request)
    {
        var path = Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) ? uri.AbsolutePath : request.Url;
        return string.IsNullOrEmpty(request.ResourceType) ? path : request.ResourceType + " " + path;
    }

    private static string Outside(string relation, int count, int limit, List<string> counted) =>
        $"{count} API calls is {relation} {limit}: {string.Join("; ", counted)}";
}
