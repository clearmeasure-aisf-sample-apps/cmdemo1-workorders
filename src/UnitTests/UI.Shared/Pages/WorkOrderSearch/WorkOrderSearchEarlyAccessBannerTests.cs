using Bunit;
using Shouldly;
using SearchPage = ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderSearch;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderSearch;

/// <summary>
/// The early-access banner on the work order search page, shown only while the EarlyAccessBanner flag is on.
/// </summary>
[TestFixture]
public class WorkOrderSearchEarlyAccessBannerTests
{
    private const string Banner = ".early-access-banner";

    [Test]
    public async Task ShouldRenderEarlyAccessBanner_When_FlagIsOn()
    {
        await using var context = WorkOrderSearchPageBuilder.Create()
            .WithFeatureFlags(new StubFeatureFlags(earlyAccessBanner: true)).Build();

        var component = context.Render<SearchPage>();

        component.Find(Banner).TextContent
            .ShouldBe("Early access: you are seeing a feature that is not yet available to everyone.");
        component.Find(Banner).GetAttribute("role").ShouldBe("status");
    }

    [Test]
    public async Task ShouldNotRenderEarlyAccessBanner_When_FlagIsOff()
    {
        await using var context = WorkOrderSearchPageBuilder.Create()
            .WithFeatureFlags(new StubFeatureFlags(earlyAccessBanner: false)).Build();

        var component = context.Render<SearchPage>();

        component.FindAll(Banner).Count.ShouldBe(0);
    }
}
