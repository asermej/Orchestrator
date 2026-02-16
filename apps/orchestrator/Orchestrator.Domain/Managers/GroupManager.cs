namespace Orchestrator.Domain;

/// <summary>
/// Manages business operations for Group entities
/// </summary>
internal sealed class GroupManager : IDisposable
{
    private readonly ServiceLocatorBase _serviceLocator;
    private DataFacade? _dataFacade;
    private DataFacade DataFacade => _dataFacade ??= new DataFacade(_serviceLocator.CreateConfigurationProvider().GetDbConnectionString());

    public GroupManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    public async Task<Group> CreateGroup(Group group)
    {
        if (string.IsNullOrEmpty(group.ApiKey))
        {
            group.ApiKey = GenerateApiKey();
        }

        GroupValidator.Validate(group);
        return await DataFacade.AddGroup(group).ConfigureAwait(false);
    }

    public async Task<Group?> GetGroupById(Guid id)
    {
        return await DataFacade.GetGroupById(id).ConfigureAwait(false);
    }

    public async Task<Group?> GetGroupByExternalGroupId(Guid externalGroupId)
    {
        return await DataFacade.GetGroupByExternalGroupId(externalGroupId).ConfigureAwait(false);
    }

    public async Task<Group?> GetGroupByApiKey(string apiKey)
    {
        return await DataFacade.GetGroupByApiKey(apiKey).ConfigureAwait(false);
    }

    public async Task<PaginatedResult<Group>> SearchGroups(string? name, bool? isActive, int pageNumber, int pageSize)
    {
        return await DataFacade.SearchGroups(name, isActive, pageNumber, pageSize).ConfigureAwait(false);
    }

    public async Task<Group> UpdateGroup(Group group)
    {
        GroupValidator.Validate(group);
        return await DataFacade.UpdateGroup(group).ConfigureAwait(false);
    }

    public async Task<bool> DeleteGroup(Guid id)
    {
        return await DataFacade.DeleteGroup(id).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets or creates a default group for the deployment.
    /// Used when multi-tenancy is not configured or for demo purposes.
    /// </summary>
    public async Task<Group> GetOrCreateDefaultGroup()
    {
        const string defaultGroupName = "Hireology";
        
        var searchResult = await DataFacade.SearchGroups(defaultGroupName, true, 1, 1).ConfigureAwait(false);
        if (searchResult.Items.Any())
        {
            return searchResult.Items.First();
        }
        
        var defaultGroup = new Group
        {
            Name = defaultGroupName,
            ApiKey = GenerateApiKey(),
            IsActive = true
        };
        
        return await DataFacade.AddGroup(defaultGroup).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates or updates a group by its external (ATS) group ID.
    /// If a group with the given external ID exists, updates its name and ATS base URL.
    /// If not, creates a new group with a generated API key.
    /// Returns the upserted group and the API key (callers need it for future requests).
    /// </summary>
    public async Task<Group> UpsertGroupByExternalId(Guid externalGroupId, string name, string? atsBaseUrl)
    {
        return await DataFacade.UpsertGroupByExternalId(externalGroupId, name, atsBaseUrl).ConfigureAwait(false);
    }

    private static string GenerateApiKey()
    {
        var bytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        var hex = BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        return $"grp_{hex[..32]}";
    }

    public void Dispose()
    {
    }
}
