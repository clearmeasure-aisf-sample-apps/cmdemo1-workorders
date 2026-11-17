using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClearMeasure.Bootcamp.DataAccess.FeatureFlags;

/// <summary>
/// Source-generated serialization for the Octopus v4 evaluations response, with the web defaults (camelCase,
/// case-insensitive) Octopus.OpenFeature uses. Generated code needs no reflection, so it works under Native AOT.
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(OctopusFlagEvaluation[]))]
internal sealed partial class OctopusFlagsJsonContext : JsonSerializerContext;
