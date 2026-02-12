using System.ComponentModel.DataAnnotations;

namespace Orchestrator.Api.ResourcesModels;

/// <summary>
/// Represents a FollowUpTemplate in API responses
/// </summary>
public class FollowUpTemplateResource
{
    /// <summary>
    /// The unique identifier of the follow-up template
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The interview question this follow-up is attached to (for Interview Questions)
    /// </summary>
    public Guid? InterviewQuestionId { get; set; }

    /// <summary>
    /// The interview configuration question this follow-up is attached to (for Interview Configuration Questions)
    /// </summary>
    public Guid? InterviewConfigurationQuestionId { get; set; }

    /// <summary>
    /// Competency tag (e.g., "Safety", "Reliability", "Communication")
    /// </summary>
    public string? CompetencyTag { get; set; }

    /// <summary>
    /// Keywords/phrases that help trigger this follow-up
    /// </summary>
    public List<string>? TriggerHints { get; set; }

    /// <summary>
    /// The approved follow-up question text
    /// </summary>
    public string CanonicalText { get; set; } = string.Empty;

    /// <summary>
    /// Whether paraphrasing is allowed (default false for V1.5)
    /// </summary>
    public bool AllowParaphrase { get; set; }

    /// <summary>
    /// Whether this follow-up is approved for use
    /// </summary>
    public bool IsApproved { get; set; }

    /// <summary>
    /// Who created this follow-up ("ai_suggested" or "admin_created")
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// When this follow-up was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this follow-up was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Request model for creating a new follow-up template
/// </summary>
public class CreateFollowUpTemplateResource
{
    /// <summary>
    /// Competency tag
    /// </summary>
    public string? CompetencyTag { get; set; }

    /// <summary>
    /// Trigger hints
    /// </summary>
    public List<string>? TriggerHints { get; set; }

    /// <summary>
    /// The follow-up question text
    /// </summary>
    [Required]
    public string CanonicalText { get; set; } = string.Empty;

    /// <summary>
    /// Whether paraphrasing is allowed
    /// </summary>
    public bool AllowParaphrase { get; set; } = false;
}

/// <summary>
/// Request model for updating a follow-up template
/// </summary>
public class UpdateFollowUpTemplateResource
{
    /// <summary>
    /// Competency tag
    /// </summary>
    public string? CompetencyTag { get; set; }

    /// <summary>
    /// Trigger hints
    /// </summary>
    public List<string>? TriggerHints { get; set; }

    /// <summary>
    /// The follow-up question text
    /// </summary>
    public string? CanonicalText { get; set; }

    /// <summary>
    /// Whether paraphrasing is allowed
    /// </summary>
    public bool? AllowParaphrase { get; set; }

    /// <summary>
    /// Whether this follow-up is approved
    /// </summary>
    public bool? IsApproved { get; set; }
}

/// <summary>
/// Represents a follow-up suggestion from AI generation
/// </summary>
public class FollowUpSuggestionResource
{
    /// <summary>
    /// The template ID (if already saved)
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Competency tag
    /// </summary>
    public string? CompetencyTag { get; set; }

    /// <summary>
    /// Trigger hints
    /// </summary>
    public List<string> TriggerHints { get; set; } = new List<string>();

    /// <summary>
    /// The follow-up question text
    /// </summary>
    public string CanonicalText { get; set; } = string.Empty;

    /// <summary>
    /// Whether this suggestion is approved
    /// </summary>
    public bool IsApproved { get; set; }
}

/// <summary>
/// Request model for approving follow-ups
/// </summary>
public class ApproveFollowUpsResource
{
    /// <summary>
    /// List of template IDs to approve
    /// </summary>
    [Required]
    public List<Guid> TemplateIds { get; set; } = new List<Guid>();
}

/// <summary>
/// Response model for follow-up selection at runtime
/// </summary>
public class FollowUpSelectionResponseResource
{
    /// <summary>
    /// The selected follow-up template ID (null if no follow-up selected)
    /// </summary>
    public Guid? SelectedTemplateId { get; set; }

    /// <summary>
    /// The follow-up question text (if selected)
    /// </summary>
    public string? QuestionText { get; set; }

    /// <summary>
    /// The matched competency tag
    /// </summary>
    public string? MatchedCompetencyTag { get; set; }

    /// <summary>
    /// The rationale for selection
    /// </summary>
    public string? Rationale { get; set; }

    /// <summary>
    /// The type of next question: "followup", "main", or "complete"
    /// </summary>
    public string NextQuestionType { get; set; } = "main";
}
