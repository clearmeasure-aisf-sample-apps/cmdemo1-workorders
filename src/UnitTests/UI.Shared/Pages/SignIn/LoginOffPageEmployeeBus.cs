using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using MediatR;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.SignIn;

/// <summary>
/// Stub bus whose <see cref="EmployeeGetAllQuery"/> result excludes tlovejoy, simulating
/// a user past the first page of the employee directory.
/// </summary>
public sealed class LoginOffPageEmployeeBus : StubBus
{
    /// <summary>
    /// When true, <see cref="EmployeeByUserNameQuery"/> throws for every user name
    /// including tlovejoy, simulating a user the server does not know.
    /// </summary>
    public bool FailAllLookups { get; init; }

    /// <inheritdoc />
    public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        if (request is EmployeeGetAllQuery)
        {
            var employees = new[]
            {
                EmployeeBuilder.Create("hsimpson").WithFirstName("HOMER").WithLastName("SIMPSON").WithEmail("homer@springfield.com").Build(),
                EmployeeBuilder.Create("mburns").WithFirstName("Montgomery").WithLastName("Burns").WithEmail("burns@plant.com").Build(),
                EmployeeBuilder.Create("nflanders").WithFirstName("Ned").WithLastName("Flanders").WithEmail("ned@flanders.com").Build()
            };
            return Task.FromResult((TResponse)(object)employees);
        }

        if (request is EmployeeByUserNameQuery query)
        {
            if (!FailAllLookups && query.Username == "tlovejoy")
            {
                var employee = EmployeeBuilder.Create("tlovejoy")
                    .WithFirstName("Timothy")
                    .WithLastName("Lovejoy Jr")
                    .WithEmail("reverend@firstchurchspringfield.org")
                    .Build();
                return Task.FromResult((TResponse)(object)employee);
            }

            throw new InvalidOperationException("No employee with that user name was found.");
        }

        return base.Send(request);
    }
}
