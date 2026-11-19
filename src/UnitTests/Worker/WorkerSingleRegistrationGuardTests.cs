using ClearMeasure.Bootcamp.Core;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Worker;

namespace ClearMeasure.Bootcamp.UnitTests.Worker;

/// <summary>
/// Guards the Worker's service registrations against registering the same validator or
/// notification handler implementation more than once per service type. Probes the
/// registered service descriptors only; the NServiceBus endpoint is not started, so no
/// SQL Server or transport infrastructure is required.
/// </summary>
[TestFixture]
public class WorkerSingleRegistrationGuardTests
{
    private static readonly Type[] ServiceTypeDefinitions =
    [
        typeof(IValidator<>),
        typeof(INotificationHandler<>),
    ];

    [Test]
    public void ShouldRegisterEachValidatorAndNotificationHandlerOnce_WhenWorkerServicesAreRegistered()
    {
        var services = new HarnessEndpoint(BuildConfiguration()).CreateWorkerServiceCollection();

        // The probe must cover the Worker's own registrations, not an empty collection.
        services.Any(descriptor => descriptor.ServiceType == typeof(IBus)).ShouldBeTrue();

        var targetDescriptors = services.Where(IsTargetServiceType).ToList();

        var duplicates = targetDescriptors
            .GroupBy(descriptor => (descriptor.ServiceType, descriptor.ImplementationType))
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key.ServiceType} <- {group.Key.ImplementationType} registered {group.Count()} times")
            .ToList();

        duplicates.ShouldBeEmpty();
    }

    private static bool IsTargetServiceType(ServiceDescriptor descriptor) =>
        descriptor.ServiceType.IsGenericType
        && ServiceTypeDefinitions.Contains(descriptor.ServiceType.GetGenericTypeDefinition());

    private static IConfiguration BuildConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlConnectionString"] = "Server=.;Database=WorkerRegistrationGuard;",
                ["RemotableBus:ApiUrl"] = "http://localhost:5000/api/bus",
            })
            .Build();

    private sealed class HarnessEndpoint(IConfiguration configuration) : WorkOrderEndpoint(configuration)
    {
        public IServiceCollection CreateWorkerServiceCollection()
        {
            var services = CreateServiceCollection();
            RegisterDependencyInjection(services);
            return services;
        }
    }
}
