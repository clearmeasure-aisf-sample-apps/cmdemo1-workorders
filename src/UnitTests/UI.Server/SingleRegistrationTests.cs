using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.Events;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.McpServer;
using ClearMeasure.Bootcamp.UI.Server;
using FluentValidation;
using Lamar;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

/// <summary>
/// Each handler and validator is registered once per host, so each runs once per message. The generated mediator owns
/// handler registration; host scanning must not connect handlers again (the old Lamar McpServer registry registered
/// validators and handlers twice, so validators ran twice per request and notification handlers twice per publish).
/// </summary>
[TestFixture]
public class SingleRegistrationTests
{
    [Test]
    public void ShouldRegisterEachHandlerAndValidatorOnce_WhenUiServiceRegistryIsBuilt()
    {
        using var container = new Container(registry =>
        {
            registry.For<TimeProvider>().Use(TimeProvider.System);
            registry.IncludeRegistry<UiServiceRegistry>();
        });

        Registrations(container, typeof(INotificationHandler<UserLoggedInEvent>)).ShouldBe(1);
        Registrations(container, typeof(INotificationHandler<DatedWorkOrdersCreatedEvent>)).ShouldBe(1);
        Registrations(container, typeof(IRequestHandler<VendorsQuery, Vendor[]>)).ShouldBe(1);
        Registrations(container, typeof(IRequestHandler<SaveDraftCommand, StateCommandResult>)).ShouldBe(1);
        Registrations(container, typeof(IValidator<SaveDraftCommand>)).ShouldBe(1);
    }

    [Test]
    public void ShouldRegisterEachHandlerAndValidatorOnce_WhenMcpServerServicesAreAdded()
    {
        var services = new ServiceCollection().AddMcpServerServices();

        Registrations(services, typeof(INotificationHandler<UserLoggedInEvent>)).ShouldBe(1);
        Registrations(services, typeof(INotificationHandler<DatedWorkOrdersCreatedEvent>)).ShouldBe(1);
        Registrations(services, typeof(IRequestHandler<VendorsQuery, Vendor[]>)).ShouldBe(1);
        Registrations(services, typeof(IRequestHandler<SaveDraftCommand, StateCommandResult>)).ShouldBe(1);
        Registrations(services, typeof(IValidator<SaveDraftCommand>)).ShouldBe(1);
    }

    private static int Registrations(Container container, Type serviceType) =>
        container.Model.For(serviceType).Instances.Count();

    private static int Registrations(IServiceCollection services, Type serviceType) =>
        services.Count(descriptor => descriptor.ServiceType == serviceType);
}