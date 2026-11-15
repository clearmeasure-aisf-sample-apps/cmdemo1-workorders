using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using ClearMeasure.Bootcamp.UI.Shared;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class ValidationBehaviorTests
{
    [Test]
    public async Task Handle_Should_ThrowValidationException_WhenSaveDraftTitleExceedsMaxLength()
    {
        var bus = BusWithValidators();
        var command = CreateSaveDraftCommand(WorkOrder.TitleMaxLength + 1);

        var ex = await Should.ThrowAsync<ValidationException>(() => bus.Send(command));

        ex.Errors.ShouldContain(failure =>
            failure.PropertyName == "WorkOrder.Title" &&
            failure.ErrorMessage == "Title cannot exceed 600 characters.");
    }

    [Test]
    public async Task Handle_Should_SendSaveDraft_WhenTitleIsAtMaxLength()
    {
        var command = CreateSaveDraftCommand(WorkOrder.TitleMaxLength);
        var expected = new StateCommandResult(command.WorkOrder);
        var bus = BusWithValidators(expected);

        var result = await bus.Send(command);

        result.ShouldBe(expected);
    }

    [Test]
    public async Task Handle_Should_ThrowValidationException_WhenSaveDraftBuildingExceedsMaxLength()
    {
        var bus = BusWithValidators();
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
    }

    [Test]
    public async Task Handle_Should_ThrowValidationException_WhenShelveReasonIsEmpty()
    {
        var bus = BusWithValidators();

        var ex = await Should.ThrowAsync<ValidationException>(() => bus.Send(ShelveCommand("  ")));

        ex.Errors.ShouldContain(failure => failure.PropertyName == "Reason");
    }

    [Test]
    public async Task Handle_Should_ThrowValidationException_WhenShelveReasonExceedsMaxLength()
    {
        var bus = BusWithValidators();
        var command = ShelveCommand(new string('r', InProgressToAssignedCommand.ReasonMaxLength + 1));

        await Should.ThrowAsync<ValidationException>(() => bus.Send(command));
    }

    [Test]
    public async Task Handle_Should_SendShelve_WhenReasonIsAtMaxLength()
    {
        var command = ShelveCommand(new string('r', InProgressToAssignedCommand.ReasonMaxLength));
        var expected = new StateCommandResult(command.WorkOrder);
        var bus = BusWithValidators(expected);

        var result = await bus.Send(command);

        result.ShouldBe(expected);
    }

    [Test]
    public async Task Handle_Should_CallNext_WhenNoValidatorIsRegistered()
    {
        var services = new ServiceCollection();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<ValidationBehaviorTests>();
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddTransient<IRequestHandler<OpenProbeRequest, string>, OpenProbeHandler>();
        var bus = new Bus(services.BuildServiceProvider().GetRequiredService<IMediator>());

        var result = await bus.Send(new OpenProbeRequest());

        result.ShouldBe("ok");
    }

    [Test]
    public async Task Handle_Should_ReachHandler_WhenCreateVendorWouldFailFluentValidation()
    {
        var services = new ServiceCollection();
        services.AddTransient<IValidator<CreateVendorCommand>, CreateVendorCommandValidator>();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<ValidationBehaviorTests>();
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddTransient<IRequestHandler<CreateVendorCommand, CreateVendorResult>, CreateVendorProbeHandler>();
        var bus = new Bus(services.BuildServiceProvider().GetRequiredService<IMediator>());

        var result = await bus.Send(new CreateVendorCommand("", "HVAC", null, null, null));

        result.Succeeded.ShouldBeTrue();
        result.Message.ShouldBe("handler-ran");
    }

    private static Bus BusWithValidators(StateCommandResult? response = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddTransient<IValidator<SaveDraftCommand>, SaveDraftCommandValidator>();
        services.AddTransient<IValidator<InProgressToAssignedCommand>, InProgressToAssignedCommandValidator>();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<ValidationBehaviorTests>();
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddTransient<IRequestHandler<SaveDraftCommand, StateCommandResult>>(
            _ => new FixedStateCommandHandler(response));
        services.AddTransient<IRequestHandler<InProgressToAssignedCommand, StateCommandResult>>(
            _ => new FixedStateCommandHandler(response));
        return new Bus(services.BuildServiceProvider().GetRequiredService<IMediator>());
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

    private sealed class FixedStateCommandHandler(StateCommandResult? response)
        : IRequestHandler<SaveDraftCommand, StateCommandResult>,
            IRequestHandler<InProgressToAssignedCommand, StateCommandResult>
    {
        public Task<StateCommandResult> Handle(SaveDraftCommand request, CancellationToken cancellationToken) =>
            Task.FromResult(response ?? new StateCommandResult(request.WorkOrder));

        public Task<StateCommandResult> Handle(InProgressToAssignedCommand request, CancellationToken cancellationToken) =>
            Task.FromResult(response ?? new StateCommandResult(request.WorkOrder));
    }

    private record OpenProbeRequest : IRequest<string>;

    private sealed class OpenProbeHandler : IRequestHandler<OpenProbeRequest, string>
    {
        public Task<string> Handle(OpenProbeRequest request, CancellationToken cancellationToken) =>
            Task.FromResult("ok");
    }

    private sealed class CreateVendorProbeHandler : IRequestHandler<CreateVendorCommand, CreateVendorResult>
    {
        public Task<CreateVendorResult> Handle(CreateVendorCommand request, CancellationToken cancellationToken) =>
            Task.FromResult(new CreateVendorResult(true, "handler-ran", Guid.Empty));
    }
}
