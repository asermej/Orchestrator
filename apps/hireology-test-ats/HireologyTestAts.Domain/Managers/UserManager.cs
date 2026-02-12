namespace HireologyTestAts.Domain;

internal sealed class UserManager : IDisposable
{
    private readonly DataFacade _dataFacade;
    private readonly OrchestratorGateway _orchestratorGateway;
    private bool _disposed;

    public UserManager(ServiceLocatorBase serviceLocator)
    {
        var configProvider = serviceLocator.CreateConfigurationProvider();
        _dataFacade = new DataFacade(configProvider.GetDbConnectionString());
        _orchestratorGateway = new OrchestratorGateway(
            configProvider.GetOrchestratorBaseUrl(),
            configProvider.GetOrchestratorApiKey());
    }

    public async Task<IReadOnlyList<User>> GetUsers(int pageNumber, int pageSize)
    {
        return await _dataFacade.GetUsers(pageNumber, pageSize).ConfigureAwait(false);
    }

    public async Task<int> GetUserCount()
    {
        return await _dataFacade.GetUserCount().ConfigureAwait(false);
    }

    public async Task<User> GetUserById(Guid id)
    {
        var user = await _dataFacade.GetUserById(id).ConfigureAwait(false);
        if (user == null) throw new UserNotFoundException();
        return user;
    }

    public async Task<User?> GetUserByAuth0Sub(string auth0Sub)
    {
        if (string.IsNullOrEmpty(auth0Sub)) return null;
        return await _dataFacade.GetUserByAuth0Sub(auth0Sub).ConfigureAwait(false);
    }

    public async Task<User> GetOrCreateUser(string auth0Sub, string? email, string? name)
    {
        if (string.IsNullOrEmpty(auth0Sub))
            throw new UserValidationException("Auth0 sub is required");

        var existing = await _dataFacade.GetUserByAuth0Sub(auth0Sub).ConfigureAwait(false);
        if (existing != null) return existing;

        var user = new User
        {
            Auth0Sub = auth0Sub,
            Email = email,
            Name = name
        };
        user = await _dataFacade.CreateUser(user).ConfigureAwait(false);

        try
        {
            await _orchestratorGateway.ProvisionUserAsync(auth0Sub, email, name).ConfigureAwait(false);
        }
        catch
        {
            // Non-fatal: test-ats user is created; Orchestrator provisioning can be retried later
        }

        return user;
    }

    public async Task<User> UpdateUser(Guid id, User updates)
    {
        var existing = await _dataFacade.GetUserById(id).ConfigureAwait(false);
        if (existing == null) throw new UserNotFoundException();

        if (updates.Email != null) existing.Email = updates.Email.Trim();
        if (updates.Name != null) existing.Name = updates.Name.Trim();

        var updated = await _dataFacade.UpdateUser(existing).ConfigureAwait(false);
        if (updated == null) throw new UserNotFoundException();
        return updated;
    }

    // User access operations

    public async Task<IReadOnlyList<Guid>> GetUserGroupIds(Guid userId)
    {
        return await _dataFacade.GetUserGroupIds(userId).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Guid>> GetUserOrganizationIds(Guid userId)
    {
        return await _dataFacade.GetUserOrganizationIds(userId).ConfigureAwait(false);
    }

    public async Task SetUserAccess(Guid userId, IReadOnlyList<Guid>? groupIds, IReadOnlyList<Guid>? organizationIds)
    {
        var user = await _dataFacade.GetUserById(userId).ConfigureAwait(false);
        if (user == null) throw new UserNotFoundException();

        await _dataFacade.SetGroupAccess(userId, groupIds ?? Array.Empty<Guid>()).ConfigureAwait(false);
        await _dataFacade.SetOrganizationAccess(userId, organizationIds ?? Array.Empty<Guid>()).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Guid>> GetAllowedOrganizationIds(Guid userId)
    {
        return await _dataFacade.GetAllowedOrganizationIds(userId).ConfigureAwait(false);
    }

    public async Task<bool> CanAccessOrganization(Guid userId, Guid organizationId)
    {
        var allowed = await GetAllowedOrganizationIds(userId).ConfigureAwait(false);
        return allowed.Contains(organizationId);
    }

    public async Task<IReadOnlyList<Group>> GetAccessibleGroups(Guid userId)
    {
        return await _dataFacade.GetAccessibleGroups(userId).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Organization>> GetAccessibleOrganizations(Guid userId)
    {
        return await _dataFacade.GetAccessibleOrganizations(userId).ConfigureAwait(false);
    }

    // User session operations

    public async Task<Guid?> GetSelectedOrganizationId(Guid userId)
    {
        return await _dataFacade.GetSelectedOrganizationId(userId).ConfigureAwait(false);
    }

    public async Task SetSelectedOrganizationId(Guid userId, Guid? organizationId)
    {
        if (organizationId.HasValue)
        {
            var canAccess = await CanAccessOrganization(userId, organizationId.Value).ConfigureAwait(false);
            if (!canAccess)
                throw new AccessDeniedException("You do not have access to this organization.");
        }

        await _dataFacade.SetSelectedOrganizationId(userId, organizationId).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _orchestratorGateway.Dispose();
            _disposed = true;
        }
    }
}
