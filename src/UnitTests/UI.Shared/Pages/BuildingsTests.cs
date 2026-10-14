using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using MediatR;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class BuildingsTests
{
    [Test]
    public async Task Buildings_OnInitialized_ShouldLoadBuildingsAndRooms()
    {
        var bus = new StubBusBuildings();
        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Buildings>());

        await component.WaitForAssertionAsync(() =>
            component.Markup.ShouldContain("Main Building"));
    }

    [Test]
    public async Task Buildings_WhenNoBuildingsExist_ShouldShowEmptyState()
    {
        var bus = new StubBusBuildings { BuildingsToReturn = Array.Empty<BuildingRoomData>() };
        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Buildings>());

        await component.WaitForAssertionAsync(() =>
            component.Markup.ShouldContain("No work orders found"));
    }

    [Test]
    public async Task Buildings_ShouldDisplayBuildingCards()
    {
        var bus = new StubBusBuildings();
        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Buildings>());

        await component.WaitForAssertionAsync(() =>
        {
            component.FindAll(".building-card").Count.ShouldBeGreaterThan(0);
            component.Markup.ShouldContain("Main Building");
            component.Markup.ShouldContain("Annex");
        });
    }

    [Test]
    public async Task Buildings_ShouldDisplayRoomsUnderBuildings()
    {
        var bus = new StubBusBuildings();
        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Buildings>());

        await component.WaitForAssertionAsync(() =>
        {
            var roomItems = component.FindAll(".room-item");
            roomItems.Count.ShouldBeGreaterThan(0);
            component.Markup.ShouldContain("Room 101");
        });
    }

    [Test]
    public async Task Buildings_ShouldDisplayWorkOrderCounts()
    {
        var bus = new StubBusBuildings();
        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Buildings>());

        await component.WaitForAssertionAsync(() =>
        {
            component.Markup.ShouldContain("Open");
            component.Markup.ShouldContain("Overdue");
            component.Markup.ShouldContain("Completed 30d");
        });
    }

    [Test]
    public async Task Buildings_CountLinks_NavigateToSearchWithCorrectFilters()
    {
        var bus = new StubBusBuildings();
        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Buildings>());

        await component.WaitForAssertionAsync(() =>
        {
            var openLink = component.Find("[data-testid='room-open-link-Main Building-Room 101']");
            var href = openLink.GetAttribute("href");
            href.ShouldNotBeNull();
            href.ShouldContain("workorder/search");
            href.ShouldContain("Building=Main%20Building");
            href.ShouldContain("Room=Room%20101");
            href.ShouldContain("OpenOnly=true");
        });
    }

    [Test]
    public async Task Buildings_OverdueCountLinks_IncludeOverdueFilter()
    {
        var bus = new StubBusBuildings();
        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Buildings>());

        await component.WaitForAssertionAsync(() =>
        {
            var overdueLink = component.Find("[data-testid='room-overdue-link-Main Building-Room 101']");
            var href = overdueLink.GetAttribute("href");
            href.ShouldNotBeNull();
            href.ShouldContain("OverdueOnly=true");
        });
    }

    [Test]
    public async Task Buildings_CompletedCountLinks_IncludeCompletedFilter()
    {
        var bus = new StubBusBuildings();
        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Buildings>());

        await component.WaitForAssertionAsync(() =>
        {
            var completedLink = component.Find("[data-testid='room-completed-link-Main Building-Room 101']");
            var href = completedLink.GetAttribute("href");
            href.ShouldNotBeNull();
            href.ShouldContain("Status=Complete");
            href.ShouldContain("CompletedWithinDays=30");
        });
    }

    [Test]
    public async Task Buildings_NoBuildingLabel_UsesSpecialParameter()
    {
        var bus = new StubBusBuildings();
        bus.BuildingsToReturn = new[]
        {
            new BuildingRoomData
            {
                BuildingName = "No building",
                Rooms = new[]
                {
                    new RoomData
                    {
                        RoomName = "No room",
                        OpenCount = 5,
                        OverdueCount = 2,
                        CompletedLast30DaysCount = 8
                    }
                }
            }
        };
        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Buildings>());

        await component.WaitForAssertionAsync(() =>
        {
            var link = component.Find("[data-testid='room-open-link-No building-No room']");
            link.GetAttribute("href").ShouldContain("NoBuilding=true");
            link.GetAttribute("href").ShouldContain("NoRoom=true");
        });
    }

    private static BunitContext CreateContext(StubBusBuildings? bus = null)
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IBus>(bus ?? new StubBusBuildings());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);

        var bunitAuth = ctx.AddAuthorization();
        bunitAuth.SetAuthorized("testuser");
        var customAuth = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        customAuth.Login("testuser").GetAwaiter().GetResult();
        ctx.Services.AddSingleton<AuthenticationStateProvider>(customAuth);
        ctx.Services.AddSingleton(customAuth);

        return ctx;
    }
}

/// <summary>
/// Stub bus for Buildings tests that returns predefined building and room data.
/// </summary>
public class StubBusBuildings : StubBus
{
    private bool _buildingsInitialized;

    public BuildingRoomData[] BuildingsToReturn
    {
        get
        {
            if (!_buildingsInitialized)
            {
                field =
                [
                    new BuildingRoomData
                    {
                        BuildingName = "Main Building",
                        Rooms =
                        [
                            new RoomData
                            {
                                RoomName = "Room 101",
                                OpenCount = 5,
                                OverdueCount = 2,
                                CompletedLast30DaysCount = 8
                            },
                            new RoomData
                            {
                                RoomName = "Room 102",
                                OpenCount = 3,
                                OverdueCount = 1,
                                CompletedLast30DaysCount = 6
                            }
                        ]
                    },
                    new BuildingRoomData
                    {
                        BuildingName = "Annex",
                        Rooms =
                        [
                            new RoomData
                            {
                                RoomName = "Room 201",
                                OpenCount = 2,
                                OverdueCount = 0,
                                CompletedLast30DaysCount = 4
                            }
                        ]
                    }
                ];
                _buildingsInitialized = true;
            }

            return field;
        }
        set
        {
            field = value;
            _buildingsInitialized = true;
        }
    } = [];

    public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        if (request is WorkOrderBuildingsAndRoomsQuery)
        {
            return Task.FromResult<TResponse>((TResponse)(object)BuildingsToReturn);
        }

        return base.Send(request);
    }
}
