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

    public async Task<IReadOnlyList<Organization>> GetOrganizations(Guid? groupId)
    {
        return await _dataFacade.GetOrganizations(groupId).ConfigureAwait(false);
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

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}
