using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderSearch;

public class WorkOrderSearchPageBuilderTests
{
    [Test]
    public void Create_Build_RegistersDefaultBusAndAuth()
    {
        using var ctx = WorkOrderSearchPageBuilder.Create().Build();

        ctx.Services.GetRequiredService<IBus>().ShouldBeOfType<StubBus>();
        ctx.Services.GetRequiredService<AuthenticationStateProvider>().ShouldNotBeNull();
        ctx.Services.GetRequiredService<IFeatureFlags>().ShouldBeOfType<StubFeatureFlags>();
        ctx.Services.GetRequiredService<WorkOrderSearchState>().ShouldNotBeNull();
        WorkOrderSearchPageBuilder.DefaultUserName.ShouldBe("jpalermo");
    }

    [Test]
    public void WithBus_UsesProvidedBus()
    {
        var bus = new StubBus([]);
        using var ctx = WorkOrderSearchPageBuilder.Create().WithBus(bus).Build();

        ReferenceEquals(ctx.Services.GetRequiredService<IBus>(), bus).ShouldBeTrue();
    }

    [Test]
    public void WithOrders_SeedsStubBusResults()
    {
        var order = WorkOrderBuilder.Create().WithNumber("WO-X").WithStatus(WorkOrderStatus.Draft).Build();
        using var ctx = WorkOrderSearchPageBuilder.Create().WithOrders(order).Build();

        ctx.Services.GetRequiredService<IBus>().ShouldBeOfType<StubBus>();
    }

    [Test]
    public void WithCurrentUser_AndSignedInAs_AndSearchState_AndFlags_AreRegistered()
    {
        var user = EmployeeBuilder.Create("alice").WithFirstName("Alice").WithLastName("A").Build();
        var state = new WorkOrderSearchState { AssignedToMe = true };
        var flags = new StubFeatureFlags(showPriorityBadges: false);

        using var ctx = WorkOrderSearchPageBuilder.Create()
            .SignedInAs("alice")
            .WithCurrentUser(user)
            .WithSearchState(state)
            .WithFeatureFlags(flags)
            .Build();

        ReferenceEquals(ctx.Services.GetRequiredService<WorkOrderSearchState>(), state).ShouldBeTrue();
        ReferenceEquals(ctx.Services.GetRequiredService<IFeatureFlags>(), flags).ShouldBeTrue();
    }
}
