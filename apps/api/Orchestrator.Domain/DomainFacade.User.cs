using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

public sealed partial class DomainFacade
{
    public async Task<User> CreateUser(User user)
    {
        return await UserManager.CreateUser(user);
    }

    public async Task<User?> GetUserById(Guid id)
    {
        return await UserManager.GetUserById(id);
    }

    public async Task<User?> GetUserByAuth0Sub(string auth0Sub)
    {
        return await UserManager.GetUserByAuth0Sub(auth0Sub);
    }

    public async Task<PaginatedResult<User>> SearchUsers(string? phone, string? email, string? lastName, int pageNumber, int pageSize)
    {
        return await UserManager.SearchUsers(phone, email, lastName, pageNumber, pageSize);
    }

    public async Task<User> UpdateUser(User user)
    {
        return await UserManager.UpdateUser(user);
    }

    public async Task<bool> DeleteUser(Guid id)
    {
        return await UserManager.DeleteUser(id);
    }
} 