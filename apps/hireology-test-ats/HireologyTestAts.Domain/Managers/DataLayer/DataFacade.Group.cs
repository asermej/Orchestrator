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

    public async Task UpdateOrchestratorApiKey(Guid groupId, string apiKey)
    {
        await GroupDataManager.UpdateOrchestratorApiKey(groupId, apiKey).ConfigureAwait(false);
    }

    /// <summary>
    /// Resolves the Orchestrator API key for a given organization by looking up its parent group.
    /// Returns null if the organization or group is not found, or if no key is stored.
    /// </summary>
    public async Task<string?> GetOrchestratorApiKeyForOrganization(Guid organizationId)
    {
        var org = await OrganizationDataManager.GetByIdAsync(organizationId).ConfigureAwait(false);
        if (org == null) return null;

        var group = await GroupDataManager.GetByIdAsync(org.GroupId).ConfigureAwait(false);
        return group?.OrchestratorApiKey;
    }
}
