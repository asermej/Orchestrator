using System;
using System.ComponentModel.DataAnnotations;
using Orchestrator.Api.Common;

namespace Orchestrator.Api.ResourcesModels;

/// <summary>
/// Represents a Tag in API responses
/// </summary>
public class TagResource
{
    /// <summary>
    /// The unique identifier of the Tag
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The name of the tag (lowercase, alphanumeric with hyphens and underscores)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// When this Tag was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this Tag was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Request model for creating a new Tag
/// </summary>
public class CreateTagResource
{
    /// <summary>
    /// The name of the tag (will be normalized to lowercase)
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(50, ErrorMessage = "Name cannot exceed 50 characters")]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Name can only contain letters, numbers, hyphens, and underscores")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Request model for searching Tags
/// </summary>
public class SearchTagRequest : PaginatedRequest
{
    /// <summary>
    /// Search term for tag name (partial match, case insensitive)
    /// </summary>
    public string? SearchTerm { get; set; }
}

/// <summary>
/// Request model for adding a tag to a topic
/// </summary>
public class AddTagToTopicResource
{
    /// <summary>
    /// The name of the tag to add (will be normalized to lowercase)
    /// </summary>
    [Required(ErrorMessage = "TagName is required")]
    [MaxLength(50, ErrorMessage = "TagName cannot exceed 50 characters")]
    public string TagName { get; set; } = string.Empty;
}

/// <summary>
/// Request model for updating all tags for a topic
/// </summary>
public class UpdateTopicTagsResource
{
    /// <summary>
    /// The list of tag names to associate with the topic
    /// </summary>
    [Required(ErrorMessage = "TagNames is required")]
    public string[] TagNames { get; set; } = Array.Empty<string>();
}

