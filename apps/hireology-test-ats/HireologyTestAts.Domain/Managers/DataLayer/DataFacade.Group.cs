namespace HireologyTestAts.Domain;

internal sealed partial class DataFacade
{
    private GroupDataManager? _groupDataManager;
    private GroupDataManager GroupDataManager => _groupDataManager ??= new GroupDataManager(_dbConnectionString);

    public async Task<IReadOnlyList<Group>> GetGroups(bool excludeTestData = false)
    {
        return await GroupDataManager.ListAsync(excludeTestData).ConfigureAwait(false);
    }

    public async Task<Group?> GetGroupById(Guid id)
    {
        return await GroupDataManager.GetByIdAsync(id).ConfigureAwait(false);
    }

    public async Task<Group> CreateGroup(Group group)
    {
        return await GroupDataManager.CreateAsync(group).ConfigureAwait(false);
    }

    public async Task<Group?> UpdateGroup(Group group)
    {
        return await GroupDataManager.UpdateAsync(group).ConfigureAwait(false);
    }

    public async Task<bool> DeleteGroup(Guid id)
    {
        return await GroupDataManager.DeleteAsync(id).ConfigureAwait(false);
    }
}
