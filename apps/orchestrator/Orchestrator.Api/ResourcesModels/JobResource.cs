using System.ComponentModel.DataAnnotations;
using Orchestrator.Api.Common;

namespace Orchestrator.Api.ResourcesModels;

/// <summary>
/// Represents a Job in API responses
/// </summary>
public class JobResource
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Guid? OrganizationId { get; set; }
    public string ExternalJobId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Location { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Request model for creating a Job (from ATS)
/// </summary>
public class CreateJobResource
{
    [Required]
    public string ExternalJobId { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Location { get; set; }

    public Guid? OrganizationId { get; set; }
}

/// <summary>
/// Request model for updating a Job
/// </summary>
public class UpdateJobResource
{
    [StringLength(500)]
    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? Location { get; set; }

    public string? Status { get; set; }
}

/// <summary>
/// Request model for searching Jobs
/// </summary>
public class SearchJobRequest : PaginatedRequest
{
    public Guid? GroupId { get; set; }
    public string? Title { get; set; }
    public string? Status { get; set; }
}
