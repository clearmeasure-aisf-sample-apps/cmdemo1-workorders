using System.Text;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Messaging;
using ClearMeasure.Bootcamp.Core.Model.Events;
using ClearMeasure.Bootcamp.Core.Validation;
using ClearMeasure.Bootcamp.UI.Server.Middleware;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class WebServiceMessageValidationMiddlewareInvokeTests
{
    private const string SingleApiPath = "/api/v1.0/blazor-wasm-single-api";

    [Test]
    public async Task Should_CallNext_When_RequestIsNotSingleApiPost()
    {
        var next = new NextRecorder();
        var context = CreateContext(HttpMethods.Get, "");

        await CreateMiddleware(next).InvokeAsync(context, new InlineValidator<WebServiceMessage>(), EmptyServices());

        next.Calls.ShouldBe(1);
    }

    [Test]
    public async Task Should_Return400_When_BodyIsNotJson()
    {
        var next = new NextRecorder();
        var context = CreateContext(HttpMethods.Post, "{bad");

        await CreateMiddleware(next).InvokeAsync(context, new InlineValidator<WebServiceMessage>(), EmptyServices());

        context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        next.Calls.ShouldBe(0);
    }

    [Test]
    public async Task Should_Return400_When_EnvelopeValidatorFails()
    {
        var next = new NextRecorder();
        var envelopeValidator = new InlineValidator<WebServiceMessage>();
        envelopeValidator.RuleFor(message => message.TypeName).Must(_ => false);
        var context = CreateContext(HttpMethods.Post, MessageJson(new UserLoggedInEvent("testuser")));

        await CreateMiddleware(next).InvokeAsync(context, envelopeValidator, EmptyServices());

        context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        next.Calls.ShouldBe(0);
    }

    [Test]
    public async Task Should_CallNext_When_EventPayloadIsValid()
    {
        var next = new NextRecorder();
        var context = CreateContext(HttpMethods.Post, MessageJson(new UserLoggedInEvent("testuser")));

        await CreateMiddleware(next).InvokeAsync(context, new InlineValidator<WebServiceMessage>(), EmptyServices());

        next.Calls.ShouldBe(1);
    }

    [Test]
    public async Task Should_Return400_When_PayloadIsNotRemotable()
    {
        var next = new NextRecorder();
        var context = CreateContext(HttpMethods.Post, MessageJson("not remotable"));

        await CreateMiddleware(next).InvokeAsync(context, new InlineValidator<WebServiceMessage>(), EmptyServices());

        context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        next.Calls.ShouldBe(0);
    }

    [Test]
    public async Task Should_Return400_When_PayloadValidatorFails()
    {
        var next = new NextRecorder();
        var context = CreateContext(HttpMethods.Post, MessageJson(new UserLoggedInEvent("")));
        var services = new ServiceCollection()
            .AddSingleton<IValidator<UserLoggedInEvent>, UserLoggedInEventValidator>()
            .BuildServiceProvider();

        await CreateMiddleware(next).InvokeAsync(context, new InlineValidator<WebServiceMessage>(), services);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        next.Calls.ShouldBe(0);
    }

    private static WebServiceMessageValidationMiddleware CreateMiddleware(NextRecorder next) =>
        new(next.Invoke, NullLogger<WebServiceMessageValidationMiddleware>.Instance);

    private static ServiceProvider EmptyServices() => new ServiceCollection().BuildServiceProvider();

    private static string MessageJson(object payload) => JsonSerializer.Serialize(new WebServiceMessage(payload));

    private static DefaultHttpContext CreateContext(string method, string body) =>
        new()
        {
            RequestServices = EmptyServices(),
            Request =
            {
                Method = method,
                Path = SingleApiPath,
                ContentType = "application/json",
                Body = new MemoryStream(Encoding.UTF8.GetBytes(body))
            },
            Response = { Body = new MemoryStream() }
        };
}
