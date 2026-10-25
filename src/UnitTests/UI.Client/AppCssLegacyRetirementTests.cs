using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Client;

[TestFixture]
public class AppCssLegacyRetirementTests
{
    private string _appCss = string.Empty;
    private string _indexHtml = string.Empty;

    [SetUp]
    public void SetUp()
    {
        var root = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", ".."));
        var appCss = Path.Combine(root, "UI", "Client", "wwwroot", "css", "app.css");
        var indexHtml = Path.Combine(root, "UI", "Client", "wwwroot", "index.html");

        File.Exists(appCss).ShouldBeTrue();
        File.Exists(indexHtml).ShouldBeTrue();
        _appCss = File.ReadAllText(appCss);
        _indexHtml = File.ReadAllText(indexHtml);
    }

    [Test]
    public void AppCss_LegacyHexes_ShouldBeAbsent()
    {
        _appCss.IndexOf("#7b68ee", StringComparison.OrdinalIgnoreCase).ShouldBe(-1);
        _appCss.IndexOf("#4a90e2", StringComparison.OrdinalIgnoreCase).ShouldBe(-1);
        _appCss.IndexOf("#6a5acd", StringComparison.OrdinalIgnoreCase).ShouldBe(-1);
        _appCss.IndexOf("#4169e1", StringComparison.OrdinalIgnoreCase).ShouldBe(-1);
        _appCss.IndexOf("rgba(123, 104, 238", StringComparison.Ordinal).ShouldBe(-1);
    }

    [Test]
    public void AppCss_GlobalRules_ShouldUseDesignTokens()
    {
        var button = Rule(".btn-primary");
        button.ShouldContain("var(--gradient-primary)");
        button.ShouldContain("var(--color-on-primary)");

        var form = Rule(".form-control");
        form.ShouldContain("var(--radius-control)");
        form.ShouldNotContain("outline: none");
        Rule(".form-control:focus").ShouldNotContain("outline: none");

        var card = Rule(".card");
        card.ShouldContain("var(--color-surface)");
        card.ShouldContain("var(--radius-card)");
        Rule(".card-header").ShouldContain("var(--gradient-primary)");

        Rule(".alert-danger").ShouldContain("var(--color-danger-bg)");
        Rule(".alert-success").ShouldContain("var(--color-success-bg)");
        Rule(".alert-info").ShouldContain("var(--color-info-bg)");

        Rule(".loading-progress circle:last-child").ShouldContain("stroke: var(--color-primary)");
        Rule(".btn:focus, .btn:active:focus, .btn-link.nav-link:focus, .form-control:focus, .form-check-input:focus")
            .ShouldContain("var(--focus-ring)");

        _appCss.ShouldContain("--focus-ring-color: #4f46e5");
        _appCss.ShouldContain("--focus-ring-color: #a5b4fc");
    }

    [Test]
    public void IndexHtml_AppStylesheet_ShouldCacheBustThisChange()
    {
        _indexHtml.ShouldContain("css/app.css?v=716");
        _appCss.ShouldContain("--color-primary-fill: #4f46e5");
        _appCss.ShouldContain("--color-primary-fill-hover: #4338ca");
    }

    private string Rule(string selector)
    {
        var needle = selector + " {";
        var at = _appCss.IndexOf(needle, StringComparison.Ordinal);
        at.ShouldBeGreaterThanOrEqualTo(0);
        var end = _appCss.IndexOf('}', at);
        end.ShouldBeGreaterThan(at);
        return _appCss.Substring(at, end - at);
    }
}
