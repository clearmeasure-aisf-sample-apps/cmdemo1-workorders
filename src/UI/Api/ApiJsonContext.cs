using System.Text.Json;
using System.Text.Json.Serialization;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Api.Endpoints;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Source-generated JSON contracts for every API request and response body (web defaults), so the API serializes
/// without reflection and runs under Native AOT.
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(SimpleHealthResponse))]
[JsonSerializable(typeof(DetailedHealthReport))]
[JsonSerializable(typeof(DetailedHealthEtagFingerprint))]
[JsonSerializable(typeof(DiagnosticsResponse))]
[JsonSerializable(typeof(EchoResponse))]
[JsonSerializable(typeof(EnvironmentStatusResponse))]
[JsonSerializable(typeof(IReadOnlyDictionary<string, bool>))]
[JsonSerializable(typeof(Dictionary<string, bool>))]
[JsonSerializable(typeof(MetricsSummaryResponse))]
[JsonSerializable(typeof(VersionMetadataResponse))]
[JsonSerializable(typeof(WeatherForecast[]))]
[JsonSerializable(typeof(WorkOrderBulkImportResponse))]
[JsonSerializable(typeof(Dictionary<string, int>))]
[JsonSerializable(typeof(TimestampConverterResponse))]
[JsonSerializable(typeof(Base64Request))]
[JsonSerializable(typeof(Base64Response))]
[JsonSerializable(typeof(CaseRequest))]
[JsonSerializable(typeof(CaseResponse))]
[JsonSerializable(typeof(DueDateCheckResponse))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(HashRequest))]
[JsonSerializable(typeof(HashResponse))]
[JsonSerializable(typeof(SlugifyRequest))]
[JsonSerializable(typeof(SlugifyResponse))]
[JsonSerializable(typeof(UppercaseRequest))]
[JsonSerializable(typeof(UppercaseResponse))]
[JsonSerializable(typeof(WordCountRequest))]
[JsonSerializable(typeof(WordCountResponse))]
[JsonSerializable(typeof(WorkOrderStatusDto[]))]
// Values health checks put in their data dictionaries.
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(DateTimeOffset))]
[JsonSerializable(typeof(TimeSpan))]
public sealed partial class ApiJsonContext : JsonSerializerContext;
