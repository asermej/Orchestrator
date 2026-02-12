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
}
