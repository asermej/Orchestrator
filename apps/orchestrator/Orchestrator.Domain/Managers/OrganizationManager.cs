namespace Orchestrator.Domain;

/// <summary>
/// Manages business operations for Organization entities
/// </summary>
internal sealed class OrganizationManager : IDisposable
{
    private readonly ServiceLocatorBase _serviceLocator;
    private DataFacade? _dataFacade;
    private DataFacade DataFacade => _dataFacade ??= new DataFacade(_serviceLocator.CreateConfigurationProvider().GetDbConnectionString());

    public OrganizationManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    public async Task<Organization> CreateOrganization(Organization organization)
    {
        // Generate API key if not provided
        if (string.IsNullOrEmpty(organization.ApiKey))
        {
            organization.ApiKey = GenerateApiKey();
        }

        OrganizationValidator.Validate(organization);
        return await DataFacade.AddOrganization(organization).ConfigureAwait(false);
    }

    public async Task<Organization?> GetOrganizationById(Guid id)
    {
        return await DataFacade.GetOrganizationById(id).ConfigureAwait(false);
    }

    public async Task<Organization?> GetOrganizationByApiKey(string apiKey)
    {
        return await DataFacade.GetOrganizationByApiKey(apiKey).ConfigureAwait(false);
    }

    public async Task<PaginatedResult<Organization>> SearchOrganizations(string? name, bool? isActive, int pageNumber, int pageSize)
    {
        return await DataFacade.SearchOrganizations(name, isActive, pageNumber, pageSize).ConfigureAwait(false);
    }

    public async Task<Organization> UpdateOrganization(Organization organization)
    {
        OrganizationValidator.Validate(organization);
        return await DataFacade.UpdateOrganization(organization).ConfigureAwait(false);
    }

    public async Task<bool> DeleteOrganization(Guid id)
    {
        return await DataFacade.DeleteOrganization(id).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets or creates a default organization for the deployment.
    /// Used when multi-tenancy is not configured or for demo purposes.
    /// </summary>
    public async Task<Organization> GetOrCreateDefaultOrganization()
    {
        const string defaultOrgName = "Hireology";
        
        // Search for existing default organization
        var searchResult = await DataFacade.SearchOrganizations(defaultOrgName, true, 1, 1).ConfigureAwait(false);
        if (searchResult.Items.Any())
        {
            return searchResult.Items.First();
        }
        
        // Create default organization if it doesn't exist
        var defaultOrg = new Organization
        {
            Name = defaultOrgName,
            ApiKey = GenerateApiKey(),
            IsActive = true
        };
        
        return await DataFacade.AddOrganization(defaultOrg).ConfigureAwait(false);
    }

    private static string GenerateApiKey()
    {
        // Generate a secure API key using URL-safe base64
        var bytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        // Use hex encoding for consistent length
        var hex = BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        return $"org_{hex[..32]}";
    }

    public void Dispose()
    {
        // DataFacade doesn't implement IDisposable, so no disposal needed
    }
}
