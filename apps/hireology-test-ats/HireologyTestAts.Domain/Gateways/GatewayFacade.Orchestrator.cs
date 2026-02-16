namespace HireologyTestAts.Domain;

/// <summary>
/// Gateway facade for Orchestrator integration.
/// Provides business-focused methods that shield the domain from Orchestrator API details.
/// All methods accept a group API key so the correct Orchestrator group is targeted.
/// </summary>
internal sealed partial class GatewayFacade
{
    private OrchestratorManager? _orchestratorManager;
    private OrchestratorManager OrchestratorManager => _orchestratorManager ??= new OrchestratorManager(_serviceLocator);

    /// <summary>
    /// Syncs a group to Orchestrator (create or update by external group ID).
    /// Returns the sync result including the Orchestrator API key.
    /// </summary>
    public async Task<OrchestratorSyncGroupResult> SyncGroup(Group group)
    {
        return await OrchestratorManager.SyncGroup(group).ConfigureAwait(false);
    }

    /// <summary>
    /// Syncs a job to Orchestrator (create or update)
    /// </summary>
    public async Task SyncJob(Job job, string? groupApiKey)
    {
        await OrchestratorManager.SyncJob(job, groupApiKey).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes a job from Orchestrator by external job ID
    /// </summary>
    public async Task DeleteJob(string externalJobId, string? groupApiKey)
    {
        await OrchestratorManager.DeleteJob(externalJobId, groupApiKey).ConfigureAwait(false);
    }

    /// <summary>
    /// Lists available interview agents from Orchestrator
    /// </summary>
    public async Task<IReadOnlyList<OrchestratorAgent>> GetAgents(string? groupApiKey)
    {
        return await OrchestratorManager.GetAgents(groupApiKey).ConfigureAwait(false);
    }

    /// <summary>
    /// Lists available interview configurations from Orchestrator
    /// </summary>
    public async Task<IReadOnlyList<OrchestratorInterviewConfiguration>> GetConfigurations(string? groupApiKey)
    {
        return await OrchestratorManager.GetConfigurations(groupApiKey).ConfigureAwait(false);
    }

    /// <summary>
    /// Syncs an applicant to Orchestrator (create or update)
    /// </summary>
    public async Task SyncApplicant(Applicant applicant, string externalJobId, string? groupApiKey)
    {
        await OrchestratorManager.SyncApplicant(applicant, externalJobId, groupApiKey).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates an interview in Orchestrator and returns the interview ID and invite URL
    /// </summary>
    public async Task<OrchestratorCreateInterviewResult> CreateInterview(
        string externalApplicantId, string externalJobId, Guid interviewConfigurationId, string? groupApiKey)
    {
        return await OrchestratorManager.CreateInterview(
            externalApplicantId, externalJobId, interviewConfigurationId, groupApiKey).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets interview status from Orchestrator, including current invite status.
    /// Returns null if the interview is not found.
    /// </summary>
    public async Task<OrchestratorInterviewStatus?> GetInterviewStatus(Guid orchestratorInterviewId, string? groupApiKey)
    {
        return await OrchestratorManager.GetInterviewStatus(orchestratorInterviewId, groupApiKey).ConfigureAwait(false);
    }

    /// <summary>
    /// Refreshes the invite for an existing interview (revokes old, creates new)
    /// </summary>
    public async Task<OrchestratorCreateInterviewResult> RefreshInvite(Guid orchestratorInterviewId, string? groupApiKey)
    {
        return await OrchestratorManager.RefreshInvite(orchestratorInterviewId, groupApiKey).ConfigureAwait(false);
    }

    /// <summary>
    /// Provisions a user in Orchestrator. Checks if user exists first, creates if not found.
    /// </summary>
    public async Task ProvisionUser(string auth0Sub, string? email, string? name, string? groupApiKey)
    {
        await OrchestratorManager.ProvisionUser(auth0Sub, email, name, groupApiKey).ConfigureAwait(false);
    }
}
