namespace Orchestrator.Domain;

/// <summary>
/// Gateway facade partial class for ATS integration.
/// Provides business-focused methods for querying the ATS about user access and organizations.
/// </summary>
internal sealed partial class GatewayFacade
{
    private AtsGatewayManager? _atsGatewayManager;
    private AtsGatewayManager AtsGatewayManager => _atsGatewayManager ??= new AtsGatewayManager(_serviceLocator);

    /// <summary>
    /// Gets a user's group/organization access from the ATS.
    /// </summary>
    /// <param name="atsBaseUrl">Base URL of the ATS API (from Group.AtsBaseUrl)</param>
    /// <param name="apiKey">API key for the ATS (from Group.ApiKey)</param>
    /// <param name="auth0Sub">The user's Auth0 sub identifier</param>
    public async Task<AtsUserAccess> GetUserAccessFromAts(string atsBaseUrl, string apiKey, string auth0Sub)
    {
        return await AtsGatewayManager.GetUserAccess(atsBaseUrl, apiKey, auth0Sub).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets all organizations from the ATS, optionally filtered by group.
    /// </summary>
    /// <param name="atsBaseUrl">Base URL of the ATS API (from Group.AtsBaseUrl)</param>
    /// <param name="apiKey">API key for the ATS (from Group.ApiKey)</param>
    /// <param name="groupId">Optional group ID filter</param>
    public async Task<IReadOnlyList<AtsOrganizationAccess>> GetOrganizationsFromAts(
        string atsBaseUrl, string apiKey, Guid? groupId = null)
    {
        return await AtsGatewayManager.GetOrganizations(atsBaseUrl, apiKey, groupId).ConfigureAwait(false);
    }
}
