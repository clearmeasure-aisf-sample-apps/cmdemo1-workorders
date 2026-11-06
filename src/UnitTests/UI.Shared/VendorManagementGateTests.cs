using System.Reflection;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.FeatureFlags;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class VendorManagementGateTests
{
    [Test]
    public async Task Send_Should_DenyVendorDirectoryAndStillRunTeams_When_VendorManagementIsOff()
    {
        var vendors = new CountingHandler<VendorsQuery, Vendor[]>(_ => []);
        var createVendor = new CountingHandler<CreateVendorCommand, CreateVendorResult>(_ =>
            new CreateVendorResult(true, "created", Guid.NewGuid()));
        var teams = new CountingHandler<TeamsQuery, Team[]>(_ => []);
        var createTeam = new CountingHandler<CreateTeamCommand, CreateTeamResult>(_ =>
            new CreateTeamResult(true, "created", Guid.NewGuid()));
        await using var provider = BuildPipeline(vendorManagement: false, vendors, createVendor, teams, createTeam);
        var bus = new Bus(provider.GetRequiredService<IMediator>());

        var list = await Should.ThrowAsync<FeatureDisabledException>(() => bus.Send(new VendorsQuery()));
        list.FeatureName.ShouldBe(FeatureFlagNames.VendorManagement);
        list.RequestName.ShouldBe(nameof(VendorsQuery));

        var create = await Should.ThrowAsync<FeatureDisabledException>(() =>
            bus.Send(new CreateVendorCommand("Acme", "HVAC", null, null, null)));
        create.RequestName.ShouldBe(nameof(CreateVendorCommand));

        (await bus.Send(new TeamsQuery())).ShouldBeEmpty();
        (await bus.Send(new CreateTeamCommand("Crew", null, []))).Succeeded.ShouldBeTrue();

        vendors.Calls.ShouldBe(0);
        createVendor.Calls.ShouldBe(0);
        teams.Calls.ShouldBe(1);
        createTeam.Calls.ShouldBe(1);
    }

    [Test]
    public async Task Send_Should_RunVendorDirectory_When_VendorManagementIsOn()
    {
        var vendors = new CountingHandler<VendorsQuery, Vendor[]>(_ => []);
        var createVendor = new CountingHandler<CreateVendorCommand, CreateVendorResult>(_ =>
            new CreateVendorResult(true, "created", Guid.NewGuid()));
        await using var provider = BuildPipeline(vendorManagement: true, vendors, createVendor,
            new CountingHandler<TeamsQuery, Team[]>(_ => []),
            new CountingHandler<CreateTeamCommand, CreateTeamResult>(_ =>
                new CreateTeamResult(true, "created", Guid.NewGuid())));
        var bus = new Bus(provider.GetRequiredService<IMediator>());

        (await bus.Send(new VendorsQuery())).ShouldBeEmpty();
        (await bus.Send(new CreateVendorCommand("Acme", "HVAC", null, null, null))).Succeeded.ShouldBeTrue();
        vendors.Calls.ShouldBe(1);
        createVendor.Calls.ShouldBe(1);
    }

    [TestCase(typeof(TeamsQuery))]
    [TestCase(typeof(CreateTeamCommand))]
    [TestCase(typeof(InProgressToWaitingOnVendorCommand))]
    [TestCase(typeof(WaitingOnVendorToInProgressCommand))]
    [TestCase(typeof(WaitingOnVendorToCancelledCommand))]
    public void Request_Should_NotRequireVendorManagement(Type requestType)
    {
        requestType.GetCustomAttributes<RequiresFeatureAttribute>(inherit: true).ShouldBeEmpty();
    }

    private static ServiceProvider BuildPipeline(
        bool vendorManagement,
        CountingHandler<VendorsQuery, Vendor[]> vendors,
        CountingHandler<CreateVendorCommand, CreateVendorResult> createVendor,
        CountingHandler<TeamsQuery, Team[]> teams,
        CountingHandler<CreateTeamCommand, CreateTeamResult> createTeam)
    {
        var services = new ServiceCollection();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<IBus>();
            cfg.AddOpenBehavior(typeof(FeatureFlagBehavior<,>));
        });
        services.AddSingleton<IFeatureFlagEvaluator>(new StubFeatureFlagEvaluator(vendorManagement));
        services.AddSingleton(vendors);
        services.AddSingleton(createVendor);
        services.AddSingleton(teams);
        services.AddSingleton(createTeam);
        services.AddTransient<IRequestHandler<VendorsQuery, Vendor[]>>(_ => vendors);
        services.AddTransient<IRequestHandler<CreateVendorCommand, CreateVendorResult>>(_ => createVendor);
        services.AddTransient<IRequestHandler<TeamsQuery, Team[]>>(_ => teams);
        services.AddTransient<IRequestHandler<CreateTeamCommand, CreateTeamResult>>(_ => createTeam);
        return services.BuildServiceProvider();
    }

    private sealed class CountingHandler<TRequest, TResponse>(Func<TRequest, TResponse> respond)
        : IRequestHandler<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public int Calls { get; private set; }

        public Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(respond(request));
        }
    }

    private sealed class StubFeatureFlagEvaluator(bool vendorManagement) : IFeatureFlagEvaluator
    {
        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) =>
            Task.FromResult(featureName == FeatureFlagNames.VendorManagement && vendorManagement);
    }
}
