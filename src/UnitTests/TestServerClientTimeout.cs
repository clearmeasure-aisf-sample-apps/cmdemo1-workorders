namespace ClearMeasure.Bootcamp.UnitTests;

/// <summary>
/// A swallowed server-side failure inside TestServer must fail the test with a timeout rather than hang forever.
/// </summary>
public static class TestServerClientTimeout
{
    public static readonly TimeSpan Value = TimeSpan.FromSeconds(60);
}
