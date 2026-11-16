using ClearMeasure.Bootcamp.Core.Queries;
using MediatR;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.Login;

/// <summary>
/// Stub bus that delays <see cref="EmployeeGetAllQuery"/> until <see cref="ReleaseEmployees"/> is called.
/// </summary>
public sealed class LoginGatedEmployeeBus : StubBus
{
    private readonly TaskCompletionSource _gate =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Completes the gate so employee load can finish.</summary>
    public void ReleaseEmployees() => _gate.TrySetResult();

    /// <inheritdoc />
    public override async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        if (request is EmployeeGetAllQuery)
        {
            await _gate.Task;
        }

        return await base.Send(request);
    }
}
