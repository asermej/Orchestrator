using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Orchestrator.Api.Common;

namespace Orchestrator.Api.ResourcesModels;

/// <summary>
/// Represents a Chat in API responses
/// </summary>
public class ChatResource
{
    /// <summary>
    /// The unique identifier of the Chat
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The ID of the agent this chat is with
    /// </summary>
    public Guid AgentId { get; set; }

    /// <summary>
    /// The ID of the user who owns this chat
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The title of the chat (optional)
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// When the last message was sent in this chat
    /// </summary>
    public DateTime LastMessageAt { get; set; }

    /// <summary>
    /// When this Chat was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this Chat was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Request model for creating a new Chat
/// </summary>
public class CreateChatResource
{
    /// <summary>
    /// The ID of the agent this chat is with
    /// </summary>
    [Required(ErrorMessage = "AgentId is required")]
    public Guid AgentId { get; set; }

    /// <summary>
    /// The ID of the user who owns this chat
    /// </summary>
    [Required(ErrorMessage = "UserId is required")]
    public Guid UserId { get; set; }

    /// <summary>
    /// The title of the chat (optional)
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// When the last message was sent in this chat
    /// </summary>
    [Required(ErrorMessage = "LastMessageAt is required")]
    public DateTime LastMessageAt { get; set; }
}

/// <summary>
/// Request model for updating an existing Chat
/// </summary>
public class UpdateChatResource
{
    /// <summary>
    /// The title of the chat
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// When the last message was sent in this chat
    /// </summary>
    public DateTime? LastMessageAt { get; set; }
}

/// <summary>
/// Request model for searching Chats
/// </summary>
public class SearchChatRequest : PaginatedRequest
{
    /// <summary>
    /// Filter by AgentId
    /// </summary>
    public Guid? AgentId { get; set; }

    /// <summary>
    /// Filter by UserId
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Filter by Title
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Filter by date range
    /// </summary>
    public DateTimeRange? CreatedAtRange { get; set; }

    /// <summary>
    /// Filter by date range
    /// </summary>
    public DateTimeRange? UpdatedAtRange { get; set; }
}

