using System;
using System.ComponentModel.DataAnnotations;

namespace Orchestrator.Api.ResourcesModels;

/// <summary>
/// Request model for adding a topic to a chat
/// </summary>
public class AddTopicToChatResource
{
    /// <summary>
    /// The topic ID to add
    /// </summary>
    [Required(ErrorMessage = "TopicId is required")]
    public Guid TopicId { get; set; }
}

