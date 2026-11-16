using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class WhatDoIHaveControllerTests
{
    [Test]
    public void Services_Should_ListEachRegistrationWithLifetimeAndProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddTransient<IComparer<string>, OrdinalComparer>();
        var controller = new WhatDoIHaveController();

        var result = controller.Services(new RegisteredServices(services));

        result.ShouldContain("Singleton System.TimeProvider -> instance of");
        result.ShouldContain($"Transient System.Collections.Generic.IComparer`1[[System.String");
        result.ShouldContain($"-> {typeof(OrdinalComparer).FullName}");
    }

    private sealed class OrdinalComparer : IComparer<string>
    {
        public int Compare(string? x, string? y) => string.CompareOrdinal(x, y);
    }
}
