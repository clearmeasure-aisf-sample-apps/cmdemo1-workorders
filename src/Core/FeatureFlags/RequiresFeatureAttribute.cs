namespace ClearMeasure.Bootcamp.Core.FeatureFlags;

/// <summary>
/// Declares that a bus request only runs while the named feature flag is enabled. The bus feature flag
/// behavior rejects the request with <see cref="FeatureDisabledException"/> otherwise. Apply more than once to
/// require several flags.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class RequiresFeatureAttribute(string featureName) : Attribute
{
    /// <summary>The <see cref="FeatureFlagNames"/> entry that must be enabled.</summary>
    public string FeatureName { get; } = featureName;
}
