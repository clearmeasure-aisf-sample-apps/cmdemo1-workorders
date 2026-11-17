using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Client;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;
using MediatR;
using Microsoft.AspNetCore.Components.Authorization;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Client;

[TestFixture]
public class UserSessionTests
{
    [Test]
    public async Task GetCurrentUserAsync_ShouldReturnNull_WhenUsernameEmpty()
    {
        var authProvider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        var session = new UserSession(new StubEmployeeBus(), authProvider);

        var user = await session.GetCurrentUserAsync();

        user.ShouldBeNull();
    }

    [Test]
    public async Task GetCurrentUserNameAsync_ShouldPreferPreferredUsernameClaim()
    {
        var store = new StubUserSessionStore();
        var authProvider = new CustomAuthenticationStateProvider(store);
        await authProvider.Login("from-name-claim");
        var session = new UserSession(new StubEmployeeBus(), authProvider);

        var userName = await session.GetCurrentUserNameAsync();

        userName.ShouldBe("from-name-claim");
    }

    [Test]
    public async Task GetCurrentUserAsync_ShouldReturnEmployee_WhenValid()
    {
        var employee = Homer();
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
        var authProvider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        await authProvider.Login("hsimpson");
        var stubBus = new StubEmployeeBus(Homer());
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
        var session = new UserSession(new StubEmployeeBus(), authProvider);

        var ex = await Should.ThrowAsync<InvalidEmployeeLoginException>(session.GetCurrentUserAsync);

        ex.Message.ShouldContain("doesn't exist");
    }

    [Test]
    public async Task SignOutAsync_ShouldClearCachedNameAndEmployee()
    {
        var authProvider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        await authProvider.Login("hsimpson");
        var stubBus = new StubEmployeeBus(Homer());
        var session = new UserSession(stubBus, authProvider);
        (await session.GetCurrentUserNameAsync()).ShouldBe("hsimpson");
        (await session.GetCurrentUserAsync())!.UserName.ShouldBe("hsimpson");

        await session.SignOutAsync();

        (await session.GetCurrentUserNameAsync()).ShouldBeNull();
        (await session.GetCurrentUserAsync()).ShouldBeNull();
        stubBus.QueryCount.ShouldBe(1);
    }

    [Test]
    public async Task SignInAsync_ShouldReplaceCachedNameAndEmployee_WhenDifferentUserSignsIn()
    {
        var authProvider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        var stubBus = new StubEmployeeBus(Homer(), Lovejoy());
        var session = new UserSession(stubBus, authProvider);
        await session.SignInAsync("hsimpson");
        (await session.GetCurrentUserNameAsync()).ShouldBe("hsimpson");
        (await session.GetCurrentUserAsync())!.UserName.ShouldBe("hsimpson");

        await session.SignInAsync("tlovejoy");

        (await session.GetCurrentUserNameAsync()).ShouldBe("tlovejoy");
        (await session.GetCurrentUserAsync())!.UserName.ShouldBe("tlovejoy");
        stubBus.QueryCount.ShouldBe(2);
        stubBus.LastQueriedUsername.ShouldBe("tlovejoy");
    }

    [Test]
    public async Task GetCurrentUserNameAsync_ShouldNotRepopulateCache_WhenAuthChangesDuringLookup()
    {
        var authProvider = new GatedAuthProvider(new StubUserSessionStore());
        await authProvider.Login("hsimpson");
        var session = new UserSession(new StubEmployeeBus(), authProvider);
        authProvider.AfterReadGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var inflight = session.GetCurrentUserNameAsync();
        await WaitUntilWaiting(authProvider);

        await session.SignOutAsync();
        authProvider.AfterReadGate.TrySetResult();
        await inflight;

        (await session.GetCurrentUserNameAsync()).ShouldBeNull();
    }

    [Test]
    public async Task GetCurrentUserAsync_ShouldNotRepopulateCache_WhenAuthChangesDuringLookup()
    {
        var authProvider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        await authProvider.Login("hsimpson");
        var stubBus = new StubEmployeeBus(Homer())
        {
            SendGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)
        };
        var session = new UserSession(stubBus, authProvider);
        var inflight = session.GetCurrentUserAsync();
        await WaitUntilWaiting(stubBus);

        await session.SignOutAsync();
        stubBus.SendGate.TrySetResult();
        (await inflight)!.UserName.ShouldBe("hsimpson");

        (await session.GetCurrentUserNameAsync()).ShouldBeNull();
        (await session.GetCurrentUserAsync()).ShouldBeNull();
        stubBus.QueryCount.ShouldBe(1);
    }

    [Test]
    public async Task SignOutAsync_ShouldClearAuth()
    {
        var authProvider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        await authProvider.Login("hsimpson");
        var session = new UserSession(new StubEmployeeBus(), authProvider);

        await session.SignOutAsync();

        var authState = await authProvider.GetAuthenticationStateAsync();
        authState.User.Identity!.IsAuthenticated.ShouldBeFalse();
    }

    [Test]
    public async Task SignInAsync_ShouldAuthenticateAndRaiseChanged()
    {
        var authProvider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        var session = new UserSession(new StubEmployeeBus(), authProvider);
        var raised = false;
        session.AuthenticationStateChanged += () => raised = true;

        await session.SignInAsync("hsimpson");

        authProvider.IsAuthenticated().ShouldBeTrue();
        authProvider.GetUsername().ShouldBe("hsimpson");
        raised.ShouldBeTrue();
    }

    private static Employee Homer() =>
        EmployeeBuilder.Create("hsimpson")
            .WithFirstName("Homer")
            .WithLastName("Simpson")
            .WithEmail("homer@example.com")
            .WithId(Guid.Parse("11111111-1111-1111-1111-111111111111"))
            .Build();

    private static Employee Lovejoy() =>
        EmployeeBuilder.Create("tlovejoy")
            .WithFirstName("Timothy")
            .WithLastName("Lovejoy")
            .WithEmail("lovejoy@example.com")
            .WithId(Guid.Parse("22222222-2222-2222-2222-222222222222"))
            .Build();

    private static async Task WaitUntilWaiting(GatedAuthProvider authProvider)
    {
        while (authProvider.WaitingCount == 0)
        {
            await Task.Yield();
        }
    }

    private static async Task WaitUntilWaiting(StubEmployeeBus stubBus)
    {
        while (stubBus.WaitingCount == 0)
        {
            await Task.Yield();
        }
    }

    private sealed class GatedAuthProvider(IUserSessionStore store) : CustomAuthenticationStateProvider(store)
    {
        public TaskCompletionSource? AfterReadGate { get; set; }
        public int WaitingCount { get; private set; }

#pragma warning disable BL0013 // -- reason: test double calls GetAuthenticationStateAsync on base without subscribing; callers subscribe
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var state = await base.GetAuthenticationStateAsync();
            var gate = AfterReadGate;
            if (gate == null)
            {
                return state;
            }

            WaitingCount++;
            await gate.Task;
            return state;
        }
#pragma warning restore BL0013
    }

    private sealed class StubEmployeeBus(params Employee?[] employees) : IBus
    {
        private readonly Dictionary<string, Employee?> _byUserName = employees
            .Where(e => e != null)
            .ToDictionary(e => e!.UserName, e => e, StringComparer.OrdinalIgnoreCase);

        public string? LastQueriedUsername { get; private set; }
        public int QueryCount { get; private set; }
        public TaskCompletionSource? SendGate { get; init; }
        public int WaitingCount { get; private set; }

        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is not EmployeeByUserNameQuery query)
            {
                throw new NotSupportedException(request.GetType().Name);
            }

            QueryCount++;
            LastQueriedUsername = query.Username;
            var gate = SendGate;
            if (gate != null)
            {
                WaitingCount++;
                await gate.Task;
            }

            _byUserName.TryGetValue(query.Username, out var employee);
            return (TResponse)(object)employee!;
        }

        public Task<object?> Send(object request) => throw new NotSupportedException();

        public Task Publish(INotification notification) => throw new NotSupportedException();
    }
}
