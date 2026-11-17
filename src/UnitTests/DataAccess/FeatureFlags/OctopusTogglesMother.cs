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

    /// <summary>One toggle per malformed shape the Octopus specification defines; each must resolve to a parse error.</summary>
    public const string MalformedToggles = """
        [
          { "slug": "value-and-rules", "value": true, "reason": "Enabled.", "evaluationKey": "k",
            "rules": [ { "name": "R", "conditions": [ { "type": "percentage-by-context", "percentage": 100 } ] } ] },
          { "slug": "neither-value-nor-rules" },
          { "slug": "rules-without-evaluation-key",
            "rules": [ { "name": "R", "conditions": [ { "type": "percentage-by-context", "percentage": 100 } ] } ] },
          { "slug": "no-rules", "evaluationKey": "k", "rules": [] },
          { "slug": "a-missing-rule", "evaluationKey": "k", "rules": [ null ] },
          { "slug": "rule-without-name", "evaluationKey": "k",
            "rules": [ { "conditions": [ { "type": "percentage-by-context", "percentage": 100 } ] } ] },
          { "slug": "rule-without-conditions", "evaluationKey": "k", "rules": [ { "name": "R", "conditions": [] } ] },
          { "slug": "rule-with-a-missing-condition", "evaluationKey": "k", "rules": [ { "name": "R", "conditions": [ null ] } ] },
          { "slug": "condition-without-type", "evaluationKey": "k",
            "rules": [ { "name": "R", "conditions": [ { "key": "region", "values": [ "eu" ] } ] } ] },
          { "slug": "condition-without-key", "evaluationKey": "k",
            "rules": [ { "name": "R", "conditions": [ { "type": "context-attribute-is-one-of", "values": [ "eu" ] } ] } ] },
          { "slug": "condition-without-values", "evaluationKey": "k",
            "rules": [ { "name": "R", "conditions": [ { "type": "context-attribute-is-one-of", "key": "region" } ] } ] },
          { "slug": "condition-with-a-missing-value", "evaluationKey": "k",
            "rules": [ { "name": "R", "conditions": [ { "type": "context-attribute-is-not-one-of", "key": "region", "values": [ null ] } ] } ] },
          { "slug": "rollout-without-percentage", "evaluationKey": "k",
            "rules": [ { "name": "R", "conditions": [ { "type": "percentage-by-context" } ] } ] },
          { "slug": "rollout-over-100", "evaluationKey": "k",
            "rules": [ { "name": "R", "conditions": [ { "type": "percentage-by-context", "percentage": 101 } ] } ] }
        ]
        """;
}
