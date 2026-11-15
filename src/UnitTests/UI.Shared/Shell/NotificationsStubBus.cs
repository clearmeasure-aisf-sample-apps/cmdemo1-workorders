using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using MediatR;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Shell;

/// <summary>
/// Stub bus that returns a fixed <see cref="NotificationsSummary"/> for the shell bell.
/// </summary>
internal sealed class NotificationsStubBus(NotificationsSummary? result) : StubBus
{
    public string? LastUserName { get; private set; }

    public NotificationsSummary? Result { get; set; } = result;

    public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        if (request is not NotificationsPanelQuery query)
            return base.Send(request);

        LastUserName = query.UserName;
        if (Result is null)
            throw new InvalidOperationException("boom");

        return Task.FromResult((TResponse)(object)Result);
    }
}
