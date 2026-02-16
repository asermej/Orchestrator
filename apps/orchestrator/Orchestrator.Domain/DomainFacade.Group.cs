namespace Orchestrator.Domain;

public sealed partial class DomainFacade
{
    /// <summary>
    /// Creates a new Group
    /// </summary>
    public async Task<Group> CreateGroup(Group group)
    {
        return await GroupManager.CreateGroup(group).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a Group by ID
    /// </summary>
    public async Task<Group?> GetGroupById(Guid id)
    {
        return await GroupManager.GetGroupById(id).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a Group by its external (ATS) group ID
    /// </summary>
    public async Task<Group?> GetGroupByExternalGroupId(Guid externalGroupId)
    {
        return await GroupManager.GetGroupByExternalGroupId(externalGroupId).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a Group by API key (for authentication)
    /// </summary>
    public async Task<Group?> GetGroupByApiKey(string apiKey)
    {
        return await GroupManager.GetGroupByApiKey(apiKey).ConfigureAwait(false);
    }

    /// <summary>
    /// Searches for Groups
    /// </summary>
    public async Task<PaginatedResult<Group>> SearchGroups(string? name, bool? isActive, int pageNumber, int pageSize)
    {
        return await GroupManager.SearchGroups(name, isActive, pageNumber, pageSize).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates a Group
    /// </summary>
    public async Task<Group> UpdateGroup(Group group)
    {
        return await GroupManager.UpdateGroup(group).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes a Group
    /// </summary>
    public async Task<bool> DeleteGroup(Guid id)
    {
        return await GroupManager.DeleteGroup(id).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets or creates a default group for deployments without multi-tenancy configured
    /// </summary>
    public async Task<Group> GetOrCreateDefaultGroup()
    {
        return await GroupManager.GetOrCreateDefaultGroup().ConfigureAwait(false);
    }

    /// <summary>
    /// Creates or updates a group by its external (ATS) group ID.
    /// Returns the upserted group including the API key.
    /// </summary>
    public async Task<Group> UpsertGroupByExternalId(Guid externalGroupId, string name, string? atsBaseUrl)
    {
        return await GroupManager.UpsertGroupByExternalId(externalGroupId, name, atsBaseUrl).ConfigureAwait(false);
    }
}
