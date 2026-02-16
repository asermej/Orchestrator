using System.ComponentModel.DataAnnotations;
using Orchestrator.Api.Common;

namespace Orchestrator.Api.ResourcesModels;

/// <summary>
/// Represents a Group in API responses
/// </summary>
public class GroupResource
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string? WebhookUrl { get; set; }
    public bool IsActive { get; set; }
    public Guid? ExternalGroupId { get; set; }
    public string? AtsBaseUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Request model for creating a new Group
/// </summary>
public class CreateGroupResource
{
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [Url]
    public string? WebhookUrl { get; set; }

    public Guid? ExternalGroupId { get; set; }

    [Url]
    public string? AtsBaseUrl { get; set; }
}

/// <summary>
/// Request model for updating a Group
/// </summary>
public class UpdateGroupResource
{
    [StringLength(255)]
    public string? Name { get; set; }

    [Url]
    public string? WebhookUrl { get; set; }

    public bool? IsActive { get; set; }

    public Guid? ExternalGroupId { get; set; }

    [Url]
    public string? AtsBaseUrl { get; set; }
}

/// <summary>
/// Request model for updating webhook URL via ATS API
/// </summary>
public class UpdateWebhookResource
{
    public string? WebhookUrl { get; set; }
}

/// <summary>
/// Request model for searching Groups
/// </summary>
public class SearchGroupRequest : PaginatedRequest
{
    public string? Name { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// Request model for ATS group sync (upsert by external group ID)
/// </summary>
public class SyncGroupResource
{
    [Required]
    public Guid ExternalGroupId { get; set; }

    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [Url]
    public string? AtsBaseUrl { get; set; }

    [Url]
    public string? WebhookUrl { get; set; }

    /// <summary>
    /// The ATS's own API key for authenticating Orchestrator→ATS callbacks
    /// (e.g. user-access and organization queries).
    /// </summary>
    [StringLength(255)]
    public string? AtsApiKey { get; set; }
}

/// <summary>
/// Response model for ATS group sync (includes the API key the ATS should store)
/// </summary>
public class SyncGroupResponseResource
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public Guid ExternalGroupId { get; set; }
    public bool IsNew { get; set; }
}
