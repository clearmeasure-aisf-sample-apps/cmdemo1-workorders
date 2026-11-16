using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using IStateCommand = ClearMeasure.Bootcamp.Core.Services.IStateCommand;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderManage;

public class WorkOrderManagePageContextTests
{
    [Test]
    public void Build_RegistersBusSessionTranslationAndNewOrderSource()
    {
        var user = WorkOrderManageSteps.Viewer();
        var page = WorkOrderManagePageContext.Create().SignedInAs(user);

        using var ctx = page.Build();

        ctx.Services.GetRequiredService<IBus>().ShouldBeOfType<WorkOrderManageBus>();
        ctx.Services.GetRequiredService<IUserSession>().ShouldBe(page.Session);
        ctx.Services.GetRequiredService<ITranslationService>().ShouldBe(page.Translation);
        ctx.Services.GetRequiredService<IWorkOrderBuilder>().ShouldBe(page.NewOrder);
    }

    [Test]
    public void Editing_SeedsExistingWorkOrderOnBus()
    {
        var user = WorkOrderManageSteps.Creator();
        var order = WorkOrderBuilder.Create()
            .WithNumber("WO-EDIT")
            .WithStatus(WorkOrderStatus.Draft)
            .CreatedBy(user)
            .Build();
        var page = WorkOrderManagePageContext.Create().SignedInAs(user).Editing(order);

        using var ctx = page.Build();

        ReferenceEquals(page.Bus, ctx.Services.GetRequiredService<IBus>()).ShouldBeTrue();
    }

    [Test]
    public void WhenBusReceives_EnablesStateCommandCapture()
    {
        var user = WorkOrderManageSteps.Creator();
        var page = WorkOrderManagePageContext.Create()
            .SignedInAs(user)
            .WhenBusReceives<IStateCommand>();

        using var _ = page.Build();

        page.Bus.LastCommand.ShouldBeNull();
        page.Bus.CommandCount.ShouldBe(0);
    }

    [Test]
    public void WithNewOrder_ConfiguresSeed()
    {
        var user = WorkOrderManageSteps.Viewer();
        var id = Guid.NewGuid();
        var page = WorkOrderManagePageContext.Create()
            .SignedInAs(user)
            .WithNewOrder(o => o.WithId(id).WithNumber("WO-ROOM").WithTitle("Room field"));

        using var _ = page.Build();

        var created = page.NewOrder.CreateNewWorkOrder(user);
        created.Id.ShouldBe(id);
        created.Number.ShouldBe("WO-ROOM");
        created.Title.ShouldBe("Room field");
    }
}
