using ClearMeasure.Bootcamp.Core.Model.Events;
using Mediator;
using Microsoft.Extensions.Logging;
using System.Diagnostics.Metrics;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class TelemetryHandler(ILogger<TelemetryHandler> logger)
    : INotificationHandler<UserLoggedInEvent>
{
    private static readonly Meter Meter = new("ChurchBulletin.Application", "1.0.0");

    private static readonly Counter<long> LoginCounter = Meter.CreateCounter<long>(
        "app.user.logins",
        unit: "{logins}",
        description: "Number of user login events");

    public ValueTask Handle(UserLoggedInEvent request, CancellationToken cancellationToken)
    {
        LoginCounter.Add(1, new KeyValuePair<string, object?>("user.name", request.UserName));
        logger.LogInformation("Recorded login metric for {UserName}", request.UserName);
        return ValueTask.CompletedTask;
    }
}