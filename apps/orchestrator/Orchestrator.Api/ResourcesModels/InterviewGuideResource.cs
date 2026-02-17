using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Orchestrator.Api.Common;

namespace Orchestrator.Api.ResourcesModels;

/// <summary>
/// Represents an Interview Guide in API responses
/// </summary>
public class InterviewGuideResource
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Guid? OrganizationId { get; set; }
    public string VisibilityScope { get; set; } = "organization_only";
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? OpeningTemplate { get; set; }
    public string? ClosingTemplate { get; set; }
    public string? ScoringRubric { get; set; }
    public bool IsActive { get; set; }
    public List<InterviewGuideQuestionResource> Questions { get; set; } = new();
    public int QuestionCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Whether this guide is inherited from a parent organization
    /// </summary>
    public bool IsInherited { get; set; }

    /// <summary>
    /// The name of the organization that owns this guide (for inherited guides)
    /// </summary>
    public string? OwnerOrganizationName { get; set; }
}

/// <summary>
/// Represents a question within an Interview Guide
/// </summary>
public class InterviewGuideQuestionResource
{
    public Guid Id { get; set; }
    public Guid InterviewGuideId { get; set; }
    public string Question { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public decimal ScoringWeight { get; set; }
    public string? ScoringGuidance { get; set; }
    public bool FollowUpsEnabled { get; set; }
    public int MaxFollowUps { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Request model for creating a new Interview Guide
/// </summary>
public class CreateInterviewGuideResource
{
    [Required(ErrorMessage = "GroupId is required")]
    public Guid GroupId { get; set; }

    public Guid? OrganizationId { get; set; }

    /// <summary>
    /// The visibility scope (organization_only, organization_and_descendants, descendants_only)
    /// </summary>
    public string? VisibilityScope { get; set; }

    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? OpeningTemplate { get; set; }
    public string? ClosingTemplate { get; set; }
    public string? ScoringRubric { get; set; }
    public bool IsActive { get; set; } = true;
    public List<CreateInterviewGuideQuestionResource>? Questions { get; set; }
    public string? CreatedBy { get; set; }
}

/// <summary>
/// Request model for creating a question within a guide
/// </summary>
public class CreateInterviewGuideQuestionResource
{
    [Required(ErrorMessage = "Question is required")]
    public string Question { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }
    public decimal ScoringWeight { get; set; } = 1.0m;
    public string? ScoringGuidance { get; set; }
    public bool FollowUpsEnabled { get; set; } = true;
    public int MaxFollowUps { get; set; } = 2;
}

/// <summary>
/// Request model for updating an Interview Guide
/// </summary>
public class UpdateInterviewGuideResource
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? OpeningTemplate { get; set; }
    public string? ClosingTemplate { get; set; }
    public string? ScoringRubric { get; set; }
    public bool? IsActive { get; set; }

    /// <summary>
    /// The visibility scope (organization_only, organization_and_descendants, descendants_only)
    /// </summary>
    public string? VisibilityScope { get; set; }

    public List<CreateInterviewGuideQuestionResource>? Questions { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Request model for searching Interview Guides
/// </summary>
public class SearchInterviewGuideRequest : PaginatedRequest
{
    public Guid? GroupId { get; set; }
    public string? Name { get; set; }
    public bool? IsActive { get; set; }
    public new string? SortBy { get; set; }

    /// <summary>
    /// Filter by source: "local" for guides created at the current org,
    /// "inherited" for guides from ancestor orgs. Omit for legacy behavior.
    /// </summary>
    public string? Source { get; set; }
}

/// <summary>
/// Lightweight interview guide resource for ATS integration endpoints
/// </summary>
public class AtsInterviewGuideResource
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int QuestionCount { get; set; }
    public bool IsActive { get; set; }
}
