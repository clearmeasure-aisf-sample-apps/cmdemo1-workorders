using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Client;

/// <summary>
/// The .NET 11 WebAssembly runtime rejects POSIX locale names ("en-US@posix", reported by Chromium under LANG=C),
/// so index.html starts Blazor itself and passes the language tag as the application culture.
/// </summary>
[TestFixture]
public class IndexHtmlBlazorStartScriptTests
{
    private static string ReadIndexHtml()
    {
        var indexHtml = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "UI", "Client", "wwwroot", "index.html"));
        return File.ReadAllText(indexHtml);
    }

    [Test]
    public void IndexHtml_ShouldStartBlazorExplicitly()
    {
        var markup = ReadIndexHtml();

        markup.ShouldContain("<script src=\"_framework/blazor.webassembly.js\" autostart=\"false\"></script>");
        markup.ShouldContain("Blazor.start(options);");
    }

    [Test]
    public void IndexHtml_ShouldStripPosixModifierFromBrowserLanguage()
    {
        var markup = ReadIndexHtml();

        markup.ShouldContain("language.split(/[@.]/)[0]");
        markup.ShouldContain("options.applicationCulture");
    }

    [Test]
    public void IndexHtml_ShouldFallBackToEnUs_WhenLocaleIsPosixOrC()
    {
        var markup = ReadIndexHtml();

        markup.ShouldContain("/^(|c|posix)$/i.test(tag) ? \"en-US\" : tag");
    }
}
