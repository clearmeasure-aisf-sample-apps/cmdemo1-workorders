using System.Globalization;
using Bunit;
using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class PrintLayoutTests
{
    [Test]
    public async Task ShouldRenderCopyrightFooter_WithCurrentYearAndClearMeasureLink()
    {
        await using var ctx = new BunitContext();

        var cut = ctx.Render<PrintLayout>();

        var footer = cut.Find("footer.print-copyright");
        var yearText = DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture);
        footer.TextContent.ShouldContain(yearText);
        footer.TextContent.ShouldContain("ClearMeasure Labs");
        footer.TextContent.ShouldContain("· Facilities");

        var link = cut.Find("footer.print-copyright a.print-copyright-link");
        link.GetAttribute("href")!.TrimEnd('/').ShouldBe("https://clearmeasure.com");
        link.GetAttribute("target").ShouldBe("_blank");
        var rel = link.GetAttribute("rel");
        rel.ShouldNotBeNull();
        rel.ShouldContain("noopener");
        rel.ShouldContain("noreferrer");
        var linkText = string.Join(' ', link.TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        linkText.ShouldContain("ClearMeasure Labs (opens in a new tab)");
        link.ClassList.ShouldContain("print-copyright-link");
        footer.ClassList.ShouldContain("print-copyright");
    }

    [Test]
    public async Task ShouldRenderCompanyLink_WithVisuallyHiddenNewTabText()
    {
        await using var ctx = new BunitContext();

        var cut = ctx.Render<PrintLayout>();

        var link = cut.Find("footer.print-copyright a.print-copyright-link");
        var linkText = string.Join(' ', link.TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        var labsIndex = linkText.IndexOf("ClearMeasure Labs", StringComparison.Ordinal);
        var hintIndex = linkText.IndexOf("(opens in a new tab)", StringComparison.Ordinal);
        hintIndex.ShouldBeGreaterThan(labsIndex);
        var hidden = link.QuerySelector(".visually-hidden");
        hidden.ShouldNotBeNull();
        hidden.TextContent.ShouldBe("(opens in a new tab)");
        hidden.ClassList.ShouldContain("visually-hidden");
    }
}
