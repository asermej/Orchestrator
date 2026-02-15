namespace HireologyTestAts.Api.ResourceModels;

public class InterviewRequestResource
{
    public Guid Id { get; set; }
    public Guid ApplicantId { get; set; }
    public Guid JobId { get; set; }
    public Guid? OrchestratorInterviewId { get; set; }
    public string? InviteUrl { get; set; }
    public string? ShortCode { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? Score { get; set; }
    public string? ResultSummary { get; set; }
    public string? ResultRecommendation { get; set; }
    public string? ResultStrengths { get; set; }
    public string? ResultAreasForImprovement { get; set; }
    public DateTime? WebhookReceivedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class SendInterviewRequestResource
{
    public Guid InterviewConfigurationId { get; set; }
}

public class AgentResource
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
}

public class InterviewConfigurationResource
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid AgentId { get; set; }
    public string? AgentDisplayName { get; set; }
    public int QuestionCount { get; set; }
}
