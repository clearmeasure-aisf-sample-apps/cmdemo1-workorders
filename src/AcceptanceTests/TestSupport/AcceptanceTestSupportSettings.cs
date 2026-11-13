namespace ClearMeasure.Bootcamp.AcceptanceTests.TestSupport;

/// <summary>Secret and helpers for calling test-support from the acceptance suite.</summary>
public static class AcceptanceTestSupportSettings
{
    /// <summary>
    /// Shared secret written into the local acceptance server env and sent as
    /// <c>X-Test-Support-Secret</c>. Not a production credential.
    /// </summary>
    public const string Secret = "acceptance-test-support-secret";

    /// <summary>Builds a client aimed at the running application under test.</summary>
    public static TestSupportClient CreateClient() =>
        new(ServerFixture.ApplicationBaseUrl, Secret);
}
