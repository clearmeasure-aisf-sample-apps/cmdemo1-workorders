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

    [Test]
    public void ShouldStretchInFlowSidebar_AndKeepViewportOverlay()
    {
        var cssPath = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "UI.Shared", "MainLayout.razor.css"));
        var css = File.ReadAllText(cssPath);

        var baseRule = CssRuleBlock(css, ".modern-sidebar");
        baseRule.ShouldContain("min-height: 100vh");
        baseRule.Replace("min-height: 100vh", "", StringComparison.Ordinal).ShouldNotContain("height: 100vh");

        CssRuleBlock(css, ".modern-sidebar.rail-hidden").ShouldContain("height: 100vh");
        CssRuleBlock(css, "@media (max-width: 768px)").ShouldContain("height: 100vh");

        var iconRail = CssRuleBlock(css, "@media (min-width: 769px) and (max-width: 1023px)");
        iconRail.ShouldContain("width: 72px");
        iconRail.ShouldContain("margin-left: 0");
    }

    private static string CssRuleBlock(string css, string header)
    {
        var token = header + " {";
        var start = css.IndexOf(token, StringComparison.Ordinal);
        start.ShouldBeGreaterThanOrEqualTo(0);
        var depth = 0;
        var bodyStart = start + token.Length;
        for (var i = bodyStart - 1; i < css.Length; i++)
        {
            if (css[i] == '{')
            {
                depth++;
            }
            else if (css[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return css.Substring(bodyStart, i - bodyStart);
                }
            }
        }

        return "";
    }
}
