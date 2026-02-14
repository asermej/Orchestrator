namespace HireologyTestAts.Domain;

internal sealed partial class DataFacade
{
    private UserDataManager? _userDataManager;
    private UserDataManager UserDataManager => _userDataManager ??= new UserDataManager(_dbConnectionString);

    public async Task<User?> GetUserByAuth0Sub(string auth0Sub)
    {
        return await UserDataManager.GetByAuth0SubAsync(auth0Sub).ConfigureAwait(false);
    }

    public async Task<User?> GetUserById(Guid id)
    {
        return await UserDataManager.GetByIdAsync(id).ConfigureAwait(false);
    }

    public async Task<User> CreateUser(User user)
    {
        return await UserDataManager.CreateAsync(user).ConfigureAwait(false);
    }

    public async Task<User?> UpdateUser(User user)
    {
        return await UserDataManager.UpdateAsync(user).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<User>> GetUsers(int pageNumber, int pageSize)
    {
        return await UserDataManager.ListAsync(pageNumber, pageSize).ConfigureAwait(false);
    }

    public async Task<int> GetUserCount()
    {
        return await UserDataManager.CountAsync().ConfigureAwait(false);
    }

    public async Task<bool> SetSuperadmin(Guid userId, bool isSuperadmin)
    {
        return await UserDataManager.SetSuperadminAsync(userId, isSuperadmin).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<User>> GetSuperadmins()
    {
        return await UserDataManager.GetSuperadminsAsync().ConfigureAwait(false);
    }

    public async Task<User?> GetUserByEmail(string email)
    {
        return await UserDataManager.GetByEmailAsync(email).ConfigureAwait(false);
    }

    public async Task<User?> UpdateAuth0Sub(Guid userId, string auth0Sub, string? name)
    {
        return await UserDataManager.UpdateAuth0SubAsync(userId, auth0Sub, name).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<User>> GetUsersByGroup(Guid groupId)
    {
        return await UserDataManager.GetUsersByGroupAsync(groupId).ConfigureAwait(false);
    }
}
