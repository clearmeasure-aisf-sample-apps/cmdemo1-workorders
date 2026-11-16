using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class RegisteredServicesTests
{
    [Test]
    public void Describe_Should_ListLifetimeServiceAndProvider_SortedOrdinally()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddScoped<IFormatProvider, System.Globalization.CultureInfo>();
        services.AddKeyedTransient<IComparable, Version>("v");

        var lines = new RegisteredServices(services).Describe().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        lines.ShouldBe([
            "Scoped    System.IFormatProvider -> System.Globalization.CultureInfo",
            "Singleton System.TimeProvider -> instance of System.TimeProvider+SystemTimeProvider",
            "Transient System.IComparable [v] -> System.Version"
        ]);
    }
}
