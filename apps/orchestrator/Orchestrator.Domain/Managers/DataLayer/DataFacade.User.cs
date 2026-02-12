namespace Orchestrator.Domain;

internal sealed partial class DataFacade
{
    private UserDataManager UserDataManager => new(_dbConnectionString);

    public Task<User> AddUser(User user)
    {
        return UserDataManager.Add(user);
    }

    public async Task<User?> GetUserById(System.Guid id)
    {
        return await UserDataManager.GetById(id);
    }

    public async Task<User?> GetUserByAuth0Sub(string auth0Sub)
    {
        return await UserDataManager.GetByAuth0Sub(auth0Sub);
    }
    
    public Task<User> UpdateUser(User user)
    {
        return UserDataManager.Update(user);
    }

    public Task<bool> DeleteUser(System.Guid id)
    {
        return UserDataManager.Delete(id);
    }

    public Task<PaginatedResult<User>> SearchUsers(string? phone, string? email, string? lastName, int pageNumber, int pageSize)
    {
        return UserDataManager.Search(phone, email, lastName, pageNumber, pageSize);
    }
} 