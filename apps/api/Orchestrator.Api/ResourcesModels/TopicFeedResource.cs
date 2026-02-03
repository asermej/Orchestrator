using System;
using Orchestrator.Api.Common;

namespace Orchestrator.Api.ResourcesModels;

/// <summary>
/// Represents the Agent that owns a Topic for feed display
/// </summary>
public class TopicAuthorResource
{
    /// <summary>
    /// The unique identifier of the agent
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The first name of the agent
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// The last name of the agent
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// The profile image URL of the agent
    /// </summary>
    public string? ProfileImageUrl { get; set; }
}

/// <summary>
/// Represents a Topic in the feed with enriched data (author info, engagement metrics)
/// </summary>
public class TopicFeedResource
{
    /// <summary>
    /// The unique identifier of the Topic
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The name of the topic
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The description of the topic
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The ID of the agent that owns this topic
    /// </summary>
    public Guid AgentId { get; set; }

    /// <summary>
    /// The agent that owns this topic
    /// </summary>
    public TopicAuthorResource Author { get; set; } = null!;

    /// <summary>
    /// The number of chats using this topic
    /// </summary>
    public int ChatCount { get; set; }

    /// <summary>
    /// The category details
    /// </summary>
    public CategoryResource Category { get; set; } = null!;

    /// <summary>
    /// The tags associated with this topic
    /// </summary>
    public TagResource[] Tags { get; set; } = Array.Empty<TagResource>();

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
/// Request model for searching Topics in the feed
/// </summary>
public class SearchTopicFeedRequest : PaginatedRequest
{
    /// <summary>
    /// Filter by Category ID
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Filter by Tag IDs (topics must have at least one of these tags)
    /// </summary>
    public Guid[]? TagIds { get; set; }

    /// <summary>
    /// Search term for topic name/description
    /// </summary>
    public string? SearchTerm { get; set; }

    // Note: SortBy is inherited from PaginatedRequest
    // Valid values: "popular" (default), "recent", "chat_count"
}

