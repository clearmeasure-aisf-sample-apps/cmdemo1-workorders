using ClearMeasure.Bootcamp.Core.Model.Events;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using Shouldly;
using System.Diagnostics.Metrics;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class TelemetryHandlerTests
{
    private sealed class LoginCapture
    {
        public long Recorded { get; private set; }
        public string? UserName { get; private set; }

        public void OnMeasurement(long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            Recorded += measurement;
            foreach (var tag in tags)
            {
                if (tag.Key == "user.name")
                {
                    UserName = tag.Value?.ToString();
                }
            }
        }
    }

    [Test]
    public async Task ShouldIncrementLoginCounter_WhenUserLogsIn()
    {
        var capture = new LoginCapture();
        using var listener = GivenLoginListener(capture.OnMeasurement);

        await WhenUserLogsInAsync("testuser");
        listener.RecordObservableInstruments();

        capture.Recorded.ShouldBeGreaterThan(0);
        capture.UserName.ShouldBe("testuser");
    }

    [Test]
    public async Task Handle_WithMultipleLogins_IncrementsCounterForEach()
    {
        long recorded = 0;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (publishedInstrument, meterListener) =>
        {
            if (publishedInstrument.Name == "app.user.logins")
            {
                meterListener.EnableMeasurementEvents(publishedInstrument);
            }
        };

        listener.SetMeasurementEventCallback<long>((_, measurement, _, _) =>
        {
            recorded += measurement;
        });

        listener.Start();

        var handler = TestHost.GetRequiredService<TelemetryHandler>();

        await handler.Handle(new UserLoggedInEvent("user1"), CancellationToken.None);
        await handler.Handle(new UserLoggedInEvent("user2"), CancellationToken.None);

        listener.RecordObservableInstruments();

        recorded.ShouldBe(2);
    }

    private static MeterListener GivenLoginListener(
        Action<long, ReadOnlySpan<KeyValuePair<string, object?>>> onMeasurement)
    {
        var listener = new MeterListener();
        listener.InstrumentPublished = (publishedInstrument, meterListener) =>
        {
            if (publishedInstrument.Name == "app.user.logins")
            {
                meterListener.EnableMeasurementEvents(publishedInstrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, measurement, tags, _) => onMeasurement(measurement, tags));
        listener.Start();
        return listener;
    }

    private static Task WhenUserLogsInAsync(string userName) =>
        TestHost.GetRequiredService<TelemetryHandler>().Handle(new UserLoggedInEvent(userName), CancellationToken.None);
}
