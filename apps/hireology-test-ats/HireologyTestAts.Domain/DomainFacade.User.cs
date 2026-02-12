namespace HireologyTestAts.Domain;

public sealed partial class DomainFacade
{
    // User CRUD

    public async Task<IReadOnlyList<User>> GetUsers(int pageNumber, int pageSize)
    {
        return await UserManager.GetUsers(pageNumber, pageSize).ConfigureAwait(false);
    }

    public async Task<int> GetUserCount()
    {
        return await UserManager.GetUserCount().ConfigureAwait(false);
    }

    public async Task<User> GetUserById(Guid id)
    {
        return await UserManager.GetUserById(id).ConfigureAwait(false);
    }

    public async Task<User?> GetUserByAuth0Sub(string auth0Sub)
    {
        return await UserManager.GetUserByAuth0Sub(auth0Sub).ConfigureAwait(false);
    }

    public async Task<User> GetOrCreateUser(string auth0Sub, string? email, string? name)
    {
        return await UserManager.GetOrCreateUser(auth0Sub, email, name).ConfigureAwait(false);
    }

    public async Task<User> UpdateUser(Guid id, User updates)
    {
        return await UserManager.UpdateUser(id, updates).ConfigureAwait(false);
    }

    // User access

    public async Task<IReadOnlyList<Guid>> GetUserGroupIds(Guid userId)
    {
        return await UserManager.GetUserGroupIds(userId).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Guid>> GetUserOrganizationIds(Guid userId)
    {
        return await UserManager.GetUserOrganizationIds(userId).ConfigureAwait(false);
    }

    public async Task SetUserAccess(Guid userId, IReadOnlyList<Guid>? groupIds, IReadOnlyList<Guid>? organizationIds)
    {
        await UserManager.SetUserAccess(userId, groupIds, organizationIds).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Guid>> GetAllowedOrganizationIds(Guid userId)
    {
        return await UserManager.GetAllowedOrganizationIds(userId).ConfigureAwait(false);
    }

    public async Task<bool> CanAccessOrganization(Guid userId, Guid organizationId)
    {
        return await UserManager.CanAccessOrganization(userId, organizationId).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Group>> GetAccessibleGroups(Guid userId)
    {
        return await UserManager.GetAccessibleGroups(userId).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Organization>> GetAccessibleOrganizations(Guid userId)
    {
        return await UserManager.GetAccessibleOrganizations(userId).ConfigureAwait(false);
    }

    // User sessions

    public async Task<Guid?> GetSelectedOrganizationId(Guid userId)
    {
        return await UserManager.GetSelectedOrganizationId(userId).ConfigureAwait(false);
    }

    public async Task SetSelectedOrganizationId(Guid userId, Guid? organizationId)
    {
        await UserManager.SetSelectedOrganizationId(userId, organizationId).ConfigureAwait(false);
    }
}
