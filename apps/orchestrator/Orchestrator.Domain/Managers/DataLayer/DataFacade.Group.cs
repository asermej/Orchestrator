namespace Orchestrator.Domain;

internal sealed partial class DataFacade
{
    private GroupDataManager GroupDataManager => new(_dbConnectionString);

    public Task<Group> AddGroup(Group group)
    {
        return GroupDataManager.Add(group);
    }

    public async Task<Group?> GetGroupById(Guid id)
    {
        return await GroupDataManager.GetById(id);
    }

    public async Task<Group?> GetGroupByExternalGroupId(Guid externalGroupId)
    {
        return await GroupDataManager.GetByExternalGroupId(externalGroupId);
    }

    public async Task<Group> UpsertGroupByExternalId(Guid externalGroupId, string name, string? atsBaseUrl, string? webhookUrl, string? atsApiKey = null)
    {
        return await GroupDataManager.Upsert(externalGroupId, name, atsBaseUrl, webhookUrl, atsApiKey);
    }

    // Alias for WebhookManager
    public Task<Group?> GetGroupByIdAsync(Guid id) => GetGroupById(id);

    public async Task<Group?> GetGroupByApiKey(string apiKey)
    {
        return await GroupDataManager.GetByApiKey(apiKey);
    }

    public Task<Group> UpdateGroup(Group group)
    {
        return GroupDataManager.Update(group);
    }

    public Task<bool> DeleteGroup(Guid id)
    {
        return GroupDataManager.Delete(id);
    }

    public Task<PaginatedResult<Group>> SearchGroups(string? name, bool? isActive, int pageNumber, int pageSize)
    {
        return GroupDataManager.Search(name, isActive, pageNumber, pageSize);
    }
}
