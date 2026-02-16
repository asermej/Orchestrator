namespace HireologyTestAts.Domain;

/// <summary>
/// Maps between domain models and Orchestrator API resource models
/// </summary>
internal static class OrchestratorMapper
{
    // --- ToRequest Mappings (Domain -> API) ---

    public static OrchestratorSyncJobRequest ToSyncJobRequest(Job job)
    {
        if (job == null)
            throw new ArgumentNullException(nameof(job));

        return new OrchestratorSyncJobRequest
        {
            ExternalJobId = job.ExternalJobId,
            Title = job.Title,
            Description = job.Description,
            Location = job.Location,
            OrganizationId = job.OrganizationId
        };
    }

    public static OrchestratorSyncApplicantRequest ToSyncApplicantRequest(Applicant applicant, string externalJobId)
    {
        if (applicant == null)
            throw new ArgumentNullException(nameof(applicant));

        return new OrchestratorSyncApplicantRequest
        {
            ExternalApplicantId = applicant.Id.ToString(),
            ExternalJobId = externalJobId,
            FirstName = applicant.FirstName,
            LastName = applicant.LastName,
            Email = applicant.Email,
            Phone = applicant.Phone,
            OrganizationId = applicant.OrganizationId
        };
    }

    public static OrchestratorCreateInterviewRequest ToCreateInterviewRequest(
        string externalApplicantId, string externalJobId, Guid interviewConfigurationId)
    {
        return new OrchestratorCreateInterviewRequest
        {
            ExternalApplicantId = externalApplicantId,
            ExternalJobId = externalJobId,
            InterviewConfigurationId = interviewConfigurationId,
            InterviewType = "voice"
        };
    }

    public static OrchestratorProvisionUserRequest ToProvisionUserRequest(string auth0Sub, string? email, string? name)
    {
        var safeEmail = !string.IsNullOrWhiteSpace(email)
            ? email!.Trim()
            : $"{auth0Sub.Replace("|", "-")}@test-ats.local";

        var (firstName, lastName) = SplitName(name);

        return new OrchestratorProvisionUserRequest
        {
            Auth0Sub = auth0Sub,
            Email = safeEmail,
            FirstName = firstName,
            LastName = lastName,
            Phone = null
        };
    }

    // --- FromResponse Mappings (API -> Domain) ---

    public static OrchestratorCreateInterviewResult ToCreateInterviewResult(
        OrchestratorCreateInterviewResponse response)
    {
        if (response == null)
            throw new ArgumentNullException(nameof(response));

        var shortCode = response.Invite.ShortCode;

        // Use the invite URL from the response if available, otherwise construct a default
        var inviteUrl = !string.IsNullOrEmpty(response.Invite.Url)
            ? response.Invite.Url
            : $"http://localhost:3000/i/{shortCode}";

        return new OrchestratorCreateInterviewResult
        {
            InterviewId = response.Interview.Id,
            InviteUrl = inviteUrl,
            ShortCode = shortCode
        };
    }

    public static OrchestratorCreateInterviewResult ToRefreshInviteResult(
        Guid orchestratorInterviewId, OrchestratorRefreshInviteResponse response)
    {
        if (response == null)
            throw new ArgumentNullException(nameof(response));

        var shortCode = response.Invite.ShortCode;

        var inviteUrl = !string.IsNullOrEmpty(response.InviteUrl)
            ? response.InviteUrl
            : $"http://localhost:3000/i/{shortCode}";

        return new OrchestratorCreateInterviewResult
        {
            InterviewId = orchestratorInterviewId,
            InviteUrl = inviteUrl,
            ShortCode = shortCode
        };
    }

    public static OrchestratorInterviewStatus ToInterviewStatus(OrchestratorInterviewStatusResponse response)
    {
        if (response == null)
            throw new ArgumentNullException(nameof(response));

        return new OrchestratorInterviewStatus
        {
            InterviewStatus = response.Interview.Status,
            InviteStatus = response.InviteStatus
        };
    }

    public static IReadOnlyList<OrchestratorAgent> ToAgents(List<OrchestratorAgentResponse>? responses)
    {
        if (responses == null || responses.Count == 0)
            return Array.Empty<OrchestratorAgent>();

        return responses.Select(r => new OrchestratorAgent
        {
            Id = r.Id,
            DisplayName = r.DisplayName,
            ProfileImageUrl = r.ProfileImageUrl
        }).ToList();
    }

    public static IReadOnlyList<OrchestratorInterviewConfiguration> ToConfigurations(
        List<OrchestratorInterviewConfigurationResponse>? responses)
    {
        if (responses == null || responses.Count == 0)
            return Array.Empty<OrchestratorInterviewConfiguration>();

        return responses.Select(r => new OrchestratorInterviewConfiguration
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            AgentId = r.AgentId,
            AgentDisplayName = r.AgentDisplayName,
            QuestionCount = r.QuestionCount
        }).ToList();
    }

    public static OrchestratorSyncGroupRequest ToSyncGroupRequest(Group group, string? atsBaseUrl, string? webhookUrl, string? atsApiKey)
    {
        if (group == null)
            throw new ArgumentNullException(nameof(group));

        return new OrchestratorSyncGroupRequest
        {
            ExternalGroupId = group.Id,
            Name = group.Name,
            AtsBaseUrl = atsBaseUrl,
            WebhookUrl = webhookUrl,
            AtsApiKey = atsApiKey
        };
    }

    public static OrchestratorSyncGroupResult ToSyncGroupResult(OrchestratorSyncGroupResponse response)
    {
        if (response == null)
            throw new ArgumentNullException(nameof(response));

        return new OrchestratorSyncGroupResult
        {
            OrchestratorGroupId = response.Id,
            ApiKey = response.ApiKey,
            IsNew = response.IsNew
        };
    }

    // --- Private Helpers ---

    private static (string FirstName, string LastName) SplitName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return ("", "");
        var parts = name.Trim().Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 ? (parts[0], parts[1]) : (parts[0], "");
    }
}
