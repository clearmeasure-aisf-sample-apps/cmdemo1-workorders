using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.Events;
using ClearMeasure.Bootcamp.UI.Server.Endpoints;
using Mediator;
using Shouldly;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Messaging;
using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class SingleApiControllerTests
{
    [Test]
    public async Task Should_Post_ReturnSerializedResponse_ForRemotableRequest()
    {
        var expectedEmployees = new[] { new Employee("hsimpson", "Homer", "Simpson", "homer@test.com") };
        var stubBus = new StubBus();
        stubBus.SetSendResponse(expectedEmployees);
        
        var request = new EmployeeGetAllQuery();
        var message = new WebServiceMessage(request);
        var json = await SingleApiEndpoint.Post(message, stubBus);

        var responseMessage = JsonSerializer.Deserialize<WebServiceMessage>(json)!;
        var result = (Employee[])responseMessage.GetBodyObject();
        result.Length.ShouldBe(1);
        result[0].UserName.ShouldBe("hsimpson");
    }

    [Test]
    public async Task Should_Post_PublishAndReturnEmptyMessage_ForRemotableEvent()
    {
        var stubBus = new StubBus();
        
        var loginEvent = new UserLoggedInEvent("testuser");
        var message = new WebServiceMessage(loginEvent);
        var json = await SingleApiEndpoint.Post(message, stubBus);

        stubBus.LastPublishedNotification.ShouldBeOfType<UserLoggedInEvent>();
        ((UserLoggedInEvent)stubBus.LastPublishedNotification!).UserName.ShouldBe("testuser");
        var responseMessage = JsonSerializer.Deserialize<WebServiceMessage>(json)!;
        responseMessage.Body.ShouldBeEmpty();
    }

    [Test]
    public async Task Should_Post_PublishViaBusNotSend_ForRemotableEvent()
    {
        var stubBus = new StubBus();
        
        var loginEvent = new UserLoggedInEvent("testuser");
        var message = new WebServiceMessage(loginEvent);
        await SingleApiEndpoint.Post(message, stubBus);

        stubBus.LastPublishedNotification.ShouldNotBeNull();
        stubBus.LastSentObject.ShouldBeNull();
    }

    [Test]
    public async Task Should_Post_ReturnEmptyTypedWebServiceMessage_ForRemotableEvent()
    {
        var stubBus = new StubBus();
        
        var loginEvent = new UserLoggedInEvent("testuser");
        var message = new WebServiceMessage(loginEvent);
        var json = await SingleApiEndpoint.Post(message, stubBus);

        var responseMessage = JsonSerializer.Deserialize<WebServiceMessage>(json)!;
        responseMessage.Body.ShouldBeEmpty();
        responseMessage.TypeName.ShouldBeEmpty();
    }

    [Test]
    public async Task Should_Post_Throw_ForNonRemotableObject()
    {
        var stubBus = new StubBus();
        
        var message = new WebServiceMessage(new NonRemotableObject { Value = "test" });
        await Should.ThrowAsync<InvalidOperationException>(async () => await SingleApiEndpoint.Post(message, stubBus));
        message.GetBodyObject().ShouldBeOfType<NonRemotableObject>().Value.ShouldBe("test");
    }

    [Test]
    public async Task Should_Post_SendBodyObjectToBus_ForRemotableRequest()
    {
        var stubBus = new StubBus();
        stubBus.SetSendResponse(Array.Empty<Employee>());
        
        var request = new EmployeeGetAllQuery();
        var message = new WebServiceMessage(request);
        await SingleApiEndpoint.Post(message, stubBus);

        stubBus.LastSentObject.ShouldBeOfType<EmployeeGetAllQuery>();
    }

    [Test]
    public async Task Should_Post_Throw_WhenBusReturnsNull_ForRemotableRequest()
    {
        var stubBus = new StubBus();
        stubBus.SetSendResponse(null);
        
        var request = new EmployeeGetAllQuery();
        var message = new WebServiceMessage(request);
        await Should.ThrowAsync<InvalidOperationException>(async () => await SingleApiEndpoint.Post(message, stubBus));
    }

    private class StubBus : IBus
    {
        private object? _sendResponse;
        public object? LastSentObject { get; private set; }
        public object? LastPublishedNotification { get; private set; }

        public void SetSendResponse(object? response)
        {
            _sendResponse = response;
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            throw new NotImplementedException();
        }

        public Task<object?> Send(object request)
        {
            LastSentObject = request;
            return Task.FromResult(_sendResponse);
        }

        public Task Publish(INotification notification)
        {
            LastPublishedNotification = notification;
            return Task.CompletedTask;
        }
    }

    private class NonRemotableObject
    {
        public string Value { get; init; } = "";
    }
}
