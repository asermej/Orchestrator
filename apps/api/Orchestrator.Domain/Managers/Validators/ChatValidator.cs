using System.Text.RegularExpressions;

namespace Orchestrator.Domain;

/// <summary>
/// Provides validation methods for the <see cref="Chat"/> entity.
/// This validator uses conventions based on property names to apply specific format validations.
/// Only includes regex patterns for properties that actually exist and only validates required fields.
/// </summary>
internal static class ChatValidator
{
    /// <summary>
    /// Validates the specified Chat instance, applying format checks based on property name conventions.
    /// Review the generated validation logic and adjust required vs optional field validation as needed.
    /// </summary>
    /// <param name="chat">The instance to validate.</param>
    /// <exception cref="ChatValidationException">Thrown when validation fails.</exception>
    public static void Validate(Chat chat)
    {
        var errors = new List<string>();

        // --- Validation for AgentId ---
        if (chat.AgentId == Guid.Empty)
        {
            errors.Add("AgentId is required.");
        }

        // --- Validation for UserId ---
        if (chat.UserId == Guid.Empty)
        {
            errors.Add("UserId is required.");
        }

        // --- Validation for Title ---
        var titleValue = chat.Title;
        
        // Title is optional - no validation needed when null or empty

        // --- Validation for LastMessageAt ---
        if (chat.LastMessageAt == default)
        {
            errors.Add("LastMessageAt is required.");
        }

        if (errors.Any())
        {
            throw new ChatValidationException(string.Join("; ", errors));
        }
    }
}

