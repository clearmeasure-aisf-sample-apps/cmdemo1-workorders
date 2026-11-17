using System.Diagnostics;
using System.Net;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// The page browsers get when an unhandled exception reaches the pipeline outside Development: no exception details,
/// only the request id to quote to support.
/// </summary>
public static class ErrorPage
{
    private const string RequestIdPlaceholder = "{RequestIdLine}";

    private const string Template = """
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no" />
            <title>Error - .NET Conf Work Orders</title>
            <style>
                body { font-family: system-ui, -apple-system, Segoe UI, sans-serif; margin: 2rem; }
                .text-danger { color: #dc3545; }
                code { font-family: ui-monospace, Consolas, monospace; }
            </style>
        </head>
        <body>
            <div class="main">
                <div class="content px-4">
                    <h1 class="text-danger">Error.</h1>
                    <h2 class="text-danger">An error occurred while processing your request.</h2>
        {RequestIdLine}
                    <h3>Development Mode</h3>
                    <p>
                        Swapping to the <strong>Development</strong> environment displays detailed information about the error that occurred.
                    </p>
                    <p>
                        <strong>The Development environment shouldn't be enabled for deployed applications.</strong>
                        It can result in displaying sensitive information from exceptions to end users.
                        For local debugging, enable the <strong>Development</strong> environment by setting the <strong>ASPNETCORE_ENVIRONMENT</strong> environment variable to <strong>Development</strong>
                        and restarting the app.
                    </p>
                </div>
            </div>
        </body>
        </html>
        """;

    /// <summary>Writes the error page for the failed request.</summary>
    public static Task WriteAsync(HttpContext context)
    {
        var requestId = Activity.Current?.Id ?? context.TraceIdentifier;
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.Headers.CacheControl = "no-store,no-cache";
        context.Response.Headers.Pragma = "no-cache";
        return context.Response.WriteAsync(Render(requestId));
    }

    /// <summary>The page's HTML, naming <paramref name="requestId"/> when there is one.</summary>
    public static string Render(string? requestId) =>
        Template.Replace(RequestIdPlaceholder, RequestIdLine(requestId), StringComparison.Ordinal);

    private static string RequestIdLine(string? requestId) =>
        string.IsNullOrEmpty(requestId)
            ? ""
            : $"<p><strong>Request ID:</strong> <code>{WebUtility.HtmlEncode(requestId)}</code></p>";
}