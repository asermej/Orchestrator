using System;
using System.ComponentModel.DataAnnotations;
using Orchestrator.Api.Common;

namespace Orchestrator.Api.ResourcesModels;

/// <summary>
/// Represents a Topic in API responses
/// </summary>
public class TopicResource
{
    /// <summary>
    /// The unique identifier of the Topic
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The name of the topic (required)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The description of the topic (optional)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The ID of the category this topic belongs to
    /// </summary>
    public Guid CategoryId { get; set; }

    /// <summary>
    /// The ID of the persona that owns this topic
    /// </summary>
    public Guid PersonaId { get; set; }

    /// <summary>
    /// The storage URL pointing to the training content (file://, s3://, https://, etc.).
    /// Use POST /Topic/{id}/training to save content and GET /Topic/{id}/training to retrieve it.
    /// </summary>
    public string ContentUrl { get; set; } = string.Empty;

    /// <summary>
    /// Optional notes about this topic's content contribution
    /// </summary>
    public string? ContributionNotes { get; set; }

    /// <summary>
    /// The tags associated with this topic
    /// </summary>
    public TagResource[] Tags { get; set; } = Array.Empty<TagResource>();

    /// <summary>
    /// The ID of the user who created this topic (optional)
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// When this Topic was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this Topic was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Request model for creating a new Topic
/// </summary>
public class CreateTopicResource
{
    /// <summary>
    /// The name of the topic (required)
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The description of the topic (optional)
    /// </summary>
    [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// The ID of the category this topic belongs to (required)
    /// </summary>
    [Required(ErrorMessage = "CategoryId is required")]
    public Guid CategoryId { get; set; }

    /// <summary>
    /// The ID of the persona that owns this topic (required)
    /// </summary>
    [Required(ErrorMessage = "PersonaId is required")]
    public Guid PersonaId { get; set; }

    /// <summary>
    /// The training content for this topic (required, up to 50,000 characters)
    /// </summary>
    [Required(ErrorMessage = "Training content is required")]
    [MaxLength(50000, ErrorMessage = "Training content cannot exceed 50,000 characters")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Optional notes about this topic's content contribution (up to 5,000 characters)
    /// </summary>
    [MaxLength(5000, ErrorMessage = "Contribution notes cannot exceed 5,000 characters")]
    public string? ContributionNotes { get; set; }
}

/// <summary>
/// Request model for updating an existing Topic
/// </summary>
public class UpdateTopicResource
{
    /// <summary>
    /// The name of the topic
    /// </summary>
    [MaxLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string? Name { get; set; }

    /// <summary>
    /// The description of the topic
    /// </summary>
    [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// The ID of the category this topic belongs to
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Optional training content to update (up to 50,000 characters)
    /// If provided, the content will be saved and the content URL will be updated automatically
    /// </summary>
    [MaxLength(50000, ErrorMessage = "Training content cannot exceed 50,000 characters")]
    public string? Content { get; set; }

    /// <summary>
    /// Optional notes about this topic's content contribution (up to 5,000 characters)
    /// </summary>
    [MaxLength(5000, ErrorMessage = "Contribution notes cannot exceed 5,000 characters")]
    public string? ContributionNotes { get; set; }
}

/// <summary>
/// Request model for searching Topics
/// </summary>
public class SearchTopicRequest : PaginatedRequest
{
    /// <summary>
    /// Filter by Name (partial match, case insensitive)
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Filter by persona ID (topics owned by a specific persona)
    /// </summary>
    public Guid? PersonaId { get; set; }
}

/// <summary>
/// Request model for saving topic training content
/// </summary>
public class SaveTopicTrainingResource
{
    /// <summary>
    /// The training content to save (up to 50,000 characters)
    /// </summary>
    [Required(ErrorMessage = "Content is required")]
    [MaxLength(50000, ErrorMessage = "Training content cannot exceed 50,000 characters")]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Response model for saving topic training content
/// </summary>
public class SaveTopicTrainingResponse
{
    /// <summary>
    /// The storage URL where the content was saved (file://, s3://, etc.)
    /// </summary>
    public string ContentUrl { get; set; } = string.Empty;
}

/// <summary>
/// Response model for getting topic training content
/// </summary>
public class GetTopicTrainingResponse
{
    /// <summary>
    /// The training content
    /// </summary>
    public string Content { get; set; } = string.Empty;
}

