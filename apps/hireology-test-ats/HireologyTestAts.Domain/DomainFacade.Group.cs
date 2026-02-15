namespace HireologyTestAts.Domain;

public sealed partial class DomainFacade
{
    public async Task<IReadOnlyList<Group>> GetGroups(bool excludeTestData = false)
    {
        return await GroupManager.GetGroups(excludeTestData).ConfigureAwait(false);
    }

    public async Task<Group> GetGroupById(Guid id)
    {
        return await GroupManager.GetGroupById(id).ConfigureAwait(false);
    }

    public async Task<Group> CreateGroup(Group group, string? adminEmail = null)
    {
        return await GroupManager.CreateGroup(group, adminEmail).ConfigureAwait(false);
    }

    public async Task<Group> UpdateGroup(Guid id, Group updates)
    {
        return await GroupManager.UpdateGroup(id, updates).ConfigureAwait(false);
    }

    public async Task<bool> DeleteGroup(Guid id)
    {
        return await GroupManager.DeleteGroup(id).ConfigureAwait(false);
    }
}
