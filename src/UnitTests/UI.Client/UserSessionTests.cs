using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Client;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using MediatR;
using Shouldly;
using ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Client;

[TestFixture]
public class UserSessionTests
{
    [Test]
    public async Task GetCurrentUserAsync_ShouldReturnNull_WhenUsernameEmpty()
    {
        var authProvider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        var session = new UserSession(new StubEmployeeBus(null), authProvider);

        var user = await session.GetCurrentUserAsync();

        user.ShouldBeNull();
    }

    [Test]
    public async Task GetCurrentUserNameAsync_ShouldPreferPreferredUsernameClaim()
    {
        var store = new StubUserSessionStore();
        var authProvider = new CustomAuthenticationStateProvider(store);
        await authProvider.Login("from-name-claim");
        // Login sets ClaimTypes.Name; claim-fallback still resolves Identity.Name.
        var session = new UserSession(new StubEmployeeBus(null), authProvider);

        var userName = await session.GetCurrentUserNameAsync();

        userName.ShouldBe("from-name-claim");
    }

    [Test]
    public async Task GetCurrentUserAsync_ShouldReturnEmployee_WhenValid()
    {
        var employee = new Employee("hsimpson", "Homer", "Simpson", "homer@example.com")
        {
            Id = Guid.NewGuid()
        };
        var authProvider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        await authProvider.Login("hsimpson");
        var stubBus = new StubEmployeeBus(employee);
        var session = new UserSession(stubBus, authProvider);

        var user = await session.GetCurrentUserAsync();

        user.ShouldNotBeNull();
        user.UserName.ShouldBe("hsimpson");
        user.FirstName.ShouldBe("Homer");
        stubBus.LastQueriedUsername.ShouldBe("hsimpson");
    }

    [Test]
    public async Task GetCurrentUserAsync_ShouldCacheEmployee_ForSameCircuit()
    {
        var employee = new Employee("hsimpson", "Homer", "Simpson", "homer@example.com")
        {
            Id = Guid.NewGuid()
        };
        var authProvider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        await authProvider.Login("hsimpson");
        var stubBus = new StubEmployeeBus(employee);
        var session = new UserSession(stubBus, authProvider);

        await session.GetCurrentUserAsync();
        await session.GetCurrentUserAsync();

        stubBus.QueryCount.ShouldBe(1);
    }

    [Test]
    public async Task GetCurrentUserAsync_ShouldRestoreStoredUsername_BeforeQueryingEmployee()
    {
        var employee = new Employee("tlovejoy", "Timothy", "Lovejoy", "lovejoy@example.com")
        {
            Id = Guid.NewGuid()
        };
        var store = new StubUserSessionStore { Username = "tlovejoy" };
        var authProvider = new CustomAuthenticationStateProvider(store);
        var stubBus = new StubEmployeeBus(employee);
        var session = new UserSession(stubBus, authProvider);

        var user = await session.GetCurrentUserAsync();

        user.ShouldBe(employee);
        stubBus.LastQueriedUsername.ShouldBe("tlovejoy");
    }

    [Test]
    public async Task GetCurrentUserAsync_ShouldThrowInvalidEmployeeLogin_WhenEmployeeMissing()
    {
        var authProvider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        await authProvider.Login("unknown");
        var session = new UserSession(new StubEmployeeBus(null), authProvider);

        var ex = await Should.ThrowAsync<InvalidEmployeeLoginException>(session.GetCurrentUserAsync);

        ex.Message.ShouldContain("doesn't exist");
    }

    [Test]
    public async Task SignOutAsync_ShouldClearAuth()
    {
        var authProvider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        await authProvider.Login("hsimpson");
        var session = new UserSession(new StubEmployeeBus(null), authProvider);

        await session.SignOutAsync();

        var authState = await authProvider.GetAuthenticationStateAsync();
        authState.User.Identity!.IsAuthenticated.ShouldBeFalse();
    }

    [Test]
    public async Task SignInAsync_ShouldAuthenticateAndRaiseChanged()
    {
        var authProvider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        var session = new UserSession(new StubEmployeeBus(null), authProvider);
        var raised = false;
        session.AuthenticationStateChanged += () => raised = true;

        await session.SignInAsync("hsimpson");

        authProvider.IsAuthenticated().ShouldBeTrue();
        authProvider.GetUsername().ShouldBe("hsimpson");
        raised.ShouldBeTrue();
    }

    private sealed class StubEmployeeBus(Employee? employee) : IBus
    {
        public string? LastQueriedUsername { get; private set; }
        public int QueryCount { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is EmployeeByUserNameQuery query)
            {
                QueryCount++;
                LastQueriedUsername = query.Username;
                return Task.FromResult((TResponse)(object)employee!);
            }

            throw new NotSupportedException(request.GetType().Name);
        }

        public Task<object?> Send(object request) => throw new NotSupportedException();

        public Task Publish(INotification notification) => throw new NotSupportedException();
    }
}
