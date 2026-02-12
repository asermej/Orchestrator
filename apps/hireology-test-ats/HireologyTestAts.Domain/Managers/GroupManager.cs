namespace HireologyTestAts.Domain;

internal sealed class GroupManager : IDisposable
{
    private readonly DataFacade _dataFacade;
    private bool _disposed;

    public GroupManager(ServiceLocatorBase serviceLocator)
    {
        var configProvider = serviceLocator.CreateConfigurationProvider();
        _dataFacade = new DataFacade(configProvider.GetDbConnectionString());
    }

    public async Task<IReadOnlyList<Group>> GetGroups()
    {
        return await _dataFacade.GetGroups().ConfigureAwait(false);
    }

    public async Task<Group> GetGroupById(Guid id)
    {
        var group = await _dataFacade.GetGroupById(id).ConfigureAwait(false);
        if (group == null) throw new GroupNotFoundException();
        return group;
    }

    public async Task<Group> CreateGroup(Group group)
    {
        if (string.IsNullOrWhiteSpace(group.Name))
            throw new GroupValidationException("Name is required");

        group.Name = group.Name.Trim();
        return await _dataFacade.CreateGroup(group).ConfigureAwait(false);
    }

    public async Task<Group> UpdateGroup(Guid id, Group updates)
    {
        var existing = await _dataFacade.GetGroupById(id).ConfigureAwait(false);
        if (existing == null) throw new GroupNotFoundException();

        if (!string.IsNullOrWhiteSpace(updates.Name)) existing.Name = updates.Name.Trim();

        var updated = await _dataFacade.UpdateGroup(existing).ConfigureAwait(false);
        if (updated == null) throw new GroupNotFoundException();
        return updated;
    }

    public async Task<bool> DeleteGroup(Guid id)
    {
        var deleted = await _dataFacade.DeleteGroup(id).ConfigureAwait(false);
        if (!deleted) throw new GroupNotFoundException();
        return true;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}
