using System.Security.Claims;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using Microsoft.AspNetCore.Components.Authorization;

namespace ClearMeasure.Bootcamp.UI.Client;

/// <summary>
/// Circuit-scoped signed-in user: claim-fallback user name and cached <see cref="Employee"/>.
/// Blazor circuits are single-threaded, so the cache needs no lock; a generation counter
/// still drops in-flight writes that started before sign-in or sign-out.
/// </summary>
public class UserSession : IUserSession
{
    private readonly IBus _bus;
    private readonly CustomAuthenticationStateProvider _authProvider;
    private int _generation;
    private string? _cachedUserName;
    private bool _userNameResolved;
    private Employee? _cachedEmployee;
    private string? _employeeCachedForUserName;

    /// <summary>
    /// Creates a session bound to the authentication provider and employee bus.
    /// </summary>
    public UserSession(IBus bus, CustomAuthenticationStateProvider authProvider)
    {
        _bus = bus;
        _authProvider = authProvider;
        _authProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    /// <inheritdoc />
    public event Action? AuthenticationStateChanged;

    /// <inheritdoc />
    public async Task<string?> GetCurrentUserNameAsync()
    {
        if (_userNameResolved)
        {
            return _cachedUserName;
        }

        var generation = _generation;
        var authState = await _authProvider.GetAuthenticationStateAsync();
        var userName = ResolveUserName(authState.User);
        if (generation == _generation)
        {
            _cachedUserName = userName;
            _userNameResolved = true;
        }

        return userName;
    }

    /// <inheritdoc />
    public async Task<Employee?> GetCurrentUserAsync()
    {
        var username = await GetCurrentUserNameAsync();
        if (string.IsNullOrEmpty(username))
        {
            return null;
        }

        if (_employeeCachedForUserName == username)
        {
            return _cachedEmployee;
        }

        var generation = _generation;
        var currentUser = await _bus.Send(new EmployeeByUserNameQuery(username));
        EnsureEmployeeExists(currentUser);
        if (generation == _generation)
        {
            _cachedEmployee = currentUser;
            _employeeCachedForUserName = username;
        }

        return currentUser;
    }

    /// <inheritdoc />
    public Task SignInAsync(string username) => _authProvider.Login(username);

    /// <inheritdoc />
    public Task SignOutAsync() => _authProvider.Logout();

    private void OnAuthenticationStateChanged(Task<AuthenticationState> _)
    {
        _generation++;
        _cachedUserName = null;
        _userNameResolved = false;
        _cachedEmployee = null;
        _employeeCachedForUserName = null;
        AuthenticationStateChanged?.Invoke();
    }

    private static string? ResolveUserName(ClaimsPrincipal user) =>
        (user.FindFirst("preferred_username") ?? user.FindFirst("name"))?.Value ?? user.Identity?.Name;

    private static void EnsureEmployeeExists(Employee? employee)
    {
        if (employee == null)
        {
            throw new InvalidEmployeeLoginException();
        }
    }
}
