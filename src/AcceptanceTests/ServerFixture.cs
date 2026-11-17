// ReSharper disable MethodHasAsyncOverload -- Qodana P5 (#9440): synchronous TestContext.Out.WriteLine diagnostics/setup are intentional

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using ClearMeasure.Bootcamp.AcceptanceTests.TestUsers;
using ClearMeasure.Bootcamp.Core;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.AcceptanceTests;

[SetUpFixture]
public class ServerFixture
{
    private const string ProjectPath = "../../../../UI/Server";
    private const int WaitTimeoutSeconds = 60;

    private static string BuildConfiguration =>
        AppDomain.CurrentDomain.BaseDirectory.Contains(
            Path.DirectorySeparatorChar + "Release" + Path.DirectorySeparatorChar)
            ? "Release"
            : "Debug";
    public static bool StartLocalServer { get; private set; } = true;
    public static int SlowMo { get; private set; }
    public static string ApplicationBaseUrl { get; private set; } = string.Empty;
    private Process? _serverProcess;
    public static bool SkipScreenshotsForSpeed { get; private set; } = true;
    public static bool HeadlessTestBrowser { get; private set; } = true;
    private static bool DatabaseInitialized { get; set; }
    private static readonly Lock DatabaseLock = new();
    private const int ServerOutputLines = 60;
    private static readonly ConcurrentQueue<string> ServerOutput = new();
    
    /// <summary>
    /// Shared Playwright instance for all tests. Thread-safe for parallel execution.
    /// </summary>
    public static IPlaywright Playwright { get; private set; } = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        ReadHostSettings();
        MaybeInitializeSharedData();
        Assertions.SetDefaultExpectTimeout(30_000);
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await StartLocalProcessesIfNeeded();
        await WarmUpContainerApp();
        await VerifyApplicationHealthy();
        await new BlazorWasmWarmUp(Playwright, ApplicationBaseUrl).ExecuteAsync();
    }

    private static void ReadHostSettings()
    {
        var configuration = TestHost.GetRequiredService<IConfiguration>();
        ApplicationBaseUrl = configuration["ApplicationBaseUrl"] ?? throw new InvalidOperationException();
        StartLocalServer = configuration.GetValue<bool>("StartLocalServer");
        SkipScreenshotsForSpeed = configuration.GetValue<bool>("SkipScreenshotsForSpeed");
        SlowMo = configuration.GetValue<int>("SlowMo");
        HeadlessTestBrowser = configuration.GetValue<bool>("HeadlessTestBrowser");
    }

    private static void MaybeInitializeSharedData()
    {
        if (!AcceptanceSharedDataPolicy.ShouldLoadSharedData(StartLocalServer))
        {
            TestContext.Out.WriteLine(
                "PR-environment / remote host: skipping ZDataLoader and leftover-employee sweep.");
            return;
        }

        InitializeDatabaseOnce();
    }

    private async Task StartLocalProcessesIfNeeded()
    {
        if (!StartLocalServer)
        {
            return;
        }

        await StartAndWaitForServer();
        await ResetServerDbConnections();
    }

    /// <summary>
    /// Sends HTTP warm-up requests to the Container App before Playwright browsers launch.
    /// This primes server-side caches, JIT compilation, and triggers Blazor WASM bundle download
    /// so that browser-based tests encounter a warmed-up application.
    /// </summary>
    private static async Task WarmUpContainerApp()
    {
        if (StartLocalServer) return; // local server is already warmed by StartAndWaitForServer

        var client = TestHttpClientFactory.CreateInsecureClient();

        string[] warmUpPaths = ["/", "/_healthcheck", "/_clienthealthcheck"];

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            TestContext.Out.WriteLine($"HTTP warm-up: round {attempt}/3");
            foreach (var path in warmUpPaths)
            {
                try
                {
                    var response = await client.GetAsync($"{ApplicationBaseUrl}{path}");
                    TestContext.Out.WriteLine($"  {path} -> {(int)response.StatusCode}");
                }
                catch (Exception ex)
                {
                    TestContext.Out.WriteLine($"  {path} -> {ex.GetType().Name}: {ex.Message}");
                }
            }

            await Task.Delay(2000);
        }
    }

    /// <summary>
    /// Verifies the application is reachable and healthy before tests start.
    /// Checks the site root and the /_healthcheck endpoint (which validates database
    /// connectivity). Fails fast with a clear diagnostic message instead of letting
    /// tests hang on an unreachable or unhealthy server.
    /// </summary>
    private static async Task VerifyApplicationHealthy()
    {
        const int maxAttempts = 3;
        const int delayBetweenAttemptsMs = 5000;
        var client = TestHttpClientFactory.CreateInsecureClient();

        TestContext.Out.WriteLine("Health gate: verifying site is reachable...");
        var siteResult = await TryReachSite(client, maxAttempts, delayBetweenAttemptsMs);
        if (!siteResult.Succeeded)
        {
            Assert.Fail(
                $"Health gate FAILED: Site is not reachable at {ApplicationBaseUrl} after {maxAttempts} attempts. {siteResult.Detail}");
        }

        TestContext.Out.WriteLine("Health gate: verifying /_healthcheck...");
        var healthResult = await TryGetHealthBody(client, maxAttempts, delayBetweenAttemptsMs);
        if (!healthResult.Succeeded)
        {
            Assert.Fail(
                $"Health gate FAILED: /_healthcheck did not return Healthy or Degraded after {maxAttempts} attempts. {healthResult.Detail}");
        }

        TestContext.Out.WriteLine("Health gate: PASSED - site is reachable and healthy.");
    }

    private static async Task<(bool Succeeded, string Detail)> TryReachSite(
        HttpClient client,
        int maxAttempts,
        int delayBetweenAttemptsMs)
    {
        HttpResponseMessage? siteResponse = null;
        Exception? lastSiteException = null;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            (siteResponse, lastSiteException) =
                await AttemptGet(client, ApplicationBaseUrl, siteResponse);
            if (siteResponse?.IsSuccessStatusCode == true)
            {
                return (true, string.Empty);
            }

            await DelayIfMoreAttempts(attempt, maxAttempts, delayBetweenAttemptsMs);
        }

        return (false, FormatReachFailure(lastSiteException, siteResponse));
    }

    private static async Task<(bool Succeeded, string Detail)> TryGetHealthBody(
        HttpClient client,
        int maxAttempts,
        int delayBetweenAttemptsMs)
    {
        var healthUrl = $"{ApplicationBaseUrl}/_healthcheck";
        string? healthBody = null;
        HttpStatusCode? healthStatus = null;
        Exception? lastHealthException = null;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var attemptResult = await AttemptHealthGet(client, healthUrl);
            if (attemptResult.Exception != null)
            {
                lastHealthException = attemptResult.Exception;
            }
            else
            {
                lastHealthException = null;
                healthStatus = attemptResult.Status;
                healthBody = attemptResult.Body;
                if (IsSuccessfulHealth(attemptResult.Status, attemptResult.Body))
                {
                    return (true, string.Empty);
                }
            }

            await DelayIfMoreAttempts(attempt, maxAttempts, delayBetweenAttemptsMs);
        }

        return (false, FormatHealthFailure(lastHealthException, healthStatus, healthBody));
    }

    private static async Task<(HttpResponseMessage? Response, Exception? Exception)> AttemptGet(
        HttpClient client,
        string url,
        HttpResponseMessage? previousResponse)
    {
        try
        {
            var response = await client.GetAsync(url);
            TestContext.Out.WriteLine($"  GET {url} -> {(int)response.StatusCode}");
            return (response, null);
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"  GET {url} -> {ex.GetType().Name}: {ex.Message}");
            return (previousResponse, ex);
        }
    }

    private static async Task<(HttpStatusCode? Status, string? Body, Exception? Exception)> AttemptHealthGet(
        HttpClient client,
        string healthUrl)
    {
        try
        {
            var response = await client.GetAsync(healthUrl);
            var body = await response.Content.ReadAsStringAsync();
            TestContext.Out.WriteLine($"  GET {healthUrl} -> {(int)response.StatusCode}: {body}");
            return (response.StatusCode, body, null);
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"  GET {healthUrl} -> {ex.GetType().Name}: {ex.Message}");
            return (null, null, ex);
        }
    }

    private static bool IsSuccessfulHealth(HttpStatusCode? status, string? body)
    {
        if (status is null || body is null)
        {
            return false;
        }

        var code = (int)status.Value;
        return code is >= 200 and < 300 && IsAcceptableHealthStatus(body);
    }

    private static async Task DelayIfMoreAttempts(int attempt, int maxAttempts, int delayBetweenAttemptsMs)
    {
        if (attempt < maxAttempts)
        {
            await Task.Delay(delayBetweenAttemptsMs);
        }
    }

    private static string FormatReachFailure(Exception? lastSiteException, HttpResponseMessage? siteResponse) =>
        lastSiteException != null
            ? $"Last exception: {lastSiteException.GetType().Name}: {lastSiteException.Message}"
            : $"Last status code: {siteResponse?.StatusCode}";

    private static string FormatHealthFailure(
        Exception? lastHealthException,
        HttpStatusCode? healthStatus,
        string? healthBody) =>
        lastHealthException != null
            ? $"Last exception: {lastHealthException.GetType().Name}: {lastHealthException.Message}"
            : $"Status: {healthStatus}, Body: {healthBody}";

    private static bool IsAcceptableHealthStatus(string body) =>
        body.Contains("Healthy", StringComparison.OrdinalIgnoreCase) ||
        body.Contains("Degraded", StringComparison.OrdinalIgnoreCase);

    private async Task StartAndWaitForServer()
    {
        const int maxAttempts = 2;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await StartAndWaitForServerOnce();
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                TestContext.Out.WriteLine(
                    $"  [Server] start attempt {attempt}/{maxAttempts} failed, retrying: {ex.Message}");
                await ProcessCleanupHelper.StopServerProcessAsync(_serverProcess, ApplicationBaseUrl);
                try { _serverProcess?.Dispose(); } catch (ObjectDisposedException) { }
                _serverProcess = null;
            }
        }
    }

    private async Task StartAndWaitForServerOnce()
    {
        var connectionString = GetSqlConnectionString();
        var useSqlite = IsSqliteConnection(connectionString);
        _serverProcess = CreateDotnetProcess(ProjectPath, BuildServerArguments(useSqlite));
        ConfigureServerEnvironment(_serverProcess, useSqlite, connectionString);
        AttachProcessLogging(_serverProcess, "Server");
        _serverProcess.Start();
        _serverProcess.BeginOutputReadLine();
        _serverProcess.BeginErrorReadLine();
        await WaitUntilUrlReady(ApplicationBaseUrl);
    }

    private static string BuildServerArguments(bool useSqlite)
    {
        var config = BuildConfiguration;
        return useSqlite
            ? $"run --no-build --configuration {config} --no-launch-profile --urls={ApplicationBaseUrl}"
            : $"run --no-build --configuration {config} --urls={ApplicationBaseUrl}";
    }

    private static void ConfigureServerEnvironment(Process process, bool useSqlite, string connectionString)
    {
        process.StartInfo.Environment["DISABLE_AUTO_CANCEL_AGENT"] = "true";
        process.StartInfo.Environment["ApiKeyAuthentication__Enabled"] = "false";
        process.StartInfo.Environment["ApiKeyAuthentication__ValidationKey"] = "";
        process.StartInfo.Environment["FeatureFlags__BuildingManagement"] = "true";
        process.StartInfo.Environment["FeatureFlags__VendorManagement"] = "true";
        // Pool-slot test-support surface: create data via HTTP instead of SQL from the suite.
        process.StartInfo.Environment["TestSupport__Enabled"] = "true";
        process.StartInfo.Environment["TestSupport__Secret"] = TestSupport.AcceptanceTestSupportSettings.Secret;
        if (useSqlite)
        {
            ApplySqliteServerEnvironment(process, connectionString);
        }
    }

    private static void ApplySqliteServerEnvironment(Process process, string connectionString)
    {
        // Development loads appsettings.Development.json: BuildingManagement and VendorManagement are ON (tdd default).
        process.StartInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        process.StartInfo.Environment["ConnectionStrings__SqlConnectionString"] =
            ResolveSqliteConnectionString(connectionString);
    }

    private static string ResolveSqliteConnectionString(string connectionString)
    {
        if (connectionString.Contains(":memory:", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        var dbPath = connectionString["Data Source=".Length..].Trim();
        var semicolonIndex = dbPath.IndexOf(';');
        if (semicolonIndex >= 0)
        {
            dbPath = dbPath[..semicolonIndex];
        }

        if (!Path.IsPathRooted(dbPath))
        {
            dbPath = Path.GetFullPath(dbPath);
        }

        return $"Data Source={dbPath}";
    }

    private static async Task WaitUntilUrlReady(string baseUrl)
    {
        var client = TestHttpClientFactory.CreateInsecureClient();
        var timeout = TimeSpan.FromSeconds(WaitTimeoutSeconds);
        var start = DateTime.UtcNow;
        Exception? lastException = null;
        while (DateTime.UtcNow - start < timeout)
        {
            try
            {
                var response = await client.GetAsync(baseUrl);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
            }

            await Task.Delay(1000);
        }

        throw new Exception(
            $"UI.Server did not start in {WaitTimeoutSeconds} seconds. Last exception: {lastException}");
    }

    private static string GetSqlConnectionString()
    {
        var configuration = TestHost.GetRequiredService<IConfiguration>();
        return configuration.GetConnectionString("SqlConnectionString") ?? "";
    }

    private static bool IsSqliteConnection(string connectionString) =>
        connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase);

    private static Process CreateDotnetProcess(string workingDirectory, string arguments) =>
        new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

    private static void AttachProcessLogging(Process process, string label)
    {
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                Log($"  [{label} stdout] {e.Data}");
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                Log($"  [{label} stderr] {e.Data}");
            }
        };
    }

    /// <summary>The last server output lines, for failures raised before any test runs (OneTimeSetUp output is not in the TRX).</summary>
    internal static string RecentServerOutput() => string.Join(Environment.NewLine, ServerOutput);

    private static void Log(string line)
    {
        TestContext.Out.WriteLine(line);
        ServerOutput.Enqueue(line);
        if (ServerOutput.Count > ServerOutputLines)
        {
            ServerOutput.TryDequeue(out _);
        }
    }

    private static async Task ResetServerDbConnections()
    {
        var client = TestHttpClientFactory.CreateInsecureClient();
        var response = await client.PostAsync($"{ApplicationBaseUrl}/_diagnostics/reset-db-connections", null);
        response.EnsureSuccessStatusCode();
    }

    private static void InitializeDatabaseOnce()
    {
        if (DatabaseInitialized) return;

        lock (DatabaseLock)
        {
            if (DatabaseInitialized) return;

            using var context = TestHost.GetRequiredService<DbContext>();
            var isSqlite = context.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;
            if (isSqlite)
            {
                DataAccess.SqliteSchemaUpgrade.EnsureCreatedAndUpgraded(context);
            }

            new ZDataLoader().LoadData();
            TestContext.Out.WriteLine("ZDataLoader().LoadData(); - complete");

            var deleted = AcceptanceTestEmployees.DeleteUnreferenced(context, AcceptanceTestEmployees.UserNamePrefix);
            TestContext.Out.WriteLine($"Deleted {deleted} unreferenced employee(s) left by earlier acceptance runs");

            // Release all pooled connections so the server process opens the
            // database file with a clean view of the seeded data
            TestHost.GetRequiredService<IDatabaseConfiguration>().ResetConnectionPool();

            DatabaseInitialized = true;
        }
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await ProcessCleanupHelper.StopServerProcessAsync(_serverProcess, ApplicationBaseUrl);
        try { _serverProcess?.Dispose(); } catch (ObjectDisposedException) { }
        _serverProcess = null;
        await TestSupport.WorkerBrowserPool.DisposeAllAsync();
        Playwright.Dispose();
    }
}