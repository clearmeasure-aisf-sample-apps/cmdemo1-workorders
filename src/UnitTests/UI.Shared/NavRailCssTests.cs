using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class NavRailCssTests
{
    [Test]
    public void ShouldKeepNavRailBreakpointAlignedAcrossCssAndJs()
    {
        var breakpoint = MainLayout.NavRailBreakpointMediaQuery;
        breakpoint.ShouldBe("(max-width: 768px)");

        var cssPath = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "UI.Shared", "MainLayout.razor.css"));
        var jsPath = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "UI.Shared", "wwwroot", "js", "mainLayoutNav.js"));

        File.Exists(cssPath).ShouldBeTrue($"Expected scoped stylesheet at {cssPath}");
        File.Exists(jsPath).ShouldBeTrue($"Expected nav layout script at {jsPath}");

        File.ReadAllText(cssPath).ShouldContain($"@media {breakpoint}");
        File.ReadAllText(jsPath).ShouldContain("matchMedia(mediaQuery)");
        File.ReadAllText(jsPath).ShouldContain("MainLayout.NavRailBreakpointMediaQuery");
    }

    [Test]
    public void ShouldCollapseAppContainer_WhenWideViewportAndNavHidden()
    {
        NavRailCss.AppContainerClass(isNarrowViewport: false, navVisible: false)
            .ShouldBe("modern-app rail-collapsed");
    }

    [Test]
    public void ShouldUseOpenSidebarClass_WhenNarrowViewportAndNavVisible()
    {
        NavRailCss.SidebarClass(isNarrowViewport: true, navVisible: true)
            .ShouldBe("modern-sidebar open");
    }

    [Test]
    public void ShouldHideRailSidebar_WhenWideViewportAndNavHidden()
    {
        NavRailCss.SidebarClass(isNarrowViewport: false, navVisible: false)
            .ShouldBe("modern-sidebar rail-hidden");
    }

    [Test]
    public void ShouldKeepDefaultAppContainer_WhenNavVisibleOrNarrow()
    {
        NavRailCss.AppContainerClass(isNarrowViewport: false, navVisible: true).ShouldBe("modern-app");
        NavRailCss.AppContainerClass(isNarrowViewport: true, navVisible: true).ShouldBe("modern-app");
        NavRailCss.AppContainerClass(isNarrowViewport: true, navVisible: false).ShouldBe("modern-app");
    }

    [Test]
    public void ShouldKeepBaseSidebar_WhenNavVisibleOnWideOrHiddenOnNarrow()
    {
        NavRailCss.SidebarClass(isNarrowViewport: false, navVisible: true).ShouldBe("modern-sidebar");
        NavRailCss.SidebarClass(isNarrowViewport: true, navVisible: false).ShouldBe("modern-sidebar");
    }
}
