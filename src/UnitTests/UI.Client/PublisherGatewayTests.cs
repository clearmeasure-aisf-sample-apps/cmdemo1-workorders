using System.Net;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Messaging;
using ClearMeasure.Bootcamp.Core.Model.Events;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Client;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Client;

[TestFixture]
public class PublisherGatewayTests
{
    [Test]
    public async Task Publish_WithRemotableRequest_WrapsInWebServiceMessageAndSendsToTopic()
    {
        var stubGateway = new StubPublisherGateway();
        var request = new EmployeeByUserNameQuery("test");

        await stubGateway.Publish(request);

        stubGateway.LastMessage.ShouldNotBeNull();
        stubGateway.LastMessage!.GetBodyObject().ShouldBeOfType<EmployeeByUserNameQuery>();
        ((EmployeeByUserNameQuery)stubGateway.LastMessage.GetBodyObject()).Username.ShouldBe("test");
    }

    [Test]
    public async Task Publish_WithRemotableRequest_ReturnsSendToTopicResult()
    {
        var stubGateway = new StubPublisherGateway();
        var expectedResponse = new WebServiceMessage(new EmployeeByUserNameQuery("response"));
        stubGateway.SetSendToTopicResponse(expectedResponse);
        var request = new EmployeeByUserNameQuery("test");

        var result = await stubGateway.Publish(request);

        result.ShouldBe(expectedResponse);
    }

    [Test]
    public async Task Publish_WithRemotableEvent_WrapsInWebServiceMessageAndSendsToTopic()
    {
        var stubGateway = new StubPublisherGateway();
        var remotableEvent = new UserLoggedInEvent("event data");

        await stubGateway.Publish(remotableEvent);

        stubGateway.LastMessage.ShouldNotBeNull();
        stubGateway.LastMessage!.GetBodyObject().ShouldBeOfType<UserLoggedInEvent>();
        ((UserLoggedInEvent)stubGateway.LastMessage.GetBodyObject()).UserName.ShouldBe("event data");
    }

    [Test]
    public async Task SendToTopic_WithValidMessage_PostsToCorrectUrl()
    {
        var stubHandler = new StubHttpMessageHandler();
        var responseMessage = new WebServiceMessage(new EmployeeByUserNameQuery("resp"));
        stubHandler.SetResponse(responseMessage.GetJson());
        var httpClient = new HttpClient(stubHandler) { BaseAddress = new Uri("http://localhost/") };
        var gateway = new PublisherGateway(httpClient);
        var message = new WebServiceMessage(new EmployeeByUserNameQuery("req"));

        await gateway.SendToTopic(message);

        stubHandler.LastRequestUri.ShouldNotBeNull();
        stubHandler.LastRequestUri!.ToString().ShouldContain(PublisherGateway.ApiRelativeUrl);
    }

    [Test]
    public async Task SendToTopic_WithValidMessage_ReturnsDeserializedWebServiceMessage()
    {
        var stubHandler = new StubHttpMessageHandler();
        var expectedBody = new EmployeeByUserNameQuery("response data");
        var responseMessage = new WebServiceMessage(expectedBody);
        stubHandler.SetResponse(responseMessage.GetJson());
        var httpClient = new HttpClient(stubHandler) { BaseAddress = new Uri("http://localhost/") };
        var gateway = new PublisherGateway(httpClient);
        var message = new WebServiceMessage(new EmployeeByUserNameQuery("req"));

        var result = await gateway.SendToTopic(message);

        result.ShouldNotBeNull();
        result.GetBodyObject().ShouldBeOfType<EmployeeByUserNameQuery>();
        ((EmployeeByUserNameQuery)result.GetBodyObject()).Username.ShouldBe("response data");
    }

    [Test]
    public async Task SendToTopic_When_ConfigurationHasKey_AddsXApiKeyHeader()
    {
        var stubHandler = new StubHttpMessageHandler();
        stubHandler.SetResponse(new WebServiceMessage().GetJson());
        var httpClient = new HttpClient(stubHandler) { BaseAddress = new Uri("http://localhost/") };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ApiKeyAuthentication:ValidationKey"] = "  client-secret  "
        }).Build();
        var gateway = new PublisherGateway(httpClient, configuration);
        var message = new WebServiceMessage(new EmployeeByUserNameQuery("req"));

        await gateway.SendToTopic(message);

        stubHandler.LastRequest.ShouldNotBeNull();
        stubHandler.LastRequest!.Headers.TryGetValues(ApiKeyConstants.HeaderName, out var values).ShouldBeTrue();
        values.Single().ShouldBe("client-secret");
    }

    private class StubPublisherGateway : PublisherGateway
    {
        private WebServiceMessage? _sendToTopicResponse = new();

        public WebServiceMessage? LastMessage { get; private set; }

        // ReSharper disable once ConvertToPrimaryConstructor -- epic guardrail: no mass primary-constructor conversion
        public StubPublisherGateway() : base(new HttpClient())
        {
        }

        public void SetSendToTopicResponse(WebServiceMessage? response)
        {
            _sendToTopicResponse = response;
        }

        public override Task<WebServiceMessage?> SendToTopic(WebServiceMessage message)
        {
            LastMessage = message;
            return Task.FromResult(_sendToTopicResponse);
        }
    }

    private class StubHttpMessageHandler : HttpMessageHandler
    {
        private string _responseContent = "";

        public Uri? LastRequestUri { get; private set; }

        public HttpRequestMessage? LastRequest { get; private set; }

        public void SetResponse(string content)
        {
            _responseContent = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastRequestUri = request.RequestUri;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseContent)
            };
            return Task.FromResult(response);
        }
    }
}
