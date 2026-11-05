using ClearMeasure.Bootcamp.Core.FeatureFlags;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace ClearMeasure.Bootcamp.McpServer;

/// <summary>
/// MCP call-tool filter that turns <see cref="FeatureDisabledException"/> from the bus into a tool error whose
/// text names the disabled feature, instead of the SDK's generic error message.
/// </summary>
public static class FeatureDisabledToolFilter
{
    /// <summary>Wraps <paramref name="next"/>; register with <c>AddCallToolFilter</c>.</summary>
    public static McpRequestHandler<CallToolRequestParams, CallToolResult> Create(
        McpRequestHandler<CallToolRequestParams, CallToolResult> next) =>
        async (request, cancellationToken) =>
        {
            try
            {
                return await next(request, cancellationToken);
            }
            catch (FeatureDisabledException ex)
            {
                return new CallToolResult
                {
                    IsError = true,
                    Content = [new TextContentBlock { Text = ex.Message }]
                };
            }
        };
}
