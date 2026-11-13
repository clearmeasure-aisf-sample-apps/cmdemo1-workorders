namespace ClearMeasure.Bootcamp.UI.Server.TestSupport;

/// <summary>
/// Pool-slot-only settings for the test-support HTTP surface. Off by default so tdd, uat and prod return 404.
/// </summary>
public sealed class TestSupportOptions
{
    /// <summary>Configuration section name: <c>TestSupport</c> (<c>TestSupport__Enabled</c>).</summary>
    public const string SectionName = "TestSupport";

    /// <summary>When false, every <c>/api/test-support</c> request answers 404.</summary>
    public bool Enabled { get; set; }

    /// <summary>Shared secret compared to the <see cref="TestSupportConstants.SecretHeaderName"/> header.</summary>
    public string Secret { get; set; } = string.Empty;
}
