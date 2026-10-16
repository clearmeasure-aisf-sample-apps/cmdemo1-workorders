using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.UnitTests.Core.Queries;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

public class StateCommandHandlerForSaveTests : IntegratedTestBase
{
    [Test]
    public async Task ShouldSaveWorkOrderBySavingDraft()
    {
        var currentUser = Faker<Employee>();
        currentUser.Id = Guid.NewGuid();
        var context = TestHost.GetRequiredService<DbContext>();
        context.Add(currentUser);
        await context.SaveChangesAsync();

        var workOrder = Faker<WorkOrder>();
        workOrder.Id = Guid.Empty;
        workOrder.CreatedDate = null; // Ensure CreatedDate is null to test setting it;
        workOrder.Creator = currentUser;
        workOrder.Instructions = "Turn off water main first";

        var command = RemotableRequestTests.SimulateRemoteObject(new SaveDraftCommand(workOrder, currentUser));
        var handler = TestHost.GetRequiredService<StateCommandHandler>();
        var result = await handler.Handle(command);

        result.TransitionVerbPresentTense.ShouldBe(command.TransitionVerbPresentTense);
        result.WorkOrder.Creator.ShouldBe(currentUser);
        result.WorkOrder.Title.ShouldBe(workOrder.Title);
        result.WorkOrder.CreatedDate.ShouldBe(TestHost.TestTime.DateTime);

        var context3 = TestHost.GetRequiredService<DbContext>();
        result.WorkOrder.Id.ShouldNotBe(Guid.Empty);
        var order = context3.Find<WorkOrder>(result.WorkOrder.Id) ?? throw new InvalidOperationException();
        order.CreatedDate.ShouldBe(TestHost.TestTime.DateTime);
        order.Title.ShouldBe(workOrder.Title);
        order.Instructions.ShouldBe("Turn off water main first");
    }

    [Test]
    public async Task ShouldSaveAndReadBackMaxLengthTitle()
    {
        var currentUser = Faker<Employee>();
        currentUser.Id = Guid.NewGuid();
        var context = TestHost.GetRequiredService<DbContext>();
        context.Add(currentUser);
        await context.SaveChangesAsync();

        var expectedTitle = new string('T', WorkOrder.TitleMaxLength);
        var workOrder = Faker<WorkOrder>();
        workOrder.Id = Guid.Empty;
        workOrder.Creator = currentUser;
        workOrder.Title = expectedTitle;

        var result = await TestHost.GetRequiredService<IBus>().Send(new SaveDraftCommand(workOrder, currentUser));

        var readContext = TestHost.GetRequiredService<DbContext>();
        var order = readContext.Find<WorkOrder>(result.WorkOrder.Id) ?? throw new InvalidOperationException();
        order.Title.ShouldBe(expectedTitle);
        order.Title!.Length.ShouldBe(WorkOrder.TitleMaxLength);
    }

    [Test]
    public async Task ShouldSaveAndReadBackMaxLengthBuilding()
    {
        var currentUser = Faker<Employee>();
        currentUser.Id = Guid.NewGuid();
        var context = TestHost.GetRequiredService<DbContext>();
        context.Add(currentUser);
        await context.SaveChangesAsync();

        var expectedBuilding = new string('B', WorkOrder.BuildingMaxLength);
        var workOrder = Faker<WorkOrder>();
        workOrder.Id = Guid.Empty;
        workOrder.Creator = currentUser;
        workOrder.Building = expectedBuilding;

        var result = await TestHost.GetRequiredService<IBus>().Send(new SaveDraftCommand(workOrder, currentUser));

        var readContext = TestHost.GetRequiredService<DbContext>();
        var order = readContext.Find<WorkOrder>(result.WorkOrder.Id) ?? throw new InvalidOperationException();
        order.Building.ShouldBe(expectedBuilding);
        order.Building!.Length.ShouldBe(WorkOrder.BuildingMaxLength);
    }

    [Test]
    public async Task ShouldSaveWorkOrderWithAssigneeAndCreator()
    {
        var workOrder = Faker<WorkOrder>();
        var currentUser = Faker<Employee>();
        workOrder.Creator = currentUser;
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(currentUser);
            context.Add(workOrder);
            await context.SaveChangesAsync();
        }

        Employee? assignee;
        await using (var context2 = TestHost.GetRequiredService<DbContext>())
        {
            assignee = context2.Find<Employee>(currentUser.Id);
        }

        workOrder.Creator = currentUser;
        workOrder.Assignee = assignee;

        var command = RemotableRequestTests.SimulateRemoteObject(new SaveDraftCommand(workOrder, currentUser));

        var handler = TestHost.GetRequiredService<StateCommandHandler>();

        _ = await handler.Handle(command);
        var context3 = TestHost.GetRequiredService<DbContext>();
        var order = context3.Find<WorkOrder>(workOrder.Id) ?? throw new InvalidOperationException();
        order.Title.ShouldBe(workOrder.Title);
        order.Description.ShouldBe(workOrder.Description);
        order.Creator.ShouldBe(currentUser);
        order.Assignee.ShouldBe(assignee);
    }

    [Test]
    public async Task ShouldUpdateWorkOrderWithAssigneeAndCreator()
    {
        var workOrder = Faker<WorkOrder>();
        var currentUser = Faker<Employee>();
        workOrder.Creator = currentUser;

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(currentUser);
            context.Add(workOrder);
            await context.SaveChangesAsync();
        }

        Employee? assignee;
        await using (var context2 = TestHost.GetRequiredService<DbContext>())
        {
            assignee = context2.Find<Employee>(currentUser.Id);
        }

        workOrder.Creator = currentUser;
        workOrder.Assignee = assignee;
        workOrder.Title = "newtitle";
        workOrder.Instructions = "Updated guidance after inspection.";

        var command = RemotableRequestTests.SimulateRemoteObject(new SaveDraftCommand(workOrder, currentUser));

        var handler = TestHost.GetRequiredService<StateCommandHandler>();

        _ = await handler.Handle(command);
        var context3 = TestHost.GetRequiredService<DbContext>();
        var order = context3.Find<WorkOrder>(workOrder.Id) ?? throw new InvalidOperationException();
        order.Title.ShouldBe("newtitle");
        order.Description.ShouldBe(workOrder.Description);
        order.Instructions.ShouldBe("Updated guidance after inspection.");
        order.Creator.ShouldBe(currentUser);
        order.Assignee.ShouldBe(assignee);
    }

    [Test]
    public async Task ShouldUpdateWorkOrderWithAssigneeAndCreatorWithRemotedOrder()
    {
        var workOrder = Faker<WorkOrder>();
        var currentUser = Faker<Employee>();
        workOrder.Creator = currentUser;

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(currentUser);
            context.Add(workOrder);
            await context.SaveChangesAsync();
        }

        Employee? assignee;
        await using (var context2 = TestHost.GetRequiredService<DbContext>())
        {
            assignee = context2.Find<Employee>(currentUser.Id);
        }

        workOrder.Creator = currentUser;
        workOrder.Assignee = assignee;
        workOrder.Title = "newtitle";

        var command = RemotableRequestTests.SimulateRemoteObject(new SaveDraftCommand(workOrder, currentUser));
        _ = RemotableRequestTests.SimulateRemoteObject(command);

        var handler = TestHost.GetRequiredService<StateCommandHandler>();

        _ = await handler.Handle(command);
        var context3 = TestHost.GetRequiredService<DbContext>();
        var order = context3.Find<WorkOrder>(workOrder.Id) ?? throw new InvalidOperationException();
        order.Title.ShouldBe("newtitle");
        order.Description.ShouldBe(workOrder.Description);
        order.Creator.ShouldBe(currentUser);
        order.Assignee.ShouldBe(assignee);
    }
    [Test]
    public async Task ShouldRejectSecondInsert_WhenNumberAlreadyExists()
    {
        var duplicateNumber = TestData.Number("P");

        var currentUser = Faker<Employee>();
        currentUser.Id = Guid.NewGuid();
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(currentUser);
            await context.SaveChangesAsync();
        }

        var first = Faker<WorkOrder>();
        first.Id = Guid.Empty;
        first.Number = duplicateNumber;
        first.Creator = currentUser;
        var second = Faker<WorkOrder>();
        second.Id = Guid.Empty;
        second.Number = duplicateNumber;
        second.Creator = currentUser;

        await TestHost.GetRequiredService<StateCommandHandler>()
            .Handle(RemotableRequestTests.SimulateRemoteObject(new SaveDraftCommand(first, currentUser)));
        var secondHandler = TestHost.GetRequiredService<StateCommandHandler>();
        var secondCommand = RemotableRequestTests.SimulateRemoteObject(new SaveDraftCommand(second, currentUser));

        var exception = await Should.ThrowAsync<InvalidOperationException>(() => secondHandler.Handle(secondCommand));

        exception.Message.ShouldContain(duplicateNumber);
        await using var verify = TestHost.GetRequiredService<DbContext>();
        (await verify.Set<WorkOrder>().CountAsync(w => w.Number == duplicateNumber)).ShouldBe(1);
    }
}