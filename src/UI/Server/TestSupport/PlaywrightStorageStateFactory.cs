namespace ClearMeasure.Bootcamp.UI.Server.TestSupport;

/// <summary>Builds Playwright storage-state payloads for test-only sign-in.</summary>
public static class PlaywrightStorageStateFactory
{
    /// <summary>Storage state that signs in as <paramref name="userName"/> at <paramref name="origin"/>.</summary>
    public static PlaywrightStorageState ForUser(string userName, string origin)
    {
        var item = new PlaywrightLocalStorageItem
        {
            Name = TestSupportConstants.UserSessionStorageKey,
            Value = userName
        };
        var playwrightOrigin = new PlaywrightOrigin
        {
            Origin = origin,
            LocalStorage = [item]
        };
        return new PlaywrightStorageState
        {
            Cookies = [],
            Origins = [playwrightOrigin]
        };
    }

    /// <summary>Serializes storage state while reading every property (keeps the JSON shape stable).</summary>
    public static object ToSerializable(PlaywrightStorageState state) =>
        new
        {
            cookies = state.Cookies,
            origins = state.Origins.Select(origin => new
            {
                origin = origin.Origin,
                localStorage = origin.LocalStorage.Select(item => new
                {
                    name = item.Name,
                    value = item.Value
                })
            })
        };
}
