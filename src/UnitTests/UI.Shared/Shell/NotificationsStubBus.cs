using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Mediator;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Shell;

/// <summary>
/// Stub bus that returns a fixed <see cref="NotificationsSummary"/> for the shell bell.
/// </summary>
internal sealed class NotificationsStubBus(NotificationsSummary? result, int delayMilliseconds = 0) : StubBus
{
    public string? LastUserName { get; private set; }

    public NotificationsSummary? Result { get; set; } = result;

    public override async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        if (request is not NotificationsPanelQuery query)
            return await base.Send(request);

        LastUserName = query.UserName;
        if (delayMilliseconds > 0)
            await Task.Delay(delayMilliseconds);

        if (Result is null)
            throw new InvalidOperationException("boom");

        return (TResponse)(object)Result;
    }
}
