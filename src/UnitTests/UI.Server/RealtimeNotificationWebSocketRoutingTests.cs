using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UI.Server.Notifications;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class RealtimeNotificationWebSocketRoutingTests
{
    [Test]
    public async Task Should_CallNext_When_PathIsNotNotifications()
    {
        var next = new NextRecorder();
        var context = new DefaultHttpContext { Request = { Path = "/api/work-orders" } };

        await new RealtimeNotificationWebSocketMiddleware(next.Invoke, new RealtimeNotificationHub()).InvokeAsync(context);

        next.Calls.ShouldBe(1);
    }

    [Test]
    public async Task Should_Return400_When_NotificationsPathIsNotWebSocketRequest()
    {
        var next = new NextRecorder();
        var context = new DefaultHttpContext { Request = { Path = "/ws/notifications" } };

        await new RealtimeNotificationWebSocketMiddleware(next.Invoke, new RealtimeNotificationHub()).InvokeAsync(context);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        next.Calls.ShouldBe(0);
    }
}
