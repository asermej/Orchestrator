namespace Orchestrator.Domain;

public sealed partial class DomainFacade
{
    /// <summary>
    /// Gets a user's group/organization access from the ATS for a given Orchestrator group.
    /// Looks up the group's AtsBaseUrl and AtsApiKey, then calls the ATS external endpoint.
    /// Returns null if the group has no ATS base URL or ATS API key configured.
    /// </summary>
    /// <param name="groupId">The Orchestrator group ID</param>
    /// <param name="auth0Sub">The user's Auth0 sub identifier</param>
    public async Task<AtsUserAccess?> GetUserAccessFromAts(Guid groupId, string auth0Sub)
    {
        var group = await GroupManager.GetGroupById(groupId).ConfigureAwait(false);
        if (group == null || string.IsNullOrEmpty(group.AtsBaseUrl) || string.IsNullOrEmpty(group.AtsApiKey))
            return null;

        return await GatewayFacade.GetUserAccessFromAts(
            group.AtsBaseUrl, group.AtsApiKey, auth0Sub).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets all organizations from the ATS for a given Orchestrator group.
    /// Returns an empty list if the group has no ATS base URL or ATS API key configured.
    /// </summary>
    /// <param name="groupId">The Orchestrator group ID</param>
    /// <param name="atsGroupId">Optional ATS group ID to filter by</param>
    public async Task<IReadOnlyList<AtsOrganizationAccess>> GetOrganizationsFromAts(Guid groupId, Guid? atsGroupId = null)
    {
        var group = await GroupManager.GetGroupById(groupId).ConfigureAwait(false);
        if (group == null || string.IsNullOrEmpty(group.AtsBaseUrl) || string.IsNullOrEmpty(group.AtsApiKey))
            return Array.Empty<AtsOrganizationAccess>();

        return await GatewayFacade.GetOrganizationsFromAts(
            group.AtsBaseUrl, group.AtsApiKey, atsGroupId).ConfigureAwait(false);
    }
}
