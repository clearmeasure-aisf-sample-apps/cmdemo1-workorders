using ClearMeasure.Bootcamp.Core.FeatureFlags;
using ClearMeasure.Bootcamp.McpServer;
using ModelContextProtocol.Protocol;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer;

[TestFixture]
public class FeatureDisabledToolFilterTests
{
    [Test]
    public async Task Create_Should_ReturnToolErrorNamingTheFeature_When_BusRejectsTheRequest()
    {
        var handler = FeatureDisabledToolFilter.Create((_, _) =>
            throw new FeatureDisabledException(FeatureFlagNames.VendorManagement, "CreateVendorCommand"));

        var result = await handler(null!, CancellationToken.None);

        result.IsError.ShouldBe(true);
        string text = result.Content.ShouldHaveSingleItem().ShouldBeOfType<TextContentBlock>().Text;
        text.ShouldContain(FeatureFlagNames.VendorManagement);
        text.ShouldContain("CreateVendorCommand");
    }

    [Test]
    public async Task Create_Should_PassResultThrough_When_ToolSucceeds()
    {
        var expected = new CallToolResult { Content = [new TextContentBlock { Text = "ok" }] };
        var handler = FeatureDisabledToolFilter.Create((_, _) => ValueTask.FromResult(expected));

        var result = await handler(null!, CancellationToken.None);

        result.ShouldBeSameAs(expected);
    }

    [Test]
    public async Task Create_Should_LetOtherExceptionsPropagate_When_ToolFailsForAnotherReason()
    {
        var handler = FeatureDisabledToolFilter.Create((_, _) => throw new InvalidOperationException("boom"));

        await Should.ThrowAsync<InvalidOperationException>(async () => await handler(null!, CancellationToken.None));
    }
}
