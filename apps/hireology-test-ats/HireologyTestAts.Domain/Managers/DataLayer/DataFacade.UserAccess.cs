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
}
