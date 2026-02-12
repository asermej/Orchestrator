namespace Orchestrator.Api.ResourcesModels;

/// <summary>
/// Represents an InterviewQuestion in API responses
/// </summary>
public class InterviewQuestionResource
{
    public Guid Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public int QuestionOrder { get; set; }
    public bool IsRequired { get; set; }
    public string? FollowUpPrompt { get; set; }
    public int MaxFollowUps { get; set; }
    public bool FollowUpsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
