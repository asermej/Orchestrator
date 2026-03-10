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
    public Guid? InterviewTemplateId { get; set; }
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

    /// <summary>
    /// The interview template that defines agent, interview content, and opening/closing.
    /// When provided, AgentId and InterviewGuideId are resolved from the template.
    /// </summary>
    public Guid? InterviewTemplateId { get; set; }

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
    public int? OverallScoreDisplay { get; set; }
    public string? Recommendation { get; set; }
    public string? RecommendationTier { get; set; }
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
    public Guid InterviewConfigurationId { get; set; }
    public Guid? InterviewTemplateId { get; set; }
    public string? TestUserName { get; set; }
}

/// <summary>
/// Request model for scoring a test interview (legacy, config-based)
/// </summary>
public class ScoreTestInterviewRequest
{
    [Required]
    public Guid InterviewConfigurationId { get; set; }
}

// ───── Runtime Resource Models ─────

/// <summary>
/// Full runtime context for a template-based interview.
/// </summary>
public class InterviewRuntimeContextResource
{
    public Guid InterviewId { get; set; }
    public string AgentName { get; set; } = "";
    public string ApplicantName { get; set; } = "";
    public string JobTitle { get; set; } = "";
    public string RoleName { get; set; } = "";
    public string Industry { get; set; } = "";
    public string OpeningText { get; set; } = "";
    public string ClosingText { get; set; } = "";
    public List<RuntimeCompetencyResource> Competencies { get; set; } = new();
}

public class RuntimeCompetencyResource
{
    public Guid CompetencyId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int ScoringWeight { get; set; }
    public int DisplayOrder { get; set; }
    public string PrimaryQuestion { get; set; } = "";
}

/// <summary>
/// Request to score and record a completed competency with all collected conversation data.
/// The frontend drives evaluation incrementally; this endpoint only scores and persists.
/// </summary>
public class ProcessCompetencyRequest
{
    [Required]
    public Guid CompetencyId { get; set; }
    [Required]
    public string PrimaryQuestion { get; set; } = "";
    [Required]
    public string CandidateResponse { get; set; } = "";
    public List<FollowUpExchangeResource>? FollowUpExchanges { get; set; }
    public int? CompetencyScore { get; set; }
    public string? Rationale { get; set; }
}

/// <summary>
/// A follow-up exchange within a competency conversation.
/// </summary>
public class FollowUpExchangeResource
{
    public string Question { get; set; } = "";
    public string Response { get; set; } = "";
}

/// <summary>
/// Request to evaluate a candidate's response holistically.
/// Supports cumulative conversation context via PriorExchanges.
/// </summary>
public class EvaluateResponseRequest
{
    [Required]
    public Guid CompetencyId { get; set; }
    [Required]
    public string CandidateResponse { get; set; } = "";
    public List<PriorExchangeResource>? PriorExchanges { get; set; }
    public string? PreviousFollowUpTarget { get; set; }
}

/// <summary>
/// A prior Q&A exchange provided as context for cumulative evaluation.
/// </summary>
public class PriorExchangeResource
{
    public string Question { get; set; } = "";
    public string Response { get; set; } = "";
}

/// <summary>
/// Request to generate the primary AI question for a competency.
/// </summary>
public class GenerateQuestionRequest
{
    [Required]
    public Guid CompetencyId { get; set; }

    /// <summary>
    /// When true, the AI will prefix the question with a brief natural transition
    /// acknowledgment referencing the previous competency exchange.
    /// </summary>
    public bool IncludeTransition { get; set; }

    /// <summary>
    /// Name of the competency that was just completed, used to generate an accurate transition.
    /// Required when IncludeTransition is true.
    /// </summary>
    public string? PreviousCompetencyName { get; set; }
}

/// <summary>
/// Response containing a generated interview question.
/// </summary>
public class GeneratedQuestionResource
{
    public Guid CompetencyId { get; set; }
    public string Question { get; set; } = "";
}

/// <summary>
/// Result of evaluating a candidate's response using holistic competency scoring.
/// action_quality, result_quality, follow_up_needed, and follow_up_target are transient.
/// </summary>
public class CompetencyEvaluationResource
{
    public int CompetencyScore { get; set; }
    public string Rationale { get; set; } = "";
    public bool FollowUpNeeded { get; set; }
    public string? FollowUpTarget { get; set; }
    public string? FollowUpQuestion { get; set; }
}

/// <summary>
/// Represents a per-competency response with holistic score in API responses
/// </summary>
public class CompetencyResponseResource
{
    public Guid Id { get; set; }
    public Guid InterviewId { get; set; }
    public Guid CompetencyId { get; set; }
    public int CompetencyScore { get; set; }
    public string? CompetencyRationale { get; set; }
    public int FollowUpCount { get; set; }
    public int? ScoringWeight { get; set; }
    public string? CompetencyTranscript { get; set; }
    public string? GeneratedQuestionText { get; set; }
    public string? QuestionsAsked { get; set; }
    public string? ResponseText { get; set; }
    public string? ResponseAudioUrl { get; set; }
    public bool CompetencySkipped { get; set; }
    public string? SkipReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// ATS-facing competency response with holistic score and paired Q&A exchanges.
/// </summary>
public class AtsCompetencyResponseResource
{
    public Guid CompetencyId { get; set; }
    public string CompetencyName { get; set; } = "";
    public int? ScoringWeight { get; set; }
    public int CompetencyScore { get; set; }
    public string? CompetencyRationale { get; set; }
    public int FollowUpCount { get; set; }
    public string? CompetencyTranscript { get; set; }
    public List<CompetencyExchangeResource> Exchanges { get; set; } = new();
    public string? AudioUrl { get; set; }
    public bool CompetencySkipped { get; set; }
    public string? SkipReason { get; set; }
}

/// <summary>
/// A single Q&A exchange within a competency conversation.
/// </summary>
public class CompetencyExchangeResource
{
    public string Question { get; set; } = "";
    public string Response { get; set; } = "";
    public string Label { get; set; } = "";
}

/// <summary>
/// Request model for creating/updating a competency response (upsert by interview_id + competency_id)
/// </summary>
public class UpsertCompetencyResponseResource
{
    [Required]
    public Guid CompetencyId { get; set; }
    [Required]
    [Range(1, 5)]
    public int CompetencyScore { get; set; }
    public string? CompetencyRationale { get; set; }
    public int FollowUpCount { get; set; }
    public int? ScoringWeight { get; set; }
    public string? QuestionsAsked { get; set; }
    public string? ResponseText { get; set; }
    public string? ResponseAudioUrl { get; set; }
}

/// <summary>
/// Global recommendation threshold settings (single row).
/// </summary>
public class RecommendationThresholdResource
{
    public Guid Id { get; set; }
    public int StronglyRecommendMin { get; set; }
    public int RecommendMin { get; set; }
    public int ConsiderMin { get; set; }
    public int DoNotRecommendMin { get; set; }
}

/// <summary>
/// Request to update recommendation thresholds.
/// </summary>
public class UpdateRecommendationThresholdRequest
{
    [Required]
    [Range(1, 100)]
    public int StronglyRecommendMin { get; set; }

    [Required]
    [Range(1, 99)]
    public int RecommendMin { get; set; }

    [Required]
    [Range(0, 98)]
    public int ConsiderMin { get; set; }

    public int DoNotRecommendMin { get; set; } = 0;
}

/// <summary>
/// Request to classify a candidate response before STAR evaluation.
/// </summary>
public class ClassifyResponseRequest
{
    [Required]
    public Guid CompetencyId { get; set; }

    [Required]
    public string CandidateResponse { get; set; } = "";

    [Required]
    public string CurrentQuestion { get; set; } = "";
}

/// <summary>
/// Result of classifying a candidate response (pre-STAR evaluation step).
/// </summary>
public class ResponseClassificationResource
{
    public string Classification { get; set; } = "on_topic";
    public bool RequiresResponse { get; set; }
    public string? ResponseText { get; set; }
    public bool ConsumesRedirect { get; set; }
    public bool AbandonCompetency { get; set; }
    public string? StoreNote { get; set; }
}

/// <summary>
/// Request to classify and evaluate a candidate response in a single round-trip.
/// If the response is on_topic, evaluation runs immediately; otherwise only classification is returned.
/// </summary>
public class ClassifyAndEvaluateRequest
{
    [Required]
    public Guid CompetencyId { get; set; }

    [Required]
    public string CandidateResponse { get; set; } = "";

    [Required]
    public string CurrentQuestion { get; set; } = "";

    [Required]
    public string CompetencyTranscript { get; set; } = "";

    public string? PreviousFollowUpTarget { get; set; }
}

/// <summary>
/// Combined classification + evaluation result.
/// Evaluation fields are null when classification is not on_topic.
/// </summary>
public class ClassifyAndEvaluateResource
{
    public string Classification { get; set; } = "on_topic";
    public bool RequiresResponse { get; set; }
    public string? ResponseText { get; set; }
    public bool ConsumesRedirect { get; set; }
    public bool AbandonCompetency { get; set; }
    public string? StoreNote { get; set; }

    public int? CompetencyScore { get; set; }
    public string? Rationale { get; set; }
    public bool? FollowUpNeeded { get; set; }
    public string? FollowUpTarget { get; set; }
    public string? FollowUpQuestion { get; set; }
}

/// <summary>
/// Request to record a skipped competency (e.g., after two off-topic responses).
/// </summary>
public class SkipCompetencyRequest
{
    [Required]
    public Guid CompetencyId { get; set; }

    [Required]
    public string PrimaryQuestion { get; set; } = "";

    [Required]
    public string SkipReason { get; set; } = "";
}
