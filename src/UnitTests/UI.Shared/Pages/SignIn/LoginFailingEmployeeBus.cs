using ClearMeasure.Bootcamp.Core.Queries;
using MediatR;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.SignIn;

/// <summary>
/// Stub bus that fails <see cref="EmployeeGetAllQuery"/> so the login page shows an error alert.
/// </summary>
public sealed class LoginFailingEmployeeBus : StubBus
{
    /// <inheritdoc />
    public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        if (request is EmployeeGetAllQuery)
        {
            throw new InvalidOperationException("employee load failed");
        }

        return base.Send(request);
    }
}
