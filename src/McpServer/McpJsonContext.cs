using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ClearMeasure.Bootcamp.McpServer.Resources;
using ClearMeasure.Bootcamp.McpServer.Tools;
using ModelContextProtocol;

namespace ClearMeasure.Bootcamp.McpServer;

/// <summary>
/// Source-generated JSON for every payload the MCP tools and resources write, indented with the C# property names.
/// Generated metadata replaces runtime reflection, which Native AOT cannot rely on.
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(EmployeeJson))]
[JsonSerializable(typeof(EmployeeJson[]))]
[JsonSerializable(typeof(TeamJson[]))]
[JsonSerializable(typeof(VendorJson[]))]
[JsonSerializable(typeof(WorkOrderSummaryJson[]))]
[JsonSerializable(typeof(WorkOrderDetailJson))]
[JsonSerializable(typeof(WorkOrderAttachmentJson[]))]
[JsonSerializable(typeof(WorkOrderStatusJson[]))]
[JsonSerializable(typeof(WorkOrderPriorityJson[]))]
[JsonSerializable(typeof(RoleJson[]))]
[JsonSerializable(typeof(StatusTransitionsJson))]
[JsonSerializable(typeof(string[]))]
internal sealed partial class McpJsonContext : JsonSerializerContext
{
    /// <summary>
    /// Options for <c>WithTools</c>: the MCP SDK's own contract types plus the tool parameter types above, so the
    /// SDK builds input schemas and binds arguments from generated metadata.
    /// </summary>
    public static JsonSerializerOptions ToolOptions { get; } = new(McpJsonUtilities.DefaultOptions)
    {
        TypeInfoResolver = JsonTypeInfoResolver.Combine(McpJsonUtilities.DefaultOptions.TypeInfoResolver, Default)
    };
}

/// <summary>
/// The create-team and create-vendor results, camel-cased as those tools have always answered.
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(CreatedJson))]
internal sealed partial class McpCamelCaseJsonContext : JsonSerializerContext;
