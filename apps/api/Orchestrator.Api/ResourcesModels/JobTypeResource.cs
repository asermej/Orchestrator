using System.ComponentModel.DataAnnotations;
using Orchestrator.Api.Common;

namespace Orchestrator.Api.ResourcesModels;

/// <summary>
/// Represents a JobType in API responses
/// </summary>
public class JobTypeResource
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int InterviewDurationMinutes { get; set; }
    public int QuestionCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<InterviewQuestionResource> Questions { get; set; } = new();
}

/// <summary>
/// Request model for creating a new JobType
/// </summary>
public class CreateJobTypeResource
{
    /// <summary>
    /// Optional organization ID. If not provided, the default organization will be used.
    /// </summary>
    public Guid? OrganizationId { get; set; }

    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public List<CreateInterviewQuestionResource>? Questions { get; set; }
}

/// <summary>
/// Request model for updating a JobType
/// </summary>
public class UpdateJobTypeResource
{
    [StringLength(255)]
    public string? Name { get; set; }

    public string? Description { get; set; }

    public bool? IsActive { get; set; }
}

/// <summary>
/// Request model for searching JobTypes
/// </summary>
public class SearchJobTypeRequest : PaginatedRequest
{
    public Guid? OrganizationId { get; set; }
    public string? Name { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// Represents an InterviewQuestion in API responses
/// </summary>
public class InterviewQuestionResource
{
    public Guid Id { get; set; }
    public Guid JobTypeId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public int QuestionOrder { get; set; }
    public bool IsRequired { get; set; }
    public string? FollowUpPrompt { get; set; }
    public int MaxFollowUps { get; set; }
}

/// <summary>
/// Request model for creating an InterviewQuestion
/// </summary>
public class CreateInterviewQuestionResource
{
    [Required]
    public string QuestionText { get; set; } = string.Empty;

    public int QuestionOrder { get; set; }

    public bool IsRequired { get; set; } = true;

    public string? FollowUpPrompt { get; set; }

    public int MaxFollowUps { get; set; } = 2;
}

/// <summary>
/// Request model for updating an InterviewQuestion
/// </summary>
public class UpdateInterviewQuestionResource
{
    public string? QuestionText { get; set; }
    public int? QuestionOrder { get; set; }
    public bool? IsRequired { get; set; }
    public string? FollowUpPrompt { get; set; }
    public int? MaxFollowUps { get; set; }
}
