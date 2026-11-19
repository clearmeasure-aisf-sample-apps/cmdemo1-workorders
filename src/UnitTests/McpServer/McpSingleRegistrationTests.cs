using ClearMeasure.Bootcamp.McpServer;
using FluentValidation;
using Lamar;
using Lamar.Diagnostics;
using MediatR;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer;

[TestFixture]
public class McpSingleRegistrationTests
{
    private static readonly Type[] ServiceTypeDefinitions =
    {
        typeof(IValidator<>),
        typeof(INotificationHandler<>),
        typeof(IRequestHandler<,>),
    };

    [Test]
    public void ShouldRegisterEachHandlerAndValidatorOnce_WhenMcpServiceRegistryIsBuilt()
    {
        using var container = BuildMcpContainer();

        var targetInstances = container.Model.AllInstances
            .Where(IsTargetServiceType)
            .ToList();

        targetInstances.ShouldNotBeEmpty();

        var duplicates = targetInstances
            .GroupBy(instance => (ServiceType: instance.ServiceType, ImplementationType: instance.ImplementationType))
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key.ServiceType} <- {group.Key.ImplementationType} registered {group.Count()} times")
            .ToList();

        duplicates.ShouldBeEmpty();
    }

    private static bool IsTargetServiceType(InstanceRef instance)
    {
        Type? serviceType = instance.ServiceType;
        return serviceType is not null
            && serviceType.IsGenericType
            && ServiceTypeDefinitions.Contains(serviceType.GetGenericTypeDefinition());
    }

    private static Container BuildMcpContainer() =>
        new(registry =>
        {
            registry.For<TimeProvider>().Use(TimeProvider.System);
            registry.IncludeRegistry<McpServiceRegistry>();
        });
}
