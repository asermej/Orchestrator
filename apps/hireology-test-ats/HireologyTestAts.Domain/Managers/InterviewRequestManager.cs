using System.Security.Cryptography;
using System.Text;

namespace HireologyTestAts.Domain;

internal sealed class InterviewRequestManager : IDisposable
{
    private readonly DataFacade _dataFacade;
    private readonly GatewayFacade _gatewayFacade;
    private readonly string _webhookSecret;
    private bool _disposed;

    private const int MaxTimestampAgeSecs = 300; // 5 minutes

    public InterviewRequestManager(ServiceLocatorBase serviceLocator, GatewayFacade gatewayFacade)
    {
        var configProvider = serviceLocator.CreateConfigurationProvider();
        _dataFacade = new DataFacade(configProvider.GetDbConnectionString());
        _gatewayFacade = gatewayFacade ?? throw new ArgumentNullException(nameof(gatewayFacade));
        _webhookSecret = configProvider.GetOrchestratorApiKey();
    }

    /// <summary>
    /// Sends an interview request: syncs applicant, creates interview in Orchestrator, saves local record
    /// </summary>
    public async Task<InterviewRequest> SendInterviewRequest(Guid applicantId, Guid agentId, Guid interviewGuideId)
    {
        var applicant = await _dataFacade.GetApplicantById(applicantId).ConfigureAwait(false);
        if (applicant == null) throw new ApplicantNotFoundException();

        var job = await _dataFacade.GetJobById(applicant.JobId).ConfigureAwait(false);
        if (job == null) throw new JobNotFoundException();

        var apiKey = await ResolveApiKeyForJob(job).ConfigureAwait(false);

        await _gatewayFacade.SyncJob(job, apiKey).ConfigureAwait(false);
        await _gatewayFacade.SyncApplicant(applicant, job.ExternalJobId, apiKey).ConfigureAwait(false);

        var result = await _gatewayFacade.CreateInterview(
            applicant.Id.ToString(),
            job.ExternalJobId,
            agentId,
            interviewGuideId,
            apiKey).ConfigureAwait(false);

        var request = new InterviewRequest
        {
            ApplicantId = applicantId,
            JobId = applicant.JobId,
            OrchestratorInterviewId = result.InterviewId,
            InviteUrl = result.InviteUrl,
            ShortCode = result.ShortCode,
            Status = InterviewRequestStatus.NotStarted
        };

        return await _dataFacade.CreateInterviewRequest(request).ConfigureAwait(false);
    }

    /// <summary>
    /// Refreshes the invite for an existing interview request (when the link is expired or used up)
    /// </summary>
    public async Task<InterviewRequest> RefreshInvite(Guid interviewRequestId)
    {
        var request = await _dataFacade.GetInterviewRequestById(interviewRequestId).ConfigureAwait(false);
        if (request == null) throw new InterviewRequestNotFoundException();
        if (request.OrchestratorInterviewId == null)
            throw new InterviewRequestValidationException("No Orchestrator interview linked to this request");

        var apiKey = await ResolveApiKeyForInterviewRequest(request).ConfigureAwait(false);
        var result = await _gatewayFacade.RefreshInvite(request.OrchestratorInterviewId.Value, apiKey).ConfigureAwait(false);

        request.InviteUrl = result.InviteUrl;
        request.ShortCode = result.ShortCode;
        request.Status = InterviewRequestStatus.NotStarted;

        return await _dataFacade.UpdateInterviewRequest(request).ConfigureAwait(false);
    }

    public async Task<InterviewRequest?> GetByApplicantId(Guid applicantId)
    {
        return await _dataFacade.GetInterviewRequestByApplicantId(applicantId).ConfigureAwait(false);
    }

    public async Task<InterviewRequest?> GetById(Guid id)
    {
        return await _dataFacade.GetInterviewRequestById(id).ConfigureAwait(false);
    }

    public async Task<InterviewRequest?> GetByOrchestratorInterviewId(Guid orchestratorInterviewId)
    {
        return await _dataFacade.GetInterviewRequestByOrchestratorInterviewId(orchestratorInterviewId).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<InterviewRequest>> GetByJobId(Guid jobId)
    {
        return await _dataFacade.GetInterviewRequestsByJobId(jobId).ConfigureAwait(false);
    }

    /// <summary>
    /// On-demand refresh: syncs local status from Orchestrator for a single interview request.
    /// Use when a user suspects a missed webhook.
    /// </summary>
    public async Task<InterviewRequest> RefreshStatusFromOrchestrator(Guid interviewRequestId)
    {
        var request = await _dataFacade.GetInterviewRequestById(interviewRequestId).ConfigureAwait(false);
        if (request == null) throw new InterviewRequestNotFoundException();

        if (request.Status is InterviewRequestStatus.Completed or InterviewRequestStatus.LinkExpired)
            return request;

        if (!request.OrchestratorInterviewId.HasValue)
            return request;

        var apiKey = await ResolveApiKeyForInterviewRequest(request).ConfigureAwait(false);
        var orchestratorStatus = await _gatewayFacade.GetInterviewStatus(
            request.OrchestratorInterviewId.Value, apiKey).ConfigureAwait(false);

        if (orchestratorStatus == null)
            return request;

        var newStatus = orchestratorStatus.InterviewStatus switch
        {
            "completed" => InterviewRequestStatus.Completed,
            "in_progress" => InterviewRequestStatus.InProgress,
            _ when orchestratorStatus.InviteStatus is "max_uses_reached" or "expired" or "revoked"
                => InterviewRequestStatus.LinkExpired,
            _ => (string?)null
        };

        if (newStatus != null && newStatus != request.Status)
        {
            request.Status = newStatus;
            return await _dataFacade.UpdateInterviewRequest(request).ConfigureAwait(false);
        }

        return request;
    }

    /// <summary>
    /// Verifies the HMAC-SHA256 signature and timestamp of an incoming webhook.
    /// Returns true if verification passes, or if no secret is configured (graceful degradation).
    /// </summary>
    public bool VerifyWebhookSignature(string body, string? signature, string? timestamp)
    {
        if (string.IsNullOrEmpty(_webhookSecret))
            return true;

        if (string.IsNullOrEmpty(signature))
            return false;

        if (!string.IsNullOrEmpty(timestamp) && long.TryParse(timestamp, out var ts))
        {
            var age = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - ts;
            if (Math.Abs(age) > MaxTimestampAgeSecs)
                return false;
        }

        var expected = ComputeSignature(body, _webhookSecret);
        return string.Equals(signature, expected, StringComparison.OrdinalIgnoreCase);
    }

    private static string ComputeSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return $"sha256={Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    /// <summary>
    /// Updates status and result from webhook payload
    /// </summary>
    public async Task<InterviewRequest> UpdateFromWebhook(
        Guid orchestratorInterviewId,
        string status,
        int? score,
        string? summary,
        string? recommendation,
        string? strengths,
        string? areasForImprovement)
    {
        var request = await _dataFacade.GetInterviewRequestByOrchestratorInterviewId(orchestratorInterviewId)
            .ConfigureAwait(false);

        if (request == null)
            throw new InterviewRequestNotFoundException();

        request.Status = status;
        request.Score = score;
        request.ResultSummary = summary;
        request.ResultRecommendation = recommendation;
        request.ResultStrengths = strengths;
        request.ResultAreasForImprovement = areasForImprovement;
        request.WebhookReceivedAt = DateTime.UtcNow;

        return await _dataFacade.UpdateInterviewRequest(request).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets available agents from Orchestrator for a specific group
    /// </summary>
    public async Task<IReadOnlyList<OrchestratorAgent>> GetAgents(string? groupApiKey)
    {
        return await _gatewayFacade.GetAgents(groupApiKey).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets available interview configurations from Orchestrator for a specific group
    /// </summary>
    public async Task<IReadOnlyList<OrchestratorInterviewConfiguration>> GetConfigurations(string? groupApiKey)
    {
        return await _gatewayFacade.GetConfigurations(groupApiKey).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets available interview guides from Orchestrator for a specific group
    /// </summary>
    public async Task<IReadOnlyList<OrchestratorInterviewGuide>> GetInterviewGuides(string? groupApiKey)
    {
        return await _gatewayFacade.GetInterviewGuides(groupApiKey).ConfigureAwait(false);
    }

    private async Task<string?> ResolveApiKeyForJob(Job job)
    {
        if (job.OrganizationId.HasValue)
        {
            return await _dataFacade.GetOrchestratorApiKeyForOrganization(job.OrganizationId.Value)
                .ConfigureAwait(false);
        }
        return null;
    }

    private async Task<string?> ResolveApiKeyForInterviewRequest(InterviewRequest request)
    {
        var job = await _dataFacade.GetJobById(request.JobId).ConfigureAwait(false);
        if (job != null)
        {
            return await ResolveApiKeyForJob(job).ConfigureAwait(false);
        }
        return null;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}
