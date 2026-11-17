using ClearMeasure.Bootcamp.UI.Shared.Components;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

/// <summary>A disconnected circuit makes the module unavailable instead of failing the component.</summary>
[TestFixture]
public class LazyJsModuleTests
{
    [Test]
    public async Task GetAsync_Should_ImportOnce_And_ReturnTheSameModule()
    {
        await using var module = new LazyJsModule(new DisconnectingJsRuntime(), "./js/clipboard.js");

        var first = await module.GetAsync();
        var second = await module.GetAsync();

        first.ShouldNotBeNull();
        second.ShouldBeSameAs(first);
    }

    [Test]
    public async Task GetAsync_Should_ReturnNull_When_TheCircuitDisconnectsDuringImport()
    {
        await using var module = new LazyJsModule(new DisconnectingJsRuntime(failImport: true), "./js/clipboard.js");

        (await module.GetAsync()).ShouldBeNull();
    }

    [Test]
    public async Task DisposeAsync_Should_NotThrow_When_TheCircuitDisconnectsDuringTeardown()
    {
        var module = new LazyJsModule(new DisconnectingJsRuntime(failDispose: true), "./js/clipboard.js");
        await module.GetAsync();

        await Should.NotThrowAsync(async () => await module.DisposeAsync("cleanup"));
    }
}
