using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Client;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;
using MediatR;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

[TestFixture]
public class InvalidEmployeeLoginExceptionTests
{
    [Test]
    public void Message_ShouldExplainMissingOrInvalidEmployee()
    {
        var ex = new InvalidEmployeeLoginException();

        ex.Message.ShouldContain("doesn't exist");
        ex.ShouldBeAssignableTo<Exception>();
    }

    [Test]
    public async Task UserSession_ShouldThrow_WhenSignedInUserHasNoEmployee()
    {
        var authProvider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        await authProvider.Login("unknown");
        var session = new UserSession(new MissingEmployeeBus(), authProvider);

        var ex = await Should.ThrowAsync<InvalidEmployeeLoginException>(session.GetCurrentUserAsync);

        ex.Message.ShouldBe("That user doesn't exist or is not valid.");
    }

    private sealed class MissingEmployeeBus : IBus
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is EmployeeByUserNameQuery)
            {
                return Task.FromResult((TResponse)(object)null!);
            }

            throw new NotSupportedException(request.GetType().Name);
        }

        public Task<object?> Send(object request) => throw new NotSupportedException();

        public Task Publish(INotification notification) => throw new NotSupportedException();
    }
}
