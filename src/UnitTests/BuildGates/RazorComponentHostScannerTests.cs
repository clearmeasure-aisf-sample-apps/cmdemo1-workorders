using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class RazorComponentHostScannerTests
{
    [Test]
    public void Scan_WhenPageHeaderIsUsed_ListsPagesNotMainLayout()
    {
        var hosts = RazorComponentHostScanner.Scan(RepositoryCheckout.Current);

        hosts["src/UI.Shared/Components/PageHeader.razor"].ShouldBe(
        [
            "src/UI.Shared/Pages/ApplicationChat.razor",
            "src/UI.Shared/Pages/Index.razor",
            "src/UI.Shared/Pages/Reports.razor",
            "src/UI.Shared/Pages/Settings.razor",
            "src/UI.Shared/Pages/WorkOrderManage.razor",
            "src/UI.Shared/Pages/WorkOrderSearch.razor",
            "src/UI/Client/Pages/NotFound.razor",
        ]);
    }

    [Test]
    public void Scan_WhenNestedSearchChild_ResolvesToWorkOrderSearchPage()
    {
        var hosts = RazorComponentHostScanner.Scan(RepositoryCheckout.Current);

        hosts["src/UI.Shared/Components/Search/AssigneeAvatar.razor"]
            .ShouldBe(["src/UI.Shared/Pages/WorkOrderSearch.razor"]);
    }

    [Test]
    public void Scan_WhenKpiCardIsNested_IncludesIndexAndSearch()
    {
        var hosts = RazorComponentHostScanner.Scan(RepositoryCheckout.Current);

        hosts["src/UI.Shared/Components/KpiCard.razor"].ShouldBe(
        [
            "src/UI.Shared/Pages/Index.razor",
            "src/UI.Shared/Pages/WorkOrderSearch.razor",
        ]);
    }

    [Test]
    public void Scan_WhenRedirectToLoginIsUsed_ListsApp()
    {
        var hosts = RazorComponentHostScanner.Scan(RepositoryCheckout.Current);

        hosts["src/UI.Shared/Authentication/RedirectToLogin.razor"]
            .ShouldBe(["src/UI/Client/App.razor"]);
    }
}
