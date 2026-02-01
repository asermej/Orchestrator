using System.Text.RegularExpressions;

namespace Orchestrator.Domain;

/// <summary>
/// Provides validation methods for the <see cref="Message"/> entity.
/// This validator uses conventions based on property names to apply specific format validations.
/// Only includes regex patterns for properties that actually exist and only validates required fields.
/// </summary>
internal static class MessageValidator
{
    private static readonly HashSet<string> ValidRoles = new() { "user", "assistant" };

    /// <summary>
    /// Validates the specified Message instance, applying format checks based on property name conventions.
    /// Review the generated validation logic and adjust required vs optional field validation as needed.
    /// </summary>
    /// <param name="message">The instance to validate.</param>
    /// <exception cref="MessageValidationException">Thrown when validation fails.</exception>
    public static void Validate(Message message)
    {
        var errors = new List<string>();

        // --- Validation for ChatId ---
        if (message.ChatId == Guid.Empty)
        {
            errors.Add("ChatId is required.");
        }

        // --- Validation for Role ---
        var roleValue = message.Role;
        var validationError = ValidatorString.Validate("Role", roleValue);
        if (validationError != null)
        {
            errors.Add(validationError);
        }
        else if (!ValidRoles.Contains(roleValue))
        {
            errors.Add("Role must be either 'user' or 'assistant'.");
        }

        // --- Validation for Content ---
        var contentValue = message.Content;
        validationError = ValidatorString.Validate("Content", contentValue);
        if (validationError != null)
        {
            errors.Add(validationError);
        }

        if (errors.Any())
        {
            throw new MessageValidationException(string.Join("; ", errors));
        }
    }
}

