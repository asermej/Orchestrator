using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Orchestrator.Api.Common;

namespace Orchestrator.Api.ResourcesModels;

/// <summary>
/// Represents a Message in API responses
/// </summary>
public class MessageResource
{
    /// <summary>
    /// The unique identifier of the Message
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The ID of the chat this message belongs to
    /// </summary>
    public Guid ChatId { get; set; }

    /// <summary>
    /// The role of the message sender (user or assistant)
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// The content of the message
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// When this Message was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request model for creating a new Message
/// </summary>
public class CreateMessageResource
{
    /// <summary>
    /// The ID of the chat this message belongs to
    /// </summary>
    [Required(ErrorMessage = "ChatId is required")]
    public Guid ChatId { get; set; }

    /// <summary>
    /// The role of the message sender (user or assistant)
    /// </summary>
    [Required(ErrorMessage = "Role is required")]
    [RegularExpression("^(user|assistant)$", ErrorMessage = "Role must be either 'user' or 'assistant'")]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// The content of the message
    /// </summary>
    [Required(ErrorMessage = "Content is required")]
    [StringLength(10000, ErrorMessage = "Content cannot exceed 10000 characters")]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Request model for sending a user message and receiving an AI response
/// This is the main endpoint for chat interactions with LLM
/// </summary>
public class SendMessageResource
{
    /// <summary>
    /// The ID of the chat this message belongs to
    /// </summary>
    [Required(ErrorMessage = "ChatId is required")]
    public Guid ChatId { get; set; }

    /// <summary>
    /// The user's message content
    /// </summary>
    [Required(ErrorMessage = "Content is required")]
    [StringLength(10000, ErrorMessage = "Content cannot exceed 10000 characters")]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Request model for searching Messages
/// </summary>
public class SearchMessageRequest : PaginatedRequest
{
    /// <summary>
    /// Filter by ChatId (typically used to get all messages in a conversation)
    /// </summary>
    public Guid? ChatId { get; set; }

    /// <summary>
    /// Filter by Role (user or assistant)
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Search within message content
    /// </summary>
    public string? Content { get; set; }
}

