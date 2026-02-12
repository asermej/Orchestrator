namespace Orchestrator.Domain;

public sealed partial class DomainFacade
{
    /// <summary>
    /// Creates a new Organization
    /// </summary>
    public async Task<Organization> CreateOrganization(Organization organization)
    {
        return await OrganizationManager.CreateOrganization(organization).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets an Organization by ID
    /// </summary>
    public async Task<Organization?> GetOrganizationById(Guid id)
    {
        return await OrganizationManager.GetOrganizationById(id).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets an Organization by API key (for authentication)
    /// </summary>
    public async Task<Organization?> GetOrganizationByApiKey(string apiKey)
    {
        return await OrganizationManager.GetOrganizationByApiKey(apiKey).ConfigureAwait(false);
    }

    /// <summary>
    /// Searches for Organizations
    /// </summary>
    public async Task<PaginatedResult<Organization>> SearchOrganizations(string? name, bool? isActive, int pageNumber, int pageSize)
    {
        return await OrganizationManager.SearchOrganizations(name, isActive, pageNumber, pageSize).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates an Organization
    /// </summary>
    public async Task<Organization> UpdateOrganization(Organization organization)
    {
        return await OrganizationManager.UpdateOrganization(organization).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes an Organization
    /// </summary>
    public async Task<bool> DeleteOrganization(Guid id)
    {
        return await OrganizationManager.DeleteOrganization(id).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets or creates a default organization for deployments without multi-tenancy configured
    /// </summary>
    public async Task<Organization> GetOrCreateDefaultOrganization()
    {
        return await OrganizationManager.GetOrCreateDefaultOrganization().ConfigureAwait(false);
    }
}
