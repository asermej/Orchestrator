namespace HireologyTestAts.Domain;

internal sealed class GroupManager : IDisposable
{
    private readonly DataFacade _dataFacade;
    private readonly GatewayFacade _gatewayFacade;
    private bool _disposed;

    public GroupManager(ServiceLocatorBase serviceLocator, GatewayFacade gatewayFacade)
    {
        var configProvider = serviceLocator.CreateConfigurationProvider();
        _dataFacade = new DataFacade(configProvider.GetDbConnectionString());
        _gatewayFacade = gatewayFacade ?? throw new ArgumentNullException(nameof(gatewayFacade));
    }

    public async Task<IReadOnlyList<Group>> GetGroups(bool excludeTestData = false)
    {
        return await _dataFacade.GetGroups(excludeTestData).ConfigureAwait(false);
    }

    public async Task<Group> GetGroupById(Guid id)
    {
        var group = await _dataFacade.GetGroupById(id).ConfigureAwait(false);
        if (group == null) throw new GroupNotFoundException();
        return group;
    }

    public async Task<Group> CreateGroup(Group group, string? adminEmail = null)
    {
        if (string.IsNullOrWhiteSpace(group.Name))
            throw new GroupValidationException("Name is required");

        group.Name = group.Name.Trim();

        // Create the group first (without root org)
        var createdGroup = await _dataFacade.CreateGroup(group).ConfigureAwait(false);

        // Auto-create a root organization for the group
        var rootOrg = new Organization
        {
            GroupId = createdGroup.Id,
            Name = createdGroup.Name,
            ParentOrganizationId = null
        };
        var createdRootOrg = await _dataFacade.CreateOrganization(rootOrg).ConfigureAwait(false);

        // Link the root org back to the group
        createdGroup.RootOrganizationId = createdRootOrg.Id;
        var updatedGroup = await _dataFacade.UpdateGroup(createdGroup).ConfigureAwait(false);

        // If an admin email is provided, create/find the user and assign as group admin
        if (!string.IsNullOrWhiteSpace(adminEmail))
        {
            adminEmail = adminEmail.Trim().ToLowerInvariant();
            var adminUser = await _dataFacade.GetUserByEmail(adminEmail).ConfigureAwait(false);
            if (adminUser == null)
            {
                adminUser = new User
                {
                    Auth0Sub = string.Empty,
                    Email = adminEmail,
                    Name = null
                };
                adminUser = await _dataFacade.CreateUser(adminUser).ConfigureAwait(false);
            }
            await _dataFacade.AddGroupAccess(adminUser.Id, createdGroup.Id, isAdmin: true).ConfigureAwait(false);
        }

        var result = updatedGroup ?? createdGroup;

        // Sync group to Orchestrator and save the returned API key
        try
        {
            var syncResult = await _gatewayFacade.SyncGroup(result).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(syncResult.ApiKey))
            {
                await _dataFacade.UpdateOrchestratorApiKey(result.Id, syncResult.ApiKey).ConfigureAwait(false);
                result.OrchestratorApiKey = syncResult.ApiKey;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to sync group to Orchestrator: {ex.Message}");
        }

        return result;
    }

    public async Task<Group> UpdateGroup(Guid id, Group updates)
    {
        var existing = await _dataFacade.GetGroupById(id).ConfigureAwait(false);
        if (existing == null) throw new GroupNotFoundException();

        if (!string.IsNullOrWhiteSpace(updates.Name)) existing.Name = updates.Name.Trim();

        var updated = await _dataFacade.UpdateGroup(existing).ConfigureAwait(false);
        if (updated == null) throw new GroupNotFoundException();

        // Sync updated group to Orchestrator and save the returned API key
        try
        {
            var syncResult = await _gatewayFacade.SyncGroup(updated).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(syncResult.ApiKey) && syncResult.ApiKey != updated.OrchestratorApiKey)
            {
                await _dataFacade.UpdateOrchestratorApiKey(updated.Id, syncResult.ApiKey).ConfigureAwait(false);
                updated.OrchestratorApiKey = syncResult.ApiKey;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to sync group update to Orchestrator: {ex.Message}");
        }

        return updated;
    }

    public async Task<bool> DeleteGroup(Guid id)
    {
        // First remove all organizations belonging to this group (including root org)
        var orgs = await _dataFacade.GetOrganizations(id).ConfigureAwait(false);

        // Clear root_organization_id first to avoid FK constraint issues
        var group = await _dataFacade.GetGroupById(id).ConfigureAwait(false);
        if (group != null && group.RootOrganizationId.HasValue)
        {
            group.RootOrganizationId = null;
            await _dataFacade.UpdateGroup(group).ConfigureAwait(false);
        }

        // Delete child orgs first (those with parent), then root orgs
        var childOrgs = orgs.Where(o => o.ParentOrganizationId.HasValue).ToList();
        var rootOrgs = orgs.Where(o => !o.ParentOrganizationId.HasValue).ToList();

        foreach (var org in childOrgs)
        {
            await _dataFacade.DeleteOrganization(org.Id).ConfigureAwait(false);
        }
        foreach (var org in rootOrgs)
        {
            await _dataFacade.DeleteOrganization(org.Id).ConfigureAwait(false);
        }

        var deleted = await _dataFacade.DeleteGroup(id).ConfigureAwait(false);
        if (!deleted) throw new GroupNotFoundException();
        return true;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}
