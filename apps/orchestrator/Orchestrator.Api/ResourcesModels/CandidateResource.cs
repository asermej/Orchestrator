namespace Orchestrator.Api.ResourcesModels;

/// <summary>
/// Request to redeem an invite short code and create a candidate session
/// </summary>
public class RedeemInviteRequest
{
    public string ShortCode { get; set; } = string.Empty;
}

/// <summary>
/// Response returned after successfully redeeming an invite
/// </summary>
public class CandidateSessionResponse
{
    public string Token { get; set; } = string.Empty;
    public CandidateInterviewResource Interview { get; set; } = null!;
    public CandidateAgentResource? Agent { get; set; }
    public CandidateJobResource? Job { get; set; }
    public CandidateApplicantResource? Applicant { get; set; }
    public List<CandidateQuestionResource> Questions { get; set; } = new();
}

/// <summary>
/// Candidate-facing question resource (only what's needed for the interview experience)
/// </summary>
public class CandidateQuestionResource
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool FollowUpsEnabled { get; set; }
    public int MaxFollowUps { get; set; }
}

/// <summary>
/// Candidate-facing interview resource (limited fields, no PII leakage)
/// </summary>
public class CandidateInterviewResource
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string InterviewType { get; set; } = string.Empty;
    public int CurrentQuestionIndex { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Candidate-facing agent resource (only what the candidate needs to see)
/// </summary>
public class CandidateAgentResource
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
}

/// <summary>
/// Candidate-facing job resource (only title)
/// </summary>
public class CandidateJobResource
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
}

/// <summary>
/// Candidate-facing applicant resource (only first name for personalization)
/// </summary>
public class CandidateApplicantResource
{
    public string FirstName { get; set; } = string.Empty;
}

/// <summary>
/// Response for audio upload operations
/// </summary>
public class AudioUploadResponse
{
    /// <summary>
    /// The API-relative URL for the uploaded audio
    /// </summary>
    public string Url { get; set; } = string.Empty;
}

/// <summary>
/// Interview invite resource for admin/ATS consumers
/// </summary>
public class InterviewInviteResource
{
    public Guid Id { get; set; }
    public Guid InterviewId { get; set; }
    public Guid OrganizationId { get; set; }
    public string ShortCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int MaxUses { get; set; }
    public int UseCount { get; set; }
    public string? InviteUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
