using ClearMeasure.Bootcamp.UI.Server;
using FluentValidation;
using Lamar;
using Lamar.Diagnostics;
using MediatR;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

/// <summary>
/// Guards <see cref="UiServiceRegistry"/> against registering the same validator or
/// notification handler implementation more than once per service type. A duplicate
/// registration would make MediatR invoke the same handler twice per notification and
/// the validation pipeline run the same validator twice per request.
/// </summary>
[TestFixture]
public class UiServerSingleRegistrationGuardTests
{
    private static readonly Type[] ServiceTypeDefinitions =
    [
        typeof(IValidator<>),
        typeof(INotificationHandler<>),
    ];

    [Test]
    public void ShouldRegisterEachValidatorAndNotificationHandlerOnce_WhenUiServiceRegistryIsBuilt()
    {
        using var container = BuildUiContainer();

        var targetInstances = container.Model.AllInstances.Where(IsTargetServiceType).ToList();

        targetInstances.ShouldNotBeEmpty();

        var duplicates = targetInstances
            .GroupBy(instance => (instance.ServiceType, instance.ImplementationType))
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key.ServiceType} <- {group.Key.ImplementationType} registered {group.Count()} times")
            .ToList();

        duplicates.ShouldBeEmpty();
    }

    private static bool IsTargetServiceType(InstanceRef instance) =>
        instance.ServiceType.IsGenericType
        && ServiceTypeDefinitions.Contains(instance.ServiceType.GetGenericTypeDefinition());

    private static Container BuildUiContainer() =>
        new(registry =>
        {
            registry.For<TimeProvider>().Use(TimeProvider.System);
            registry.IncludeRegistry<UiServiceRegistry>();
        });
}
