using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Mediator;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.SignIn;

/// <summary>
/// Stub bus that returns a single employee with a middle name for login welcome tests.
/// </summary>
public sealed class LoginMiddleNameEmployeeBus : StubBus
{
    /// <inheritdoc />
    public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        if (request is EmployeeGetAllQuery)
        {
            var employees = new[]
            {
                EmployeeBuilder.Create("mjsimpson")
                    .WithFirstName("Mary")
                    .WithLastName("Simpson")
                    .WithEmail("mary@springfield.com")
                    .WithMiddleName("Jo")
                    .Build()
            };
            return Task.FromResult((TResponse)(object)employees);
        }

        return base.Send(request);
    }
}
