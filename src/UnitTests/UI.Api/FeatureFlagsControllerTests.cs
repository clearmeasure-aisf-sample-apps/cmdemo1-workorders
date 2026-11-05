using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class FeatureFlagsControllerTests
{
    [Test]
    public void Get_Should_ReturnAllCatalogEntries_When_Called()
    {
        var controller = ControllerWith(new ConfigurationBuilder().Build());

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var payload = JsonSerializer.Deserialize<Dictionary<string, bool>>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        foreach (var (name, enabled) in FeatureFlagsCatalog.All)
        {
            payload.ShouldContainKey(name);
            payload[name].ShouldBe(enabled);
        }

        payload.Count.ShouldBe(FeatureFlagsCatalog.All.Count);
    }

    [Test]
    public void All_Should_EnableShowPriorityBadges_When_CatalogRead()
    {
        FeatureFlagsCatalog.All["ShowPriorityBadges"].ShouldBeTrue();
        FeatureFlagsCatalog.All[FeatureFlagNames.ShowPriorityBadges].ShouldBeTrue();
        FeatureFlagsCatalog.All[FeatureFlagsCatalog.BuildingManagement].ShouldBeFalse();
        FeatureFlagsCatalog.All[FeatureFlagsCatalog.VendorManagement].ShouldBeFalse();
        FeatureFlagNames.BuildingManagement.ShouldBe(FeatureFlagsCatalog.BuildingManagement);
        FeatureFlagNames.VendorManagement.ShouldBe(FeatureFlagsCatalog.VendorManagement);
    }

    [Test]
    public void All_Should_BeNonNull_When_CatalogAccessed()
    {
        FeatureFlagsCatalog.All.ShouldNotBeNull();
        FeatureFlagsCatalog.All.Count.ShouldBeGreaterThan(0);
    }

    [Test]
    public void Get_Should_SerializeFlagKeysAsCatalogNames_When_UsingWebJsonDefaults()
    {
        var controller = ControllerWith(new ConfigurationBuilder().Build());

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        using var doc = JsonDocument.Parse(content.Content!);
        foreach (var key in FeatureFlagsCatalog.All.Keys)
        {
            doc.RootElement.TryGetProperty(key, out _).ShouldBeTrue($"expected key '{key}' in JSON");
        }
    }

    [Test]
    public void Get_Should_OverlayConfiguration_When_KnownFlagIsSet()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{DiagnosticsFeatureFlagsOptions.SectionName}:{FeatureFlagsCatalog.BuildingManagement}"] = "true",
            [$"{DiagnosticsFeatureFlagsOptions.SectionName}:{FeatureFlagsCatalog.VendorManagement}"] = "false",
            [$"{DiagnosticsFeatureFlagsOptions.SectionName}:NotAFlag"] = "true",
            [$"{DiagnosticsFeatureFlagsOptions.SectionName}:ShowPriorityBadges"] = "nope"
        }).Build();
        var controller = ControllerWith(configuration);

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = JsonSerializer.Deserialize<Dictionary<string, bool>>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload[FeatureFlagsCatalog.BuildingManagement].ShouldBeTrue();
        payload[FeatureFlagsCatalog.VendorManagement].ShouldBeFalse();
        payload["ShowPriorityBadges"].ShouldBeTrue();
        payload.ContainsKey("NotAFlag").ShouldBeFalse();
        payload.Count.ShouldBe(FeatureFlagsCatalog.All.Count);
    }

    private static FeatureFlagsController ControllerWith(IConfiguration configuration) =>
        new(new ConfigurationFeatureFlagRuntime(configuration))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
}
