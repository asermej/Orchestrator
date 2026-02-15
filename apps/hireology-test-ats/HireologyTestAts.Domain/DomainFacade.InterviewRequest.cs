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

    public async Task<IReadOnlyList<OrchestratorAgent>> GetAgents()
    {
        return await InterviewRequestManager.GetAgents().ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<OrchestratorInterviewConfiguration>> GetInterviewConfigurations()
    {
        return await InterviewRequestManager.GetConfigurations().ConfigureAwait(false);
    }

    public async Task<bool> ConfigureWebhookUrl(string webhookUrl)
    {
        return await InterviewRequestManager.ConfigureWebhookUrl(webhookUrl).ConfigureAwait(false);
    }
}
