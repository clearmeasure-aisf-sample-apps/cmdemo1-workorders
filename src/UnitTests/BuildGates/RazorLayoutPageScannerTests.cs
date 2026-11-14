using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class RazorLayoutPageScannerTests
{
    [Test]
    public void PagesByLayout_WhenDefaultIsMainLayout_IncludesIndexAndExcludesPrint()
    {
        var pages = RazorLayoutPageScanner.PagesByLayout(RepositoryCheckout.Current);

        pages["src/UI.Shared/MainLayout.razor"].ShouldContain("src/UI.Shared/Pages/Index.razor");
        pages["src/UI.Shared/MainLayout.razor"].ShouldContain("src/UI.Shared/Pages/WorkOrderSearch.razor");
        pages["src/UI.Shared/MainLayout.razor"].ShouldNotContain("src/UI.Shared/Pages/WorkOrderPrint.razor");
        pages["src/UI.Shared/PrintLayout.razor"].ShouldBe(["src/UI.Shared/Pages/WorkOrderPrint.razor"]);
    }

    [Test]
    public void ShellPageExpansion_WhenNavMenuIsInMainLayout_ListsMainLayoutPages()
    {
        var expansion = RazorLayoutPageScanner.ShellPageExpansion(RepositoryCheckout.Current);

        expansion["src/UI.Shared/NavMenu.razor"].ShouldContain("src/UI.Shared/Pages/Index.razor");
        expansion["src/UI.Shared/NavMenu.razor"].ShouldContain("src/UI.Shared/Pages/WorkOrderSearch.razor");
        expansion["src/UI.Shared/MainLayout.razor"].ShouldContain("src/UI.Shared/Pages/Index.razor");
        expansion["src/UI/Client/App.razor"].ShouldContain("src/UI.Shared/Pages/WorkOrderSearch.razor");
    }
}
