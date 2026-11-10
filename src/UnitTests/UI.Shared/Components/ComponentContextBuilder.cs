using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

/// <summary>
/// bUnit contexts for the component fixtures: the UI bus and the application bus every <c>AppComponentBase</c> injects.
/// </summary>
public static class ComponentContextBuilder
{
    public static BunitContext WithStubBuses(IBus? bus = null)
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(bus ?? new StubBus());
        return ctx;
    }
}
