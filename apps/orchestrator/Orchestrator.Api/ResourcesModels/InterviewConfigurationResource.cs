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
    /// The organization this configuration belongs to
    /// </summary>
    public Guid OrganizationId { get; set; }

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
    /// The scoring rubric for evaluating interview responses
    /// </summary>
    public string? ScoringRubric { get; set; }

    /// <summary>
    /// Whether this configuration is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// The questions in this configuration
    /// </summary>
    public List<InterviewConfigurationQuestionResource> Questions { get; set; } = new();

    /// <summary>
    /// The number of questions in this configuration (for list views)
    /// </summary>
    public int QuestionCount { get; set; }

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
/// Represents a question within an Interview Configuration
/// </summary>
public class InterviewConfigurationQuestionResource
{
    /// <summary>
    /// The unique identifier of the question
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The configuration this question belongs to
    /// </summary>
    public Guid InterviewConfigurationId { get; set; }

    /// <summary>
    /// The question text
    /// </summary>
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// The display order of this question
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// The weight of this question in scoring (default 1.0)
    /// </summary>
    public decimal ScoringWeight { get; set; }

    /// <summary>
    /// Guidance for scoring this question
    /// </summary>
    public string? ScoringGuidance { get; set; }

    /// <summary>
    /// When this question was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this question was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Request model for creating a new Interview Configuration
/// </summary>
public class CreateInterviewConfigurationResource
{
    /// <summary>
    /// The organization this configuration belongs to
    /// </summary>
    [Required(ErrorMessage = "OrganizationId is required")]
    public Guid OrganizationId { get; set; }

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
    /// The scoring rubric for evaluating interview responses
    /// </summary>
    public string? ScoringRubric { get; set; }

    /// <summary>
    /// Whether this configuration is active (default true)
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The questions to create with this configuration
    /// </summary>
    public List<CreateInterviewConfigurationQuestionResource>? Questions { get; set; }

    /// <summary>
    /// Who is creating this configuration
    /// </summary>
    public string? CreatedBy { get; set; }
}

/// <summary>
/// Request model for creating a question within a configuration
/// </summary>
public class CreateInterviewConfigurationQuestionResource
{
    /// <summary>
    /// The question text
    /// </summary>
    [Required(ErrorMessage = "Question is required")]
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// The display order of this question
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// The weight of this question in scoring (default 1.0)
    /// </summary>
    public decimal ScoringWeight { get; set; } = 1.0m;

    /// <summary>
    /// Guidance for scoring this question
    /// </summary>
    public string? ScoringGuidance { get; set; }
}

/// <summary>
/// Request model for updating an Interview Configuration
/// </summary>
public class UpdateInterviewConfigurationResource
{
    /// <summary>
    /// The name of the configuration
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Description of the configuration
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The scoring rubric for evaluating interview responses
    /// </summary>
    public string? ScoringRubric { get; set; }

    /// <summary>
    /// Whether this configuration is active
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// The questions to replace (if provided, replaces all existing questions)
    /// </summary>
    public List<CreateInterviewConfigurationQuestionResource>? Questions { get; set; }

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
    /// Filter by organization ID
    /// </summary>
    public Guid? OrganizationId { get; set; }

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
