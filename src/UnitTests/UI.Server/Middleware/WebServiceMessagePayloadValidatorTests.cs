using ClearMeasure.Bootcamp.Core.Messaging;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Server.Middleware;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

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
    public async Task ShouldBeABadRequest_WhenNoValidatorIsRegisteredForThePayload()
    {
        var services = new ServiceCollection().BuildServiceProvider();

        var result = await WebServiceMessagePayloadValidator.ValidateAsync(
            new WebServiceMessage(new WorkOrderByNumberQuery("WO-1")), services, CancellationToken.None);

        result.BadRequestDetail.ShouldBe($"No validator registered for type {typeof(WorkOrderByNumberQuery).FullName}.");
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
