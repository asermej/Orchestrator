namespace HireologyTestAts.Domain;

public sealed partial class DomainFacade
{
    public async Task<InterviewRequest> SendInterviewRequest(Guid applicantId, Guid agentId, Guid interviewGuideId)
    {
        return await InterviewRequestManager.SendInterviewRequest(applicantId, agentId, interviewGuideId).ConfigureAwait(false);
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

    /// <summary>
    /// Verifies the HMAC-SHA256 signature and timestamp of an incoming webhook payload.
    /// </summary>
    public bool VerifyWebhookSignature(string body, string? signature, string? timestamp)
    {
        return InterviewRequestManager.VerifyWebhookSignature(body, signature, timestamp);
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
    /// On-demand refresh of interview request status from Orchestrator.
    /// Use when a webhook may have been missed.
    /// </summary>
    public async Task<InterviewRequest> RefreshInterviewRequestStatus(Guid interviewRequestId)
    {
        return await InterviewRequestManager.RefreshStatusFromOrchestrator(interviewRequestId).ConfigureAwait(false);
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
    /// Gets available interview guides from Orchestrator for a specific group.
    /// Pass the group's OrchestratorApiKey; falls back to global config if null.
    /// </summary>
    public async Task<IReadOnlyList<OrchestratorInterviewGuide>> GetInterviewGuides(string? groupApiKey = null)
    {
        return await InterviewRequestManager.GetInterviewGuides(groupApiKey).ConfigureAwait(false);
    }

}
