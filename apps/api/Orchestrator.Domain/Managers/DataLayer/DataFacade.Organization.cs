namespace Orchestrator.Domain;

internal sealed partial class DataFacade
{
    private OrganizationDataManager OrganizationDataManager => new(_dbConnectionString);

    public Task<Organization> AddOrganization(Organization organization)
    {
        return OrganizationDataManager.Add(organization);
    }

    public async Task<Organization?> GetOrganizationById(Guid id)
    {
        return await OrganizationDataManager.GetById(id);
    }

    // Alias for WebhookManager
    public Task<Organization?> GetOrganizationByIdAsync(Guid id) => GetOrganizationById(id);

    public async Task<Organization?> GetOrganizationByApiKey(string apiKey)
    {
        return await OrganizationDataManager.GetByApiKey(apiKey);
    }

    public Task<Organization> UpdateOrganization(Organization organization)
    {
        return OrganizationDataManager.Update(organization);
    }

    public Task<bool> DeleteOrganization(Guid id)
    {
        return OrganizationDataManager.Delete(id);
    }

    public Task<PaginatedResult<Organization>> SearchOrganizations(string? name, bool? isActive, int pageNumber, int pageSize)
    {
        return OrganizationDataManager.Search(name, isActive, pageNumber, pageSize);
    }
}
