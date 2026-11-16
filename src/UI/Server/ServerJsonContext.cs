using System.Text.Json;
using System.Text.Json.Serialization;
using ClearMeasure.Bootcamp.DataAccess.TestSupport;
using ClearMeasure.Bootcamp.UI.Server.LiveTelemetry;
using ClearMeasure.Bootcamp.UI.Server.Middleware;
using ClearMeasure.Bootcamp.UI.Server.Notifications;
using ClearMeasure.Bootcamp.UI.Server.Testing;
using ClearMeasure.Bootcamp.UI.Server.TestSupport;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Compile-time JSON contracts for the payloads UI.Server writes itself: health, telemetry, problem details, realtime
/// envelopes, version, and the test-only endpoints (request bodies included). Writers with their own options build a context over those options.
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(DetailedHealthCheckResponseWriter.DetailedHealthCheckResponse))]
[JsonSerializable(typeof(LiveTelemetrySnapshot))]
[JsonSerializable(typeof(ProblemDetailsWriter.ProblemDetailsDto))]
[JsonSerializable(typeof(RealtimeNotificationEnvelope))]
[JsonSerializable(typeof(ServerVersion))]
[JsonSerializable(typeof(TestingEndpoints.ConnectionCount))]
[JsonSerializable(typeof(TestingEndpoints.BodyReadTwice))]
[JsonSerializable(typeof(CreateEmployeeRequest))]
[JsonSerializable(typeof(CreateWorkOrderRequest))]
[JsonSerializable(typeof(TestSupportChatClientStatus))]
[JsonSerializable(typeof(TestSupportEmployeeDto))]
[JsonSerializable(typeof(TestSupportWorkOrderDto))]
[JsonSerializable(typeof(PlaywrightStorageState))]
internal sealed partial class ServerJsonContext : JsonSerializerContext;

/// <summary>The <c>/_version</c> payload: this server's informational version.</summary>
internal sealed record ServerVersion(string? Version);
