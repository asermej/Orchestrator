namespace HireologyTestAts.Domain;

internal sealed partial class DataFacade
{
    private UserSessionDataManager? _userSessionDataManager;
    private UserSessionDataManager UserSessionDataManager => _userSessionDataManager ??= new UserSessionDataManager(_dbConnectionString);

    public async Task<Guid?> GetSelectedOrganizationId(Guid userId)
    {
        return await UserSessionDataManager.GetSelectedOrganizationIdAsync(userId).ConfigureAwait(false);
    }

    public async Task SetSelectedOrganizationId(Guid userId, Guid? organizationId)
    {
        await UserSessionDataManager.SetSelectedOrganizationIdAsync(userId, organizationId).ConfigureAwait(false);
    }
}
