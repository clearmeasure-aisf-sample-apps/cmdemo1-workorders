namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.FeatureFlags;

/// <summary>
/// Octopus Feature Toggles v4 evaluations responses (the body of GET api/feature-flags/evaluations/v4/), in the wire shape
/// of the Octopus OpenFeature specification fixtures.
/// </summary>
public static class OctopusTogglesMother
{
    /// <summary>The TDD environment: building and vendor management on, priority badges off.</summary>
    public const string TddManagementFlagsOn = """
        [
          { "slug": "building-management", "value": true, "reason": "The flag is enabled for this environment." },
          { "slug": "vendor-management", "value": true, "reason": "The flag is enabled for this environment." },
          { "slug": "show-priority-badges", "value": false, "reason": "The flag is disabled for this environment." }
        ]
        """;

    /// <summary>One toggle deferred to the client: on for the EU region only.</summary>
    public const string EuRegionOnly = """
        [
          { "slug": "eu-only", "evaluationKey": "evaluation-key",
            "rules": [ { "name": "EU", "conditions": [ { "type": "context-attribute-is-one-of", "key": "region", "values": [ "eu" ] } ] } ] },
          { "slug": "outside-eu", "evaluationKey": "evaluation-key",
            "rules": [ { "name": "Outside EU", "conditions": [ { "type": "context-attribute-is-not-one-of", "key": "region", "values": [ "eu" ] } ] } ] }
        ]
        """;

    /// <summary>A toggle on for everyone through a 100% rollout rule.</summary>
    public const string EveryoneByPercentage = """
        [
          { "slug": "everyone", "evaluationKey": "evaluation-key",
            "rules": [ { "name": "Everyone", "conditions": [ { "type": "percentage-by-context", "percentage": 100 } ] } ] }
        ]
        """;

    /// <summary>A toggle whose only condition type is newer than this provider, and an unknown field.</summary>
    public const string ConditionFromANewerServer = """
        [
          { "slug": "newer", "evaluationKey": "evaluation-key", "futureField": { "x": 1 },
            "rules": [ { "name": "Future", "conditions": [ { "type": "condition-from-the-future" } ] } ] }
        ]
        """;

    /// <summary>A toggle with a server value but no reason, which Octopus never sends.</summary>
    public const string ValueWithoutReason = """
        [ { "slug": "malformed", "value": true } ]
        """;
}
