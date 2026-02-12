using System.ComponentModel.DataAnnotations;
using Orchestrator.Api.Common;

namespace Orchestrator.Api.ResourcesModels;

/// <summary>
/// Represents an Organization in API responses
/// </summary>
public class OrganizationResource
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string? WebhookUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Request model for creating a new Organization
/// </summary>
public class CreateOrganizationResource
{
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [Url]
    public string? WebhookUrl { get; set; }
}

/// <summary>
/// Request model for updating an Organization
/// </summary>
public class UpdateOrganizationResource
{
    [StringLength(255)]
    public string? Name { get; set; }

    [Url]
    public string? WebhookUrl { get; set; }

    public bool? IsActive { get; set; }
}

/// <summary>
/// Request model for searching Organizations
/// </summary>
public class SearchOrganizationRequest : PaginatedRequest
{
    public string? Name { get; set; }
    public bool? IsActive { get; set; }
}
