using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>
/// Canonical source for the signed-in user name and <see cref="Employee"/> in the UI circuit.
/// </summary>
public interface IUserSession
{
    /// <summary>
    /// Raised when authentication state changes (sign-in, sign-out, or restored session).
    /// </summary>
    event Action? AuthenticationStateChanged;

    /// <summary>
    /// Signed-in user name from claims (<c>preferred_username</c>, then <c>name</c>, then
    /// <see cref="System.Security.Principal.IIdentity.Name"/>), or null when anonymous.
    /// </summary>
    Task<string?> GetCurrentUserNameAsync();

    /// <summary>
    /// Employee for the signed-in user, or null when anonymous.
    /// Throws <see cref="InvalidEmployeeLoginException"/> when a user name is present but no valid employee exists.
    /// </summary>
    Task<Employee?> GetCurrentUserAsync();

    /// <summary>
    /// Persists the selected username and marks the circuit authenticated.
    /// </summary>
    Task SignInAsync(string username);

    /// <summary>
    /// Clears the persisted login for this circuit.
    /// </summary>
    Task SignOutAsync();
}
