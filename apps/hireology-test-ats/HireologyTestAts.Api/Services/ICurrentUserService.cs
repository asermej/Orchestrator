using HireologyTestAts.Api.Models;

namespace HireologyTestAts.Api.Services;

/// <summary>
/// Resolves the current authenticated user from the request (Auth0 JWT).
/// On first login, creates the user in the database.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user. Returns null if not authenticated.
    /// If the user exists in Auth0 but not in our DB, creates them (first-login).
    /// </summary>
    Task<UserItem?> GetCurrentUserAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the Auth0 sub claim from the current request, or null if not authenticated.
    /// </summary>
    string? GetAuth0Sub();
}
