namespace ClearMeasure.Bootcamp.AcceptanceTests.TestSupport;

/// <summary>Guards for acceptance tests that need the local server or a remote non-pool host.</summary>
public static class LocalAcceptanceHost
{
    /// <summary>Ignores the test when the suite is aimed at a remote host instead of the local process.</summary>
    public static void Require()
    {
        if (!ServerFixture.StartLocalServer)
        {
            Assert.Ignore("Requires the local acceptance host.");
        }
    }

    /// <summary>Ignores the test when the suite is aimed at the local process (pool-slot shape).</summary>
    public static void RequireRemoteNonPool()
    {
        if (ServerFixture.StartLocalServer)
        {
            Assert.Ignore(
                "Non-pool 404 is proven against a remote host (tdd/uat/prod) and by UnitTests TestSupportEndpointWebTests.");
        }
    }
}
