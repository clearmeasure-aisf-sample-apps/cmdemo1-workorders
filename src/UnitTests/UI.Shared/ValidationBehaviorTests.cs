using ClearMeasure.Bootcamp.Core.FeatureFlags;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using ClearMeasure.Bootcamp.UI.Shared;
using FluentValidation;
using Mediator;
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
            failure.ErrorMessage == WorkOrderFieldRules.TitleCannotExceed);
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
            failure.ErrorMessage == WorkOrderFieldRules.BuildingCannotExceed);
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
        var expected = Array.Empty<WeatherForecast>();
        var bus = new Bus(HostPipeline.Build(new AllFeaturesOn(), services =>
                services.AddTransient<IRequestHandler<ForecastQuery, WeatherForecast[]>>(_ => new ForecastProbeHandler(expected)))
            .GetRequiredService<IMediator>());

        var result = await bus.Send(new ForecastQuery());

        result.ShouldBeSameAs(expected);
    }

    [Test]
    public async Task Handle_Should_ReachHandler_WhenDeferredHttpEnvelopeValidatorsAreNotRegisteredAsIValidator()
    {
        var bus = new Bus(HostPipeline.Build(new AllFeaturesOn(), services =>
        {
            services.AddTransient<IRequestHandler<CreateVendorCommand, CreateVendorResult>, CreateVendorProbeHandler>();
            services.AddTransient<IRequestHandler<CreateDatedWorkOrdersCommand, CreateDatedWorkOrdersResult>,
                CreateDatedProbeHandler>();
        }).GetRequiredService<IMediator>());

        var vendor = await bus.Send(new CreateVendorCommand("", "HVAC", null, null, null));
        var dated = await bus.Send(new CreateDatedWorkOrdersCommand("a", "b", "t", "d", []));

        vendor.Succeeded.ShouldBeTrue();
        vendor.Message.ShouldBe("handler-ran");
        dated.Success.ShouldBeTrue();
        dated.Message.ShouldBe("handler-ran");
    }

    private static Bus BusWithValidators(StateCommandResult? response = null)
    {
        // The host registers SaveDraftCommandValidator and InProgressToAssignedCommandValidator once each.
        var provider = HostPipeline.Build(new AllFeaturesOn(), services =>
        {
            services.AddTransient<IRequestHandler<SaveDraftCommand, StateCommandResult>>(
                _ => new FixedStateCommandHandler(response));
            services.AddTransient<IRequestHandler<InProgressToAssignedCommand, StateCommandResult>>(
                _ => new FixedStateCommandHandler(response));
        });
        return new Bus(provider.GetRequiredService<IMediator>());
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
        public ValueTask<StateCommandResult> Handle(SaveDraftCommand request, CancellationToken cancellationToken) =>
            ValueTask.FromResult(response ?? new StateCommandResult(request.WorkOrder));

        public ValueTask<StateCommandResult> Handle(InProgressToAssignedCommand request, CancellationToken cancellationToken) =>
            ValueTask.FromResult(response ?? new StateCommandResult(request.WorkOrder));
    }

    private sealed class ForecastProbeHandler(WeatherForecast[] forecasts) : IRequestHandler<ForecastQuery, WeatherForecast[]>
    {
        public ValueTask<WeatherForecast[]> Handle(ForecastQuery request, CancellationToken cancellationToken) =>
            ValueTask.FromResult(forecasts);
    }

    private sealed class AllFeaturesOn : IFeatureFlagEvaluator
    {
        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class CreateVendorProbeHandler : IRequestHandler<CreateVendorCommand, CreateVendorResult>
    {
        public ValueTask<CreateVendorResult> Handle(CreateVendorCommand request, CancellationToken cancellationToken) =>
            ValueTask.FromResult(new CreateVendorResult(true, "handler-ran", Guid.Empty));
    }

    private sealed class CreateDatedProbeHandler
        : IRequestHandler<CreateDatedWorkOrdersCommand, CreateDatedWorkOrdersResult>
    {
        public ValueTask<CreateDatedWorkOrdersResult> Handle(
            CreateDatedWorkOrdersCommand request,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(new CreateDatedWorkOrdersResult(true, "handler-ran", []));
    }
}
