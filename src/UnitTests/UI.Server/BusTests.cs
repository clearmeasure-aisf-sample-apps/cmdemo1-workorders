using ClearMeasure.Bootcamp.UnitTests.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared;
using Mediator;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class BusTests
{
    [Test]
    public void Should_SendRequest_WithGenericResponse_CallsMediator()
    {
        var stubMediator = new RecordingMediator();
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
        var stubMediator = new RecordingMediator();
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
        var stubMediator = new RecordingMediator();
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
        var stubMediator = new RecordingMediator();
        var bus = new Bus(stubMediator);
        var notification = new TestNotification { Message = "test message" };

        await bus.Publish(notification);

        stubMediator.LastNotification.ShouldBe(notification);
        ((TestNotification)stubMediator.LastNotification!).Message.ShouldBe("test message");
    }

    private class TestCommand;

    private class TestNotification : INotification
    {
        public string Message { get; init; } = string.Empty;
    }

    private record TestQuery : IRequest<string>;
}
