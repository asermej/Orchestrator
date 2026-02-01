using System;
using System.Collections.Generic;
using System.Linq;

namespace Orchestrator.Domain;

/// <summary>
/// Provides validation methods for the <see cref="ChatTopic"/> entity.
/// </summary>
internal static class ChatTopicValidator
{
    /// <summary>
    /// Validates the specified ChatTopic instance.
    /// </summary>
    /// <param name="chatTopic">The instance to validate.</param>
    /// <exception cref="ChatTopicValidationException">Thrown when validation fails.</exception>
    public static void Validate(ChatTopic chatTopic)
    {
        var errors = new List<string>();

        if (chatTopic.ChatId == Guid.Empty)
        {
            errors.Add("ChatId is required and cannot be empty.");
        }

        if (chatTopic.TopicId == Guid.Empty)
        {
            errors.Add("TopicId is required and cannot be empty.");
        }

        if (errors.Any())
        {
            throw new ChatTopicValidationException(string.Join("; ", errors));
        }
    }
}

