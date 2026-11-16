using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Client;

/// <summary>
/// The .NET 11 WebAssembly runtime rejects POSIX locale names ("en-US@posix", reported by Chromium under LANG=C),
/// so index.html starts Blazor itself and passes the language tag as the application culture.
/// </summary>
[TestFixture]
public class IndexHtmlBlazorStartScriptTests
{
    private string _markup = string.Empty;

    [SetUp]
    public void SetUp()
    {
        var indexHtml = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "UI", "Client", "wwwroot", "index.html"));

        File.Exists(indexHtml).ShouldBeTrue();
        _markup = File.ReadAllText(indexHtml);
    }

    [Test]
    public void IndexHtml_ShouldStartBlazorExplicitly()
    {
        _markup.ShouldContain("<script src=\"_framework/blazor.webassembly.js\" autostart=\"false\"></script>");
        _markup.ShouldContain("Blazor.start(options);");
    }

    [Test]
    public void IndexHtml_ShouldStripPosixModifierFromBrowserLanguage()
    {
        _markup.ShouldContain("language.split(/[@.]/)[0]");
        _markup.ShouldContain("options.applicationCulture");
    }

    [Test]
    public void IndexHtml_ShouldFallBackToEnUs_WhenLocaleIsPosixOrC()
    {
        _markup.ShouldContain("/^(|c|posix)$/i.test(tag) ? \"en-US\" : tag");
    }
}
