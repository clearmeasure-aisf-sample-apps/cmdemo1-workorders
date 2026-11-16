using ClearMeasure.Bootcamp.Core.Messaging;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Server.Middleware;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using ClearMeasure.Bootcamp.UnitTests.Core;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.Middleware;

[TestFixture]
public class WebServiceMessagePayloadValidatorTests
{
    [Test]
    public async Task ShouldPass_WhenThePayloadSatisfiesItsValidator()
    {
        var services = ServicesWith(new WorkOrderByNumberQueryRule());

        var result = await WebServiceMessagePayloadValidator.ValidateAsync(
            new WebServiceMessage(new WorkOrderByNumberQuery("WO-1")), services, CancellationToken.None);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldListTheFailures_WhenThePayloadBreaksItsValidator()
    {
        var services = ServicesWith(new WorkOrderByNumberQueryRule());

        var result = await WebServiceMessagePayloadValidator.ValidateAsync(
            new WebServiceMessage(new WorkOrderByNumberQuery("")), services, CancellationToken.None);

        result.IsValid.ShouldBeFalse();
        result.Errors.Single().PropertyName.ShouldBe(nameof(WorkOrderByNumberQuery.Number));
    }

    [Test]
    public async Task ShouldPass_WhenARemotablePayloadHasNoValidator()
    {
        var services = new ServiceCollection().BuildServiceProvider();

        var result = await WebServiceMessagePayloadValidator.ValidateAsync(
            new WebServiceMessage(new WorkOrderByNumberQuery("WO-1")), services, CancellationToken.None);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldBeABadRequest_WhenThePayloadIsNotRemotable()
    {
        var services = new ServiceCollection().BuildServiceProvider();

        var result = await WebServiceMessagePayloadValidator.ValidateAsync(
            new WebServiceMessage(new WeatherForecast()), services, CancellationToken.None);

        result.BadRequestDetail.ShouldBe(
            $"Type {typeof(WeatherForecast).FullName} is not a remotable request or event.");
    }

    [Test]
    public async Task ShouldBeABadRequest_WhenChatClientConfigQueryIsPosted()
    {
        var result = await WebServiceMessagePayloadValidator.ValidateAsync(
            ForeignWebServiceMessage.Carrying(new ChatClientConfigQuery()),
            new ServiceCollection().BuildServiceProvider(),
            CancellationToken.None);

        result.IsValid.ShouldBeFalse();
        result.BadRequestDetail.ShouldBe(
            "Invalid message payload or type.");
    }

    [Test]
    public async Task ShouldBeABadRequest_WhenWorkOrderChatQueryIsPosted()
    {
        var workOrder = new WorkOrder { Title = "T", Creator = new Employee() };
        var result = await WebServiceMessagePayloadValidator.ValidateAsync(
            ForeignWebServiceMessage.Carrying(new WorkOrderChatQuery("hi", workOrder)),
            new ServiceCollection().BuildServiceProvider(),
            CancellationToken.None);

        result.IsValid.ShouldBeFalse();
        result.BadRequestDetail.ShouldBe(
            "Invalid message payload or type.");
    }

    [Test]
    public async Task ShouldBeABadRequest_WhenThePayloadTypeCannotBeLoaded()
    {
        var message = new WebServiceMessage { TypeName = "No.Such.Type, NoSuchAssembly", Body = "{}" };

        var result = await WebServiceMessagePayloadValidator.ValidateAsync(
            message, new ServiceCollection().BuildServiceProvider(), CancellationToken.None);

        result.BadRequestDetail.ShouldBe("Invalid message payload or type.");
    }

    private static ServiceProvider ServicesWith(IValidator<WorkOrderByNumberQuery> validator) =>
        new ServiceCollection().AddSingleton(validator).BuildServiceProvider();

    private sealed class WorkOrderByNumberQueryRule : AbstractValidator<WorkOrderByNumberQuery>
    {
        public WorkOrderByNumberQueryRule() => RuleFor(query => query.Number).NotEmpty();
    }
}
