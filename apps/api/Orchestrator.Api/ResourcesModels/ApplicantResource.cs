using System.ComponentModel.DataAnnotations;
using Orchestrator.Api.Common;

namespace Orchestrator.Api.ResourcesModels;

/// <summary>
/// Represents an Applicant in API responses
/// </summary>
public class ApplicantResource
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string ExternalApplicantId { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Request model for creating an Applicant (from ATS)
/// </summary>
public class CreateApplicantResource
{
    [Required]
    public string ExternalApplicantId { get; set; } = string.Empty;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public string? Phone { get; set; }
}

/// <summary>
/// Request model for updating an Applicant
/// </summary>
public class UpdateApplicantResource
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public string? Phone { get; set; }
}

/// <summary>
/// Request model for searching Applicants
/// </summary>
public class SearchApplicantRequest : PaginatedRequest
{
    public Guid? OrganizationId { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
}
