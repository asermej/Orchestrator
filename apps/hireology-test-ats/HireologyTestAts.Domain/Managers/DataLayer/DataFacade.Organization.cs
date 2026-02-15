namespace HireologyTestAts.Domain;

internal sealed partial class DataFacade
{
    private OrganizationDataManager? _organizationDataManager;
    private OrganizationDataManager OrganizationDataManager => _organizationDataManager ??= new OrganizationDataManager(_dbConnectionString);

    public async Task<IReadOnlyList<Organization>> GetOrganizations(Guid? groupId = null, bool excludeTestData = false)
    {
        return await OrganizationDataManager.ListAsync(groupId, excludeTestData).ConfigureAwait(false);
    }

    public async Task<Organization?> GetOrganizationById(Guid id)
    {
        return await OrganizationDataManager.GetByIdAsync(id).ConfigureAwait(false);
    }

    public async Task<Organization> CreateOrganization(Organization org)
    {
        return await OrganizationDataManager.CreateAsync(org).ConfigureAwait(false);
    }

    public async Task<Organization?> UpdateOrganization(Organization org)
    {
        return await OrganizationDataManager.UpdateAsync(org).ConfigureAwait(false);
    }

    public async Task<bool> DeleteOrganization(Guid id)
    {
        return await OrganizationDataManager.DeleteAsync(id).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Organization>> GetChildOrganizations(Guid parentOrganizationId)
    {
        return await OrganizationDataManager.GetChildrenAsync(parentOrganizationId).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Organization>> GetOrganizationTree(Guid groupId)
    {
        return await OrganizationDataManager.GetOrganizationTreeAsync(groupId).ConfigureAwait(false);
    }
}
