using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared;
using FluentValidation;
using MediatR;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class BusTests
{
    [Test]
    public void Should_SendRequest_WithGenericResponse_CallsMediator()
    {
        var stubMediator = new StubMediator();
        var expectedResponse = "Test";
        stubMediator.SetResponse(expectedResponse);
        var bus = new Bus(stubMediator);
        var request = new TestQuery();

        var result = bus.Send(request).Result;

        result.ShouldBe(expectedResponse);
        stubMediator.LastRequest.ShouldBe(request);
    }

    [Test]
    public void Should_SendObjectRequest_CallsMediator()
    {
        var stubMediator = new StubMediator();
        var expectedResponse = "test response";
        stubMediator.SetObjectResponse(expectedResponse);
        var bus = new Bus(stubMediator);
        var request = new TestCommand();

        var result = bus.Send(request).Result;

        result.ShouldBe(expectedResponse);
        stubMediator.LastObjectRequest.ShouldBe(request);
    }

    [Test]
    public void Should_SendObjectRequest_WithNullResponse_CallsMediator()
    {
        var stubMediator = new StubMediator();
        stubMediator.SetObjectResponse(null);
        var bus = new Bus(stubMediator);
        var request = new TestCommand();

        var result = bus.Send(request).Result;

        result.ShouldBeNull();
        stubMediator.LastObjectRequest.ShouldBe(request);
    }

    [Test]
    public async Task Should_PublishNotification_CallsMediator()
    {
        var stubMediator = new StubMediator();
        var bus = new Bus(stubMediator);
        var notification = new TestNotification { Message = "test message" };

        await bus.Publish(notification);

        stubMediator.LastNotification.ShouldBe(notification);
        ((TestNotification)stubMediator.LastNotification!).Message.ShouldBe("test message");
    }

    [Test]
    public async Task ShouldThrowValidationException_WhenSaveDraftTitleExceedsMaxLength()
    {
        var stubMediator = new StubMediator();
        var bus = new Bus(stubMediator);
        var command = CreateSaveDraftCommand(WorkOrder.TitleMaxLength + 1);

        var ex = await Should.ThrowAsync<ValidationException>(() => bus.Send(command));

        ex.Errors.ShouldContain(failure =>
            failure.PropertyName == "WorkOrder.Title" &&
            failure.ErrorMessage == "Title cannot exceed 300 characters.");
        stubMediator.LastRequest.ShouldBeNull();
    }

    [Test]
    public async Task ShouldSendSaveDraft_WhenTitleIsAtMaxLength()
    {
        var stubMediator = new StubMediator();
        var command = CreateSaveDraftCommand(WorkOrder.TitleMaxLength);
        var expected = new StateCommandResult(command.WorkOrder);
        stubMediator.SetResponse(expected);
        var bus = new Bus(stubMediator);

        var result = await bus.Send(command);

        result.ShouldBe(expected);
        stubMediator.LastRequest.ShouldBe(command);
    }

    [Test]
    public async Task ShouldThrowValidationException_WhenSaveDraftSentAsObjectWithLongTitle()
    {
        var stubMediator = new StubMediator();
        var bus = new Bus(stubMediator);
        object command = CreateSaveDraftCommand(WorkOrder.TitleMaxLength + 1);

        await Should.ThrowAsync<ValidationException>(() => bus.Send(command));

        stubMediator.LastObjectRequest.ShouldBeNull();
    }

    [Test]
    public async Task ShouldThrowValidationException_WhenSaveDraftBuildingExceedsMaxLength()
    {
        var stubMediator = new StubMediator();
        var bus = new Bus(stubMediator);
        var creator = new Employee("creator1", "Creator", "One", "creator1@example.com");
        var workOrder = new WorkOrder
        {
            Title = "T",
            Building = new string('B', WorkOrder.BuildingMaxLength + 1),
            Creator = creator
        };
        var command = new SaveDraftCommand(workOrder, creator);

        var ex = await Should.ThrowAsync<ValidationException>(() => bus.Send(command));

        ex.Errors.ShouldContain(failure =>
            failure.PropertyName == "WorkOrder.Building" &&
            failure.ErrorMessage == "Building cannot exceed 100 characters.");
        stubMediator.LastRequest.ShouldBeNull();
    }

    [Test]
    public async Task ShouldThrowValidationException_WhenShelveReasonIsEmpty()
    {
        var stubMediator = new StubMediator();
        var bus = new Bus(stubMediator);
        var command = ShelveCommand("  ");

        var ex = await Should.ThrowAsync<ValidationException>(() => bus.Send(command));

        ex.Errors.ShouldContain(failure => failure.PropertyName == "Reason");
        stubMediator.LastRequest.ShouldBeNull();
    }

    [Test]
    public async Task ShouldThrowValidationException_WhenShelveReasonExceedsMaxLength()
    {
        var stubMediator = new StubMediator();
        var bus = new Bus(stubMediator);
        var command = ShelveCommand(new string('r', InProgressToAssignedCommand.ReasonMaxLength + 1));

        await Should.ThrowAsync<ValidationException>(() => bus.Send(command));

        stubMediator.LastRequest.ShouldBeNull();
    }

    [Test]
    public async Task ShouldSendShelve_WhenReasonIsAtMaxLength()
    {
        var stubMediator = new StubMediator();
        var command = ShelveCommand(new string('r', InProgressToAssignedCommand.ReasonMaxLength));
        var expected = new StateCommandResult(command.WorkOrder);
        stubMediator.SetResponse(expected);
        var bus = new Bus(stubMediator);

        var result = await bus.Send(command);

        result.ShouldBe(expected);
        stubMediator.LastRequest.ShouldBe(command);
    }

    private static InProgressToAssignedCommand ShelveCommand(string? reason)
    {
        var employee = new Employee();
        var order = new WorkOrder { Status = WorkOrderStatus.InProgress, Assignee = employee };
        return new InProgressToAssignedCommand(order, employee) { Reason = reason };
    }

    private static SaveDraftCommand CreateSaveDraftCommand(int titleLength)
    {
        var creator = new Employee("creator1", "Creator", "One", "creator1@example.com");
        var workOrder = new WorkOrder { Title = new string('T', titleLength), Creator = creator };
        return new SaveDraftCommand(workOrder, creator);
    }

    private class StubMediator : IMediator
    {
        private object? _response;
        private object? _objectResponse;

        public object? LastRequest { get; private set; }
        public object? LastObjectRequest { get; private set; }
        public object? LastNotification { get; private set; }

        public void SetResponse<T>(T response)
        {
            _response = response;
        }

        public void SetObjectResponse(object? response)
        {
            _objectResponse = response;
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult((TResponse)_response!);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = new())
            where TRequest : IRequest
        {
            throw new NotImplementedException();
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            LastObjectRequest = request;
            return Task.FromResult(_objectResponse);
        }

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            LastNotification = notification;
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            LastNotification = notification;
            return Task.CompletedTask;
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private class TestCommand;

    private class TestNotification : INotification
    {
        public string Message { get; init; } = string.Empty;
    }

    private record TestQuery : IRequest<string>;
}
