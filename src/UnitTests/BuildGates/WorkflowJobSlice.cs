namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>One job block cut from a GitHub Actions workflow YAML text.</summary>
internal sealed class WorkflowJobSlice
{
    // ReSharper disable once ConvertToPrimaryConstructor -- epic guardrail: no mass primary-constructor conversion
    public WorkflowJobSlice(string id, string text)
    {
        Id = id;
        Text = text;
    }

    /// <summary>Job id (the YAML key under <c>jobs:</c>).</summary>
    public string Id { get; }

    /// <summary>Full job YAML including the job header line.</summary>
    public string Text { get; }

    /// <summary>Whether the job declares <c>id-token: write</c>.</summary>
    public bool HasIdTokenWrite =>
        Text.Contains("id-token: write", StringComparison.Ordinal);
}
