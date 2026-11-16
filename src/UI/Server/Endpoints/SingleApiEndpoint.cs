using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Messaging;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Shared;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClearMeasure.Bootcamp.UI.Server.Endpoints;

/// <summary>
/// The single endpoint the Blazor client's remotable bus posts every <see cref="WebServiceMessage"/> to: requests
/// are sent on the server bus and answered, events are published.
/// </summary>
public static class SingleApiEndpoint
{
    public static void Map(IEndpointRouteBuilder api)
    {
        foreach (var route in new[]
                 {
                     WebServiceApiRoutes.LegacyRelativeUrl,
                     $"{ApiRoutes.VersionedApiPrefix}/{WebServiceApiRoutes.AbstractPathSegment}"
                 })
        {
            api.MapPost(route, Post).AddEndpointFilter<FeatureDisabledProblemFilter>();
        }
    }

    public static async Task<string> Post(WebServiceMessage webServiceMessage, IBus bus, ILoggerFactory? loggerFactory = null)
    {
        var logger = loggerFactory?.CreateLogger(typeof(SingleApiEndpoint).FullName!) ?? NullLogger.Instance;
        logger.LogDebug("Receiving {messageType}", webServiceMessage.TypeName);
        var bodyObject = webServiceMessage.GetBodyObject();

        if (bodyObject is IRemotableRequest remotableRequest)
        {
            var result = await bus.Send(remotableRequest) ?? throw new InvalidOperationException();
            logger.LogDebug("Returning {resultType}", result.GetType().Name);
            return new WebServiceMessage(result).GetJson();
        }

        if (bodyObject is IRemotableEvent @event)
        {
            await bus.Publish(@event);
            logger.LogDebug("Published {eventName}", @event.GetType().Name);
            return new WebServiceMessage().GetJson();
        }

        throw new InvalidOperationException($"Received a message of type {webServiceMessage.TypeName} that is not a request or event");
    }
}
