namespace HireologyTestAts.Domain;

public class InterviewRequest
{
    public Guid Id { get; set; }
    public Guid ApplicantId { get; set; }
    public Guid JobId { get; set; }
    public Guid? OrchestratorInterviewId { get; set; }
    public string? InviteUrl { get; set; }
    public string? ShortCode { get; set; }
    public string Status { get; set; } = InterviewRequestStatus.NotStarted;
    public int? Score { get; set; }
    public string? ResultSummary { get; set; }
    public string? ResultRecommendation { get; set; }
    public string? ResultStrengths { get; set; }
    public string? ResultAreasForImprovement { get; set; }
    public DateTime? WebhookReceivedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public static class InterviewRequestStatus
{
    public const string NotStarted = "not_started";
    public const string InProgress = "in_progress";
    public const string Completed = "completed";
    public const string LinkExpired = "link_expired";
}
