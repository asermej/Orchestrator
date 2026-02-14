namespace HireologyTestAts.Domain;

public sealed partial class DomainFacade
{
    public async Task<IReadOnlyList<Organization>> GetOrganizations(Guid? groupId = null)
    {
        return await OrganizationManager.GetOrganizations(groupId).ConfigureAwait(false);
    }

    public async Task<Organization> GetOrganizationById(Guid id)
    {
        return await OrganizationManager.GetOrganizationById(id).ConfigureAwait(false);
    }

    public async Task<Organization> CreateOrganization(Organization org)
    {
        return await OrganizationManager.CreateOrganization(org).ConfigureAwait(false);
    }

    public async Task<Organization> UpdateOrganization(Guid id, Organization updates)
    {
        return await OrganizationManager.UpdateOrganization(id, updates).ConfigureAwait(false);
    }

    public async Task<Organization> MoveOrganization(Guid id, Guid? newParentOrganizationId)
    {
        return await OrganizationManager.MoveOrganization(id, newParentOrganizationId).ConfigureAwait(false);
    }

    public async Task<bool> DeleteOrganization(Guid id)
    {
        return await OrganizationManager.DeleteOrganization(id).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Organization>> GetOrganizationTree(Guid groupId)
    {
        return await OrganizationManager.GetOrganizationTree(groupId).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Organization>> GetChildOrganizations(Guid parentOrganizationId)
    {
        return await OrganizationManager.GetChildOrganizations(parentOrganizationId).ConfigureAwait(false);
    }
}
