using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Orchestrator.Domain;

/// <summary>
/// Manages business operations for User entities
/// </summary>
internal sealed class UserManager : IDisposable
{
    private readonly ServiceLocatorBase _serviceLocator;
    private DataFacade? _dataFacade;
    private DataFacade DataFacade => _dataFacade ??= new DataFacade(_serviceLocator.CreateConfigurationProvider().GetDbConnectionString());

    public UserManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    /// <summary>
    /// Creates a new User
    /// </summary>
    /// <param name="user">The User entity to create</param>
    /// <returns>The created User</returns>
    public async Task<User> CreateUser(User user)
    {
        UserValidator.Validate(user);
        
        // Check for duplicate email
        var existingUserWithEmail = await DataFacade.SearchUsers(null, user.Email, null, 1, 1).ConfigureAwait(false);
        if (existingUserWithEmail.Items.Any())
        {
            throw new UserDuplicateEmailException($"A user with email '{user.Email}' already exists.");
        }

        // Check for duplicate auth0_sub
        if (!string.IsNullOrWhiteSpace(user.Auth0Sub))
        {
            var existingUserWithAuth0Sub = await DataFacade.GetUserByAuth0Sub(user.Auth0Sub).ConfigureAwait(false);
            if (existingUserWithAuth0Sub != null)
            {
                throw new UserDuplicateEmailException($"A user with Auth0 ID '{user.Auth0Sub}' already exists.");
            }
        }
        
        return await DataFacade.AddUser(user).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a User by ID
    /// </summary>
    /// <param name="id">The ID of the User to get</param>
    /// <returns>The User if found, null otherwise</returns>
    public async Task<User?> GetUserById(Guid id)
    {
        return await DataFacade.GetUserById(id).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a User by Auth0 Sub
    /// </summary>
    /// <param name="auth0Sub">The Auth0 Sub of the User to get</param>
    /// <returns>The User if found, null otherwise</returns>
    public async Task<User?> GetUserByAuth0Sub(string auth0Sub)
    {
        return await DataFacade.GetUserByAuth0Sub(auth0Sub).ConfigureAwait(false);
    }

    /// <summary>
    /// Searches for Users
    /// </summary>
    /// <param name="phone">Optional phone number to search for</param>
    /// <param name="email">Optional email to search for</param>
    /// <param name="lastName">Optional last name to search for</param>
    /// <param name="pageNumber">Page number for pagination</param>
    /// <param name="pageSize">Page size for pagination</param>
    /// <returns>A paginated list of Users</returns>
    public async Task<PaginatedResult<User>> SearchUsers(string? phone, string? email, string? lastName, int pageNumber, int pageSize)
    {
        return await DataFacade.SearchUsers(phone, email, lastName, pageNumber, pageSize).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates a User
    /// </summary>
    /// <param name="user">The User entity with updated data</param>
    /// <returns>The updated User</returns>
    public async Task<User> UpdateUser(User user)
    {
        UserValidator.Validate(user);
        
        // Check for duplicate email (excluding the current user)
        var existingUserWithEmail = await DataFacade.SearchUsers(null, user.Email, null, 1, 1).ConfigureAwait(false);
        var duplicateUser = existingUserWithEmail.Items.FirstOrDefault();
        if (duplicateUser != null && duplicateUser.Id != user.Id)
        {
            throw new UserDuplicateEmailException($"A user with email '{user.Email}' already exists.");
        }

        // Check for duplicate auth0_sub (excluding the current user)
        if (!string.IsNullOrWhiteSpace(user.Auth0Sub))
        {
            var existingUserWithAuth0Sub = await DataFacade.GetUserByAuth0Sub(user.Auth0Sub).ConfigureAwait(false);
            if (existingUserWithAuth0Sub != null && existingUserWithAuth0Sub.Id != user.Id)
            {
                throw new UserDuplicateEmailException($"A user with Auth0 ID '{user.Auth0Sub}' already exists.");
            }
        }
        
        return await DataFacade.UpdateUser(user).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes a User
    /// </summary>
    /// <param name="id">The ID of the User to delete</param>
    /// <returns>True if the User was deleted, false if not found</returns>
    public async Task<bool> DeleteUser(Guid id)
    {
        return await DataFacade.DeleteUser(id).ConfigureAwait(false);
    }

    public void Dispose()
    {
        // DataFacade doesn't implement IDisposable, so no disposal needed
    }
} 