using ClearMeasure.Bootcamp.UI.Server.TestSupport;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Pool-slot (TestSupport-enabled) evaluation of NeedsReboot from the current HTTP request.
/// tdd/uat/prod leave TestSupport off and keep the process-wide static flag.
/// </summary>
public static class NeedsRebootRequestScope
{
    /// <summary>
    /// When TestSupport is enabled, returns whether this request asked for the unhealthy reboot
    /// state via an authorized header. Null means "use the process-wide static flag".
    /// </summary>
    public static bool? TryGetRequestScopedFlag(HttpContext? httpContext, TestSupportOptions options)
    {
        if (!options.Enabled)
        {
            return null;
        }

        if (httpContext is null)
        {
            return false;
        }

        return IsAuthorizedNeedsReboot(httpContext.Request, options);
    }

    private static bool IsAuthorizedNeedsReboot(HttpRequest request, TestSupportOptions options)
    {
        if (!TestSupportSecret.Matches(request, options.Secret))
        {
            return false;
        }

        if (!request.Headers.TryGetValue(TestSupportConstants.NeedsRebootHeaderName, out var values))
        {
            return false;
        }

        var raw = values.FirstOrDefault();
        return bool.TryParse(raw, out var flag) && flag;
    }
}
