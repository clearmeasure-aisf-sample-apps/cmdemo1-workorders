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

        var link = cut.Find("footer.print-copyright a.print-copyright-link");
        link.GetAttribute("href")!.TrimEnd('/').ShouldBe("https://clearmeasure.com");
        link.GetAttribute("target").ShouldBe("_blank");
        var rel = link.GetAttribute("rel");
        rel.ShouldNotBeNull();
        rel.ShouldContain("noopener");
        rel.ShouldContain("noreferrer");
        link.TextContent.Trim().ShouldBe("ClearMeasure Labs");
    }
}
