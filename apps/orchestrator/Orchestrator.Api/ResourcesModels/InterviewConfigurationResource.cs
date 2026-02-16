using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Orchestrator.Api.Common;

namespace Orchestrator.Api.ResourcesModels;

/// <summary>
/// Represents an Interview Configuration in API responses
/// </summary>
public class InterviewConfigurationResource
{
    /// <summary>
    /// The unique identifier of the Interview Configuration
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The group this configuration belongs to
    /// </summary>
    public Guid GroupId { get; set; }

    /// <summary>
    /// The ATS organization this configuration is scoped to (null = group-wide)
    /// </summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>
    /// The interview guide used by this configuration
    /// </summary>
    public Guid InterviewGuideId { get; set; }

    /// <summary>
    /// The agent that conducts interviews using this configuration
    /// </summary>
    public Guid AgentId { get; set; }

    /// <summary>
    /// The name of the configuration
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the configuration
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this configuration is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// The number of questions from the linked interview guide (for list views)
    /// </summary>
    public int QuestionCount { get; set; }

    /// <summary>
    /// The interview guide resource (populated when requested)
    /// </summary>
    public InterviewGuideResource? InterviewGuide { get; set; }

    /// <summary>
    /// The agent resource (populated when requested)
    /// </summary>
    public AgentResource? Agent { get; set; }

    /// <summary>
    /// When this configuration was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this configuration was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Who created this configuration
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Who last updated this configuration
    /// </summary>
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Request model for creating a new Interview Configuration
/// </summary>
public class CreateInterviewConfigurationResource
{
    /// <summary>
    /// The group this configuration belongs to
    /// </summary>
    [Required(ErrorMessage = "GroupId is required")]
    public Guid GroupId { get; set; }

    /// <summary>
    /// The ATS organization this configuration is scoped to (null = group-wide)
    /// </summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>
    /// The interview guide to use for this configuration
    /// </summary>
    [Required(ErrorMessage = "InterviewGuideId is required")]
    public Guid InterviewGuideId { get; set; }

    /// <summary>
    /// The agent that conducts interviews using this configuration
    /// </summary>
    [Required(ErrorMessage = "AgentId is required")]
    public Guid AgentId { get; set; }

    /// <summary>
    /// The name of the configuration
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the configuration
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this configuration is active (default true)
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Who is creating this configuration
    /// </summary>
    public string? CreatedBy { get; set; }
}

/// <summary>
/// Request model for updating an Interview Configuration
/// </summary>
public class UpdateInterviewConfigurationResource
{
    /// <summary>
    /// The interview guide to use
    /// </summary>
    public Guid? InterviewGuideId { get; set; }

    /// <summary>
    /// The name of the configuration
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Description of the configuration
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this configuration is active
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Who is updating this configuration
    /// </summary>
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Lightweight interview configuration resource for ATS integration endpoints
/// </summary>
public class AtsInterviewConfigurationResource
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid AgentId { get; set; }
    public string? AgentDisplayName { get; set; }
    public int QuestionCount { get; set; }
}

/// <summary>
/// Request model for searching Interview Configurations
/// </summary>
public class SearchInterviewConfigurationRequest : PaginatedRequest
{
    /// <summary>
    /// Filter by group ID
    /// </summary>
    public Guid? GroupId { get; set; }

    /// <summary>
    /// Filter by agent ID
    /// </summary>
    public Guid? AgentId { get; set; }

    /// <summary>
    /// Filter by name (partial match)
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Filter by active status
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Sort by field (e.g., "name", "createdAt")
    /// </summary>
    public new string? SortBy { get; set; }
}
