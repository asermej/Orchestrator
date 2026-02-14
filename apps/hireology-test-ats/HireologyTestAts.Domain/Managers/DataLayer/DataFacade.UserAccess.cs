namespace HireologyTestAts.Domain;

internal sealed partial class DataFacade
{
    private UserAccessDataManager? _userAccessDataManager;
    private UserAccessDataManager UserAccessDataManager => _userAccessDataManager ??= new UserAccessDataManager(_dbConnectionString);

    public async Task SetGroupAccess(Guid userId, IReadOnlyList<Guid> groupIds)
    {
        await UserAccessDataManager.SetGroupAccessAsync(userId, groupIds).ConfigureAwait(false);
    }

    public async Task SetOrganizationAccess(Guid userId, IReadOnlyList<Guid> organizationIds)
    {
        await UserAccessDataManager.SetOrganizationAccessAsync(userId, organizationIds).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Guid>> GetUserGroupIds(Guid userId)
    {
        return await UserAccessDataManager.GetGroupIdsAsync(userId).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Guid>> GetUserOrganizationIds(Guid userId)
    {
        return await UserAccessDataManager.GetOrganizationIdsAsync(userId).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Guid>> GetAllowedOrganizationIds(Guid userId)
    {
        return await UserAccessDataManager.GetAllowedOrganizationIdsAsync(userId).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Group>> GetAccessibleGroups(Guid userId)
    {
        return await UserAccessDataManager.GetAccessibleGroupsAsync(userId).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Organization>> GetAccessibleOrganizations(Guid userId)
    {
        return await UserAccessDataManager.GetAccessibleOrganizationsAsync(userId).ConfigureAwait(false);
    }

    public async Task SetGroupAccessWithAdmin(Guid userId, IReadOnlyList<GroupAccessEntry> entries)
    {
        await UserAccessDataManager.SetGroupAccessWithAdminAsync(userId, entries).ConfigureAwait(false);
    }

    public async Task AddGroupAccess(Guid userId, Guid groupId, bool isAdmin)
    {
        await UserAccessDataManager.AddGroupAccessAsync(userId, groupId, isAdmin).ConfigureAwait(false);
    }

    public async Task RemoveGroupAccess(Guid userId, Guid groupId)
    {
        await UserAccessDataManager.RemoveGroupAccessAsync(userId, groupId).ConfigureAwait(false);
    }

    public async Task SetOrganizationAccessWithFlags(Guid userId, IReadOnlyList<OrganizationAccessEntry> entries)
    {
        await UserAccessDataManager.SetOrganizationAccessWithFlagsAsync(userId, entries).ConfigureAwait(false);
    }

    public async Task<bool> IsGroupAdmin(Guid userId, Guid groupId)
    {
        return await UserAccessDataManager.IsGroupAdminAsync(userId, groupId).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Guid>> GetGroupAdminGroupIds(Guid userId)
    {
        return await UserAccessDataManager.GetGroupAdminGroupIdsAsync(userId).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<OrganizationAccessEntry>> GetOrganizationAccessEntries(Guid userId)
    {
        return await UserAccessDataManager.GetOrganizationAccessEntriesAsync(userId).ConfigureAwait(false);
    }
}
