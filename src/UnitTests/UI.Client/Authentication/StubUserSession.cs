using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;

/// <summary>
/// Shared fake <see cref="IUserSession"/> for page and component tests.
/// </summary>
public class StubUserSession : IUserSession
{
    /// <summary>
    /// Creates a stub with an optional employee and user name (defaults to the employee's user name).
    /// </summary>
    public StubUserSession(Employee? currentUser = null, string? userName = null)
    {
        CurrentUser = currentUser;
        UserName = userName ?? currentUser?.UserName;
    }

    /// <inheritdoc />
    public event Action? AuthenticationStateChanged;

    /// <summary>Employee returned by <see cref="GetCurrentUserAsync"/> when <see cref="GetCurrentUserFault"/> is null.</summary>
    public Employee? CurrentUser { get; set; }

    /// <summary>User name returned by <see cref="GetCurrentUserNameAsync"/>.</summary>
    public string? UserName { get; set; }

    /// <summary>Optional side effect when <see cref="SignInAsync"/> runs (for example auth-provider login).</summary>
    public Func<string, Task>? OnSignIn { get; init; }

    /// <summary>Optional side effect when <see cref="SignOutAsync"/> runs.</summary>
    public Func<Task>? OnSignOut { get; init; }

    /// <summary>When set, <see cref="GetCurrentUserAsync"/> throws this exception.</summary>
    public Exception? GetCurrentUserFault { get; init; }

    /// <inheritdoc />
    public Task<string?> GetCurrentUserNameAsync() => Task.FromResult(UserName);

    /// <inheritdoc />
    public Task<Employee?> GetCurrentUserAsync()
    {
        if (GetCurrentUserFault != null)
        {
            throw GetCurrentUserFault;
        }

        return Task.FromResult(CurrentUser);
    }

    /// <inheritdoc />
    public async Task SignInAsync(string username)
    {
        UserName = username;
        if (OnSignIn != null)
        {
            await OnSignIn(username);
        }

        RaiseAuthenticationStateChanged();
    }

    /// <inheritdoc />
    public async Task SignOutAsync()
    {
        UserName = null;
        CurrentUser = null;
        if (OnSignOut != null)
        {
            await OnSignOut();
        }

        RaiseAuthenticationStateChanged();
    }

    /// <summary>Raises <see cref="AuthenticationStateChanged"/> for tests that simulate sign-in mid-render.</summary>
    public void RaiseAuthenticationStateChanged() => AuthenticationStateChanged?.Invoke();
}
