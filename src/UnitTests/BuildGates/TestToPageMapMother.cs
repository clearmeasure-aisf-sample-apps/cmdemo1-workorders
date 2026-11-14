namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>Builds small <see cref="TestToPageMap"/> instances for BuildGates tests.</summary>
internal static class TestToPageMapMother
{
    /// <summary>Login, landing, and Teams fixtures with a TeamCards component host.</summary>
    public static TestToPageMap LoginLandingAndTeams()
    {
        return new TestToPageMap
        {
            Tests =
            [
                LoginEntry(),
                LandingEntry(),
                TeamsEntry(),
            ],
            Components = [TeamCardsComponent()],
        };
    }

    private static TestToPageMapEntry LoginEntry()
    {
        return new TestToPageMapEntry
        {
            Class = "ClearMeasure.Bootcamp.AcceptanceTests.Authentication.LoginTests",
            Pages = ["src/UI.Shared/Pages/Login.razor", "src/UI.Shared/Pages/Index.razor"],
        };
    }

    private static TestToPageMapEntry LandingEntry()
    {
        return new TestToPageMapEntry
        {
            Class = "ClearMeasure.Bootcamp.AcceptanceTests.App.LandingPageTests",
            Pages = ["src/UI.Shared/Pages/Index.razor"],
        };
    }

    private static TestToPageMapEntry TeamsEntry()
    {
        return new TestToPageMapEntry
        {
            Class = "ClearMeasure.Bootcamp.AcceptanceTests.TeamsAndVendors.TeamsPageTests",
            Pages = ["src/UI.Shared/Pages/TeamsAndVendors.razor"],
        };
    }

    private static TestToPageComponentEntry TeamCardsComponent()
    {
        return new TestToPageComponentEntry
        {
            Path = "src/UI.Shared/Components/Teams/TeamCards.razor",
            Pages = ["src/UI.Shared/Pages/TeamsAndVendors.razor"],
        };
    }
}
