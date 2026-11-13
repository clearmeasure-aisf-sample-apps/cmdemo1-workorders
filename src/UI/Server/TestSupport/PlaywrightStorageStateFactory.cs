namespace ClearMeasure.Bootcamp.UI.Server.TestSupport;

/// <summary>Builds Playwright storage-state payloads for test-only sign-in.</summary>
public static class PlaywrightStorageStateFactory
{
    /// <summary>Storage state that signs in as <paramref name="userName"/> at <paramref name="origin"/>.</summary>
    public static PlaywrightStorageState ForUser(string userName, string origin) =>
        new(
            Cookies: [],
            Origins:
            [
                new PlaywrightOrigin(
                    Origin: origin,
                    LocalStorage:
                    [
                        new PlaywrightLocalStorageItem(
                            Name: TestSupportConstants.UserSessionStorageKey,
                            Value: userName)
                    ])
            ]);
}
