using System.ComponentModel.DataAnnotations;
using Orchestrator.Api.Common;

namespace Orchestrator.Api.ResourcesModels;

/// <summary>
/// Represents an Interview in API responses
/// </summary>
public class InterviewResource
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public Guid ApplicantId { get; set; }
    public Guid AgentId { get; set; }
    public Guid? InterviewConfigurationId { get; set; }
    public Guid? InterviewGuideId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string InterviewType { get; set; } = string.Empty;
    public DateTime? ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int CurrentQuestionIndex { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Related entities (populated on list/search and detail endpoints)
    public JobResource? Job { get; set; }
    public ApplicantResource? Applicant { get; set; }
    public AgentResource? Agent { get; set; }
    public List<InterviewResponseResource> Responses { get; set; } = new();
    public InterviewResultResource? Result { get; set; }
}

/// <summary>
/// Detailed interview resource including questions (extends base with all related data)
/// </summary>
public class InterviewDetailResource : InterviewResource
{
    public List<InterviewQuestionResource> Questions { get; set; } = new();
}

/// <summary>
/// Request model for creating an Interview
/// </summary>
public class CreateInterviewResource
{
    [Required]
    public Guid JobId { get; set; }

    [Required]
    public Guid ApplicantId { get; set; }

    [Required]
    public Guid AgentId { get; set; }

    public string InterviewType { get; set; } = "voice";

    public DateTime? ScheduledAt { get; set; }
}

/// <summary>
/// Simplified request to create interview with applicant data
/// </summary>
public class CreateInterviewWithApplicantResource
{
    [Required]
    public string ExternalJobId { get; set; } = string.Empty;

    [Required]
    public string ExternalApplicantId { get; set; } = string.Empty;

    public string? ApplicantFirstName { get; set; }
    public string? ApplicantLastName { get; set; }
    public string? ApplicantEmail { get; set; }
    public string? ApplicantPhone { get; set; }

    /// <summary>
    /// The agent to conduct the interview. Required if InterviewConfigurationId is not provided.
    /// </summary>
    public Guid AgentId { get; set; }

    /// <summary>
    /// The interview configuration that defines agent, questions, and scoring.
    /// When provided, AgentId is determined from the configuration.
    /// </summary>
    public Guid? InterviewConfigurationId { get; set; }

    /// <summary>
    /// The interview guide containing the questions for this interview.
    /// When provided alongside AgentId, no InterviewConfigurationId is needed.
    /// </summary>
    public Guid? InterviewGuideId { get; set; }

    public string InterviewType { get; set; } = "voice";

    public DateTime? ScheduledAt { get; set; }
}

/// <summary>
/// Request model for searching Interviews
/// </summary>
public class SearchInterviewRequest : PaginatedRequest
{
    public Guid? JobId { get; set; }
    public Guid? ApplicantId { get; set; }
    public Guid? AgentId { get; set; }
    public string? Status { get; set; }
}

/// <summary>
/// Represents an InterviewResponse in API responses
/// </summary>
public class InterviewResponseResource
{
    public Guid Id { get; set; }
    public Guid InterviewId { get; set; }
    public Guid? QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? Transcript { get; set; }
    public string? AudioUrl { get; set; }
    public int? DurationSeconds { get; set; }
    public int ResponseOrder { get; set; }
    public bool IsFollowUp { get; set; }
    public Guid? FollowUpTemplateId { get; set; }
    public string QuestionType { get; set; } = "main";
    public string? AiAnalysis { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request model for adding an InterviewResponse
/// </summary>
public class CreateInterviewResponseResource
{
    /// <summary>
    /// Optional for test interviews (which use configuration questions)
    /// </summary>
    public Guid? QuestionId { get; set; }

    [Required]
    public string QuestionText { get; set; } = string.Empty;

    public string? Transcript { get; set; }

    public string? AudioUrl { get; set; }

    public int? DurationSeconds { get; set; }

    public int ResponseOrder { get; set; }

    public bool IsFollowUp { get; set; }

    public Guid? FollowUpTemplateId { get; set; }
}

/// <summary>
/// Represents an InterviewResult in API responses
/// </summary>
public class InterviewResultResource
{
    public Guid Id { get; set; }
    public Guid InterviewId { get; set; }
    public string? Summary { get; set; }
    public int? Score { get; set; }
    public string? Recommendation { get; set; }
    public string? Strengths { get; set; }
    public string? AreasForImprovement { get; set; }
    public string? FullTranscriptUrl { get; set; }
    public DateTime? WebhookSentAt { get; set; }
    public List<QuestionScoreResource> QuestionScores { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Represents a per-question score in API responses
/// </summary>
public class QuestionScoreResource
{
    public int QuestionIndex { get; set; }
    public string Question { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public decimal Weight { get; set; }
    public string Feedback { get; set; } = string.Empty;
}

/// <summary>
/// Request model for creating/updating an InterviewResult
/// </summary>
public class CreateInterviewResultResource
{
    public string? Summary { get; set; }
    public int? Score { get; set; }
    public string? Recommendation { get; set; }
    public string? Strengths { get; set; }
    public string? AreasForImprovement { get; set; }
}

/// <summary>
/// Request model for creating a test interview
/// </summary>
public class CreateTestInterviewRequest
{
    [Required]
    public Guid InterviewConfigurationId { get; set; }

    public string? TestUserName { get; set; }
}

/// <summary>
/// Request model for scoring a test interview
/// </summary>
public class ScoreTestInterviewRequest
{
    [Required]
    public Guid InterviewConfigurationId { get; set; }
}
