namespace HireologyTestAts.Domain;

internal sealed class UserManager : IDisposable
{
    private readonly DataFacade _dataFacade;
    private readonly GatewayFacade _gatewayFacade;
    private bool _disposed;

    public UserManager(ServiceLocatorBase serviceLocator, GatewayFacade gatewayFacade)
    {
        var configProvider = serviceLocator.CreateConfigurationProvider();
        _dataFacade = new DataFacade(configProvider.GetDbConnectionString());
        _gatewayFacade = gatewayFacade ?? throw new ArgumentNullException(nameof(gatewayFacade));
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
        if (existing != null)
        {
            // Sync email/name from Auth0 claims on each login
            var needsUpdate = false;
            if (!string.IsNullOrEmpty(email) && existing.Email != email) { existing.Email = email; needsUpdate = true; }
            if (!string.IsNullOrEmpty(name) && existing.Name != name) { existing.Name = name; needsUpdate = true; }
            if (needsUpdate)
            {
                var updated = await _dataFacade.UpdateUser(existing).ConfigureAwait(false);
                return updated ?? existing;
            }
            return existing;
        }

        // Check for a pre-provisioned user (invited by email before first login)
        if (!string.IsNullOrEmpty(email))
        {
            var preProvisioned = await _dataFacade.GetUserByEmail(email).ConfigureAwait(false);
            if (preProvisioned != null && string.IsNullOrEmpty(preProvisioned.Auth0Sub))
            {
                // Link the auth0Sub to the pre-provisioned user; their access is already configured
                var linked = await _dataFacade.UpdateAuth0Sub(preProvisioned.Id, auth0Sub, name).ConfigureAwait(false);
                return linked ?? preProvisioned;
            }
        }

        var user = new User
        {
            Auth0Sub = auth0Sub,
            Email = email,
            Name = name
        };
        user = await _dataFacade.CreateUser(user).ConfigureAwait(false);

        try
        {
            // User provisioning is group-agnostic; pass null to use global fallback key
            await _gatewayFacade.ProvisionUser(auth0Sub, email, name, null).ConfigureAwait(false);
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

    // Superadmin operations

    public async Task<bool> IsSuperadmin(Guid userId)
    {
        var user = await _dataFacade.GetUserById(userId).ConfigureAwait(false);
        return user?.IsSuperadmin ?? false;
    }

    public async Task<bool> IsSuperadminByAuth0Sub(string auth0Sub)
    {
        if (string.IsNullOrEmpty(auth0Sub)) return false;
        var user = await _dataFacade.GetUserByAuth0Sub(auth0Sub).ConfigureAwait(false);
        return user?.IsSuperadmin ?? false;
    }

    public async Task<IReadOnlyList<User>> GetSuperadmins()
    {
        return await _dataFacade.GetSuperadmins().ConfigureAwait(false);
    }

    public async Task<User> SetSuperadmin(Guid requestingUserId, Guid targetUserId, bool isSuperadmin)
    {
        // Verify the requesting user is a superadmin
        var requestingUser = await _dataFacade.GetUserById(requestingUserId).ConfigureAwait(false);
        if (requestingUser == null || !requestingUser.IsSuperadmin)
            throw new SuperadminRequiredException();

        // Verify the target user exists
        var targetUser = await _dataFacade.GetUserById(targetUserId).ConfigureAwait(false);
        if (targetUser == null) throw new UserNotFoundException();

        // Prevent removing your own superadmin status
        if (requestingUserId == targetUserId && !isSuperadmin)
            throw new UserValidationException("You cannot remove your own superadmin privileges");

        var updated = await _dataFacade.SetSuperadmin(targetUserId, isSuperadmin).ConfigureAwait(false);
        if (!updated) throw new UserNotFoundException();

        // Return the updated user
        var result = await _dataFacade.GetUserById(targetUserId).ConfigureAwait(false);
        return result!;
    }

    // User invitation / pre-provisioning

    public async Task<User> InviteUser(string email, Guid groupId, bool isAdmin)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new UserValidationException("Email is required");

        email = email.Trim().ToLowerInvariant();

        // Find existing user by email, or create a new pre-provisioned user (no auth0Sub)
        var user = await _dataFacade.GetUserByEmail(email).ConfigureAwait(false);
        if (user == null)
        {
            user = new User
            {
                Auth0Sub = string.Empty,
                Email = email,
                Name = null
            };
            user = await _dataFacade.CreateUser(user).ConfigureAwait(false);
        }

        // Add group access (with admin flag)
        await _dataFacade.AddGroupAccess(user.Id, groupId, isAdmin).ConfigureAwait(false);

        return user;
    }

    public async Task<User> InviteUserWithOrgAccess(string email, Guid groupId, bool isAdmin, IReadOnlyList<OrganizationAccessEntry>? orgAccessEntries)
    {
        var user = await InviteUser(email, groupId, isAdmin).ConfigureAwait(false);

        if (orgAccessEntries != null && orgAccessEntries.Count > 0)
        {
            // Get current org entries and merge with new ones
            var existing = await _dataFacade.GetOrganizationAccessEntries(user.Id).ConfigureAwait(false);
            var merged = existing.ToList();
            foreach (var entry in orgAccessEntries)
            {
                if (!merged.Any(e => e.OrganizationId == entry.OrganizationId))
                {
                    merged.Add(entry);
                }
            }
            await _dataFacade.SetOrganizationAccessWithFlags(user.Id, merged).ConfigureAwait(false);
        }

        return user;
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

    public async Task SetUserOrganizationAccessWithFlags(Guid userId, IReadOnlyList<OrganizationAccessEntry> entries)
    {
        var user = await _dataFacade.GetUserById(userId).ConfigureAwait(false);
        if (user == null) throw new UserNotFoundException();

        await _dataFacade.SetOrganizationAccessWithFlags(userId, entries).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<OrganizationAccessEntry>> GetOrganizationAccessEntries(Guid userId)
    {
        return await _dataFacade.GetOrganizationAccessEntries(userId).ConfigureAwait(false);
    }

    // Group admin operations

    public async Task<bool> IsGroupAdmin(Guid userId, Guid groupId)
    {
        return await _dataFacade.IsGroupAdmin(userId, groupId).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Guid>> GetGroupAdminGroupIds(Guid userId)
    {
        return await _dataFacade.GetGroupAdminGroupIds(userId).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<User>> GetUsersByGroup(Guid groupId)
    {
        return await _dataFacade.GetUsersByGroup(groupId).ConfigureAwait(false);
    }

    public async Task RemoveUserFromGroup(Guid userId, Guid groupId)
    {
        var user = await _dataFacade.GetUserById(userId).ConfigureAwait(false);
        if (user == null) throw new UserNotFoundException();

        await _dataFacade.RemoveGroupAccess(userId, groupId).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Guid>> GetAllowedOrganizationIds(Guid userId)
    {
        return await _dataFacade.GetAllowedOrganizationIds(userId).ConfigureAwait(false);
    }

    public async Task<bool> CanAccessOrganization(Guid userId, Guid organizationId)
    {
        // Superadmins can access any organization
        var user = await _dataFacade.GetUserById(userId).ConfigureAwait(false);
        if (user?.IsSuperadmin == true) return true;

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
            // GatewayFacade is owned by DomainFacade, not disposed here
            _disposed = true;
        }
    }
}
