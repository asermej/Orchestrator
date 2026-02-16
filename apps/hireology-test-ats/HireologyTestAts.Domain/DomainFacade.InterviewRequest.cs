namespace HireologyTestAts.Domain;

public sealed partial class DomainFacade
{
    public async Task<InterviewRequest> SendInterviewRequest(Guid applicantId, Guid interviewConfigurationId)
    {
        return await InterviewRequestManager.SendInterviewRequest(applicantId, interviewConfigurationId).ConfigureAwait(false);
    }

    public async Task<InterviewRequest?> GetInterviewRequestByApplicantId(Guid applicantId)
    {
        return await InterviewRequestManager.GetByApplicantId(applicantId).ConfigureAwait(false);
    }

    public async Task<InterviewRequest?> GetInterviewRequestById(Guid id)
    {
        return await InterviewRequestManager.GetById(id).ConfigureAwait(false);
    }

    public async Task<InterviewRequest?> GetInterviewRequestByOrchestratorInterviewId(Guid orchestratorInterviewId)
    {
        return await InterviewRequestManager.GetByOrchestratorInterviewId(orchestratorInterviewId).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<InterviewRequest>> GetInterviewRequestsByJobId(Guid jobId)
    {
        return await InterviewRequestManager.GetByJobId(jobId).ConfigureAwait(false);
    }

    public async Task<InterviewRequest> UpdateInterviewRequestFromWebhook(
        Guid orchestratorInterviewId,
        string status,
        int? score,
        string? summary,
        string? recommendation,
        string? strengths,
        string? areasForImprovement)
    {
        return await InterviewRequestManager.UpdateFromWebhook(
            orchestratorInterviewId, status, score, summary, recommendation, strengths, areasForImprovement)
            .ConfigureAwait(false);
    }

    public async Task<InterviewRequest> RefreshInterviewInvite(Guid interviewRequestId)
    {
        return await InterviewRequestManager.RefreshInvite(interviewRequestId).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets available agents from Orchestrator for a specific group.
    /// Pass the group's OrchestratorApiKey; falls back to global config if null.
    /// </summary>
    public async Task<IReadOnlyList<OrchestratorAgent>> GetAgents(string? groupApiKey = null)
    {
        return await InterviewRequestManager.GetAgents(groupApiKey).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets available interview configurations from Orchestrator for a specific group.
    /// Pass the group's OrchestratorApiKey; falls back to global config if null.
    /// </summary>
    public async Task<IReadOnlyList<OrchestratorInterviewConfiguration>> GetInterviewConfigurations(string? groupApiKey = null)
    {
        return await InterviewRequestManager.GetConfigurations(groupApiKey).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the current webhook status from Orchestrator for a specific group.
    /// Pass the group's OrchestratorApiKey; falls back to global config if null.
    /// </summary>
    public async Task<(bool Configured, string? WebhookUrl)> GetWebhookStatus(string? groupApiKey = null)
    {
        return await InterviewRequestManager.GetWebhookStatus(groupApiKey).ConfigureAwait(false);
    }

    /// <summary>
    /// Configures the webhook URL in Orchestrator for a specific group.
    /// Pass the group's OrchestratorApiKey; falls back to global config if null.
    /// </summary>
    public async Task<bool> ConfigureWebhookUrl(string webhookUrl, string? groupApiKey = null)
    {
        return await InterviewRequestManager.ConfigureWebhookUrl(webhookUrl, groupApiKey).ConfigureAwait(false);
    }
}
