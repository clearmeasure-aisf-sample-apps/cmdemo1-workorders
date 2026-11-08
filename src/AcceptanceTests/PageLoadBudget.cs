using System.Diagnostics;

namespace ClearMeasure.Bootcamp.AcceptanceTests;

/// <summary>
/// Counts same-origin application API calls and wall-clock time for one signed-in navigation.
/// An application API call is an HTTP request under <c>/api/</c> on the app origin
/// (the Blazor remoting POST <c>/api/blazor-wasm-single-api</c> and other app endpoints).
/// Framework files, static assets, and document requests are not counted.
/// The quiet window that confirms in-flight calls have finished is not included in <see cref="PageLoadMeasurement.Elapsed"/>.
/// </summary>
public static class PageLoadBudget
{
    /// <summary>
    /// How long the browser must stay free of in-flight app API calls before a load is finished.
    /// Long enough that a call which starts on the next Blazor turn is still counted.
    /// </summary>
    private static readonly TimeSpan ApiQuietWindow = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// A same-origin request whose path contains <c>/api/</c>.
    /// </summary>
    private static bool IsAppApiRequest(string requestUrl, string appBaseUrl)
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
    /// Waits until the page has made no app API call for <see cref="ApiQuietWindow"/>.
    /// Call this after login, before <see cref="MeasureAsync"/>, so the measurement starts from a settled shell.
    /// </summary>
    public static async Task WaitForQuietAsync(IPage page, string appBaseUrl, int timeoutMs = 30_000)
    {
        var probe = new ApiCallProbe(page, appBaseUrl);
        try
        {
            await WaitUntilQuietAsync(probe, timeoutMs);
        }
        finally
        {
            probe.Detach();
        }
    }

    /// <summary>
    /// Times <paramref name="navigate"/> until <paramref name="ready"/> is visible and app API calls have gone quiet.
    /// </summary>
    public static async Task<PageLoadMeasurement> MeasureAsync(
        IPage page,
        string appBaseUrl,
        Func<Task> navigate,
        ILocator ready,
        int timeoutMs = 30_000)
    {
        var probe = new ApiCallProbe(page, appBaseUrl);
        var started = Stopwatch.GetTimestamp();
        try
        {
            await navigate();
            await ready.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = timeoutMs
            });
            var loadedAt = await WaitUntilQuietAsync(probe, timeoutMs);
            var (count, calls) = probe.Snapshot();
            return new PageLoadMeasurement(count, Stopwatch.GetElapsedTime(started, loadedAt), calls);
        }
        finally
        {
            probe.Detach();
        }
    }

    private static async Task<long> WaitUntilQuietAsync(ApiCallProbe probe, int timeoutMs)
    {
        var timeout = Stopwatch.StartNew();
        long quietBeganAt = 0;
        while (timeout.ElapsedMilliseconds < timeoutMs)
        {
            if (probe.InFlight == 0)
            {
                if (quietBeganAt == 0)
                {
                    quietBeganAt = Stopwatch.GetTimestamp();
                }

                if (Stopwatch.GetElapsedTime(quietBeganAt) >= ApiQuietWindow)
                {
                    return quietBeganAt;
                }
            }
            else
            {
                quietBeganAt = 0;
            }

            await Task.Delay(25);
        }

        var (count, calls) = probe.Snapshot();
        throw new TimeoutException(
            $"App API calls were still in flight after {timeoutMs} ms ({probe.InFlight} in flight, {count} seen). {string.Join("; ", calls)}");
    }

    private sealed class ApiCallProbe
    {
        private readonly IPage _page;
        private readonly string _appBaseUrl;
        private readonly Lock _gate = new();
        private readonly HashSet<IRequest> _tracked = [];
        private readonly List<string> _calls = [];
        private int _inFlight;

        // ReSharper disable once ConvertToPrimaryConstructor -- constructor subscribes Playwright events
        public ApiCallProbe(IPage page, string appBaseUrl)
        {
            _page = page;
            _appBaseUrl = appBaseUrl;
            _page.Request += OnRequest;
            _page.RequestFinished += OnSettled;
            _page.RequestFailed += OnSettled;
        }

        public int InFlight => Volatile.Read(ref _inFlight);

        public void Detach()
        {
            _page.Request -= OnRequest;
            _page.RequestFinished -= OnSettled;
            _page.RequestFailed -= OnSettled;
        }

        public (int Count, string[] Calls) Snapshot()
        {
            lock (_gate)
            {
                return (_calls.Count, _calls.ToArray());
            }
        }

        private void OnRequest(object? sender, IRequest request)
        {
            if (!IsAppApiRequest(request.Url, _appBaseUrl))
            {
                return;
            }

            lock (_gate)
            {
                if (!_tracked.Add(request))
                {
                    return;
                }

                _calls.Add(Describe(request));
            }

            Interlocked.Increment(ref _inFlight);
        }

        private void OnSettled(object? sender, IRequest request)
        {
            lock (_gate)
            {
                if (!_tracked.Remove(request))
                {
                    return;
                }
            }

            Interlocked.Decrement(ref _inFlight);
        }

        private static string Describe(IRequest request)
        {
            var path = Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) ? uri.AbsolutePath : request.Url;
            var typeName = TryReadTypeName(request.PostData);
            return typeName is null ? $"{request.Method} {path}" : $"{request.Method} {path} ({typeName})";
        }

        private static string? TryReadTypeName(string? postData)
        {
            if (string.IsNullOrEmpty(postData))
            {
                return null;
            }

            var marker = postData.IndexOf("typeName", StringComparison.OrdinalIgnoreCase);
            if (marker < 0)
            {
                return null;
            }

            var colon = postData.IndexOf(':', marker);
            if (colon < 0)
            {
                return null;
            }

            var open = postData.IndexOf('"', colon + 1);
            if (open < 0 || open + 1 >= postData.Length)
            {
                return null;
            }

            var close = postData.IndexOf('"', open + 1);
            if (close <= open)
            {
                return null;
            }

            var value = postData[(open + 1)..close];
            var comma = value.IndexOf(',');
            if (comma >= 0)
            {
                value = value[..comma];
            }

            var dot = value.LastIndexOf('.');
            var name = dot >= 0 && dot < value.Length - 1 ? value[(dot + 1)..] : value;
            return name.Length == 0 ? null : name;
        }
    }
}

/// <summary>
/// App API calls and wall-clock time for one measured navigation.
/// </summary>
public sealed record PageLoadMeasurement(int ApiCallCount, TimeSpan Elapsed, IReadOnlyList<string> ApiCalls)
{
    /// <summary>
    /// One line a failing assertion can print: the count, the elapsed time, and each call.
    /// </summary>
    public string Describe() =>
        $"{ApiCallCount} API calls in {Elapsed.TotalMilliseconds:0} ms: {string.Join("; ", ApiCalls)}";
}
