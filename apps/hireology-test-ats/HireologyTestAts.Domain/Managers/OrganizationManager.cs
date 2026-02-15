namespace HireologyTestAts.Domain;

internal sealed class OrganizationManager : IDisposable
{
    private readonly DataFacade _dataFacade;
    private bool _disposed;

    public OrganizationManager(ServiceLocatorBase serviceLocator)
    {
        var configProvider = serviceLocator.CreateConfigurationProvider();
        _dataFacade = new DataFacade(configProvider.GetDbConnectionString());
    }

    public async Task<IReadOnlyList<Organization>> GetOrganizations(Guid? groupId = null, bool excludeTestData = false)
    {
        return await _dataFacade.GetOrganizations(groupId, excludeTestData).ConfigureAwait(false);
    }

    public async Task<Organization> GetOrganizationById(Guid id)
    {
        var org = await _dataFacade.GetOrganizationById(id).ConfigureAwait(false);
        if (org == null) throw new OrganizationNotFoundException();
        return org;
    }

    public async Task<Organization> CreateOrganization(Organization org)
    {
        if (string.IsNullOrWhiteSpace(org.Name))
            throw new OrganizationValidationException("Name is required");
        if (org.GroupId == Guid.Empty)
            throw new OrganizationValidationException("GroupId is required");

        // Validate parent organization if specified
        if (org.ParentOrganizationId.HasValue)
        {
            var parent = await _dataFacade.GetOrganizationById(org.ParentOrganizationId.Value).ConfigureAwait(false);
            if (parent == null)
                throw new OrganizationValidationException("Parent organization not found");
            if (parent.GroupId != org.GroupId)
                throw new OrganizationValidationException("Parent organization must belong to the same group");
        }

        org.Name = org.Name.Trim();
        org.City = org.City?.Trim();
        org.State = org.State?.Trim();

        return await _dataFacade.CreateOrganization(org).ConfigureAwait(false);
    }

    public async Task<Organization> UpdateOrganization(Guid id, Organization updates)
    {
        var existing = await _dataFacade.GetOrganizationById(id).ConfigureAwait(false);
        if (existing == null) throw new OrganizationNotFoundException();

        if (updates.GroupId != Guid.Empty) existing.GroupId = updates.GroupId;
        if (!string.IsNullOrWhiteSpace(updates.Name)) existing.Name = updates.Name.Trim();
        if (updates.City != null) existing.City = updates.City.Trim();
        if (updates.State != null) existing.State = updates.State.Trim();

        // Allow updating parent organization
        if (updates.ParentOrganizationId.HasValue)
        {
            // Prevent circular reference
            if (updates.ParentOrganizationId.Value == id)
                throw new OrganizationValidationException("An organization cannot be its own parent");

            var parent = await _dataFacade.GetOrganizationById(updates.ParentOrganizationId.Value).ConfigureAwait(false);
            if (parent == null)
                throw new OrganizationValidationException("Parent organization not found");
            if (parent.GroupId != existing.GroupId)
                throw new OrganizationValidationException("Parent organization must belong to the same group");

            existing.ParentOrganizationId = updates.ParentOrganizationId;
        }

        var updated = await _dataFacade.UpdateOrganization(existing).ConfigureAwait(false);
        if (updated == null) throw new OrganizationNotFoundException();
        return updated;
    }

    public async Task<Organization> MoveOrganization(Guid id, Guid? newParentOrganizationId)
    {
        var existing = await _dataFacade.GetOrganizationById(id).ConfigureAwait(false);
        if (existing == null) throw new OrganizationNotFoundException();

        // Prevent making it its own parent
        if (newParentOrganizationId.HasValue && newParentOrganizationId.Value == id)
            throw new OrganizationValidationException("An organization cannot be its own parent");

        if (newParentOrganizationId.HasValue)
        {
            var newParent = await _dataFacade.GetOrganizationById(newParentOrganizationId.Value).ConfigureAwait(false);
            if (newParent == null)
                throw new OrganizationValidationException("New parent organization not found");
            if (newParent.GroupId != existing.GroupId)
                throw new OrganizationValidationException("New parent must belong to the same group");

            // Prevent cycles: walk up from the new parent to make sure we don't hit the org being moved
            var current = newParent;
            while (current.ParentOrganizationId.HasValue)
            {
                if (current.ParentOrganizationId.Value == id)
                    throw new OrganizationValidationException("Cannot move an organization under one of its own descendants");
                current = await _dataFacade.GetOrganizationById(current.ParentOrganizationId.Value).ConfigureAwait(false);
                if (current == null) break;
            }
        }

        existing.ParentOrganizationId = newParentOrganizationId;
        var updated = await _dataFacade.UpdateOrganization(existing).ConfigureAwait(false);
        if (updated == null) throw new OrganizationNotFoundException();
        return updated;
    }

    public async Task<bool> DeleteOrganization(Guid id)
    {
        var deleted = await _dataFacade.DeleteOrganization(id).ConfigureAwait(false);
        if (!deleted) throw new OrganizationNotFoundException();
        return true;
    }

    public async Task<IReadOnlyList<Organization>> GetOrganizationTree(Guid groupId)
    {
        return await _dataFacade.GetOrganizationTree(groupId).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Organization>> GetChildOrganizations(Guid parentOrganizationId)
    {
        return await _dataFacade.GetChildOrganizations(parentOrganizationId).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}
