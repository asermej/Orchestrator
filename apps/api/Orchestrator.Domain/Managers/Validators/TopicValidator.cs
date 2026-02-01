using System;
using System.Collections.Generic;
using System.Linq;

namespace Orchestrator.Domain;

/// <summary>
/// Provides validation methods for the <see cref="Topic"/> entity.
/// </summary>
internal static class TopicValidator
{
    /// <summary>
    /// Validates the specified Topic instance.
    /// </summary>
    /// <param name="topic">The instance to validate.</param>
    /// <exception cref="TopicValidationException">Thrown when validation fails.</exception>
    public static void Validate(Topic topic)
    {
        var errors = new List<string>();

        // --- Validation for Name ---
        var nameValue = topic.Name;
        
        // Name is required
        var validationError = ValidatorString.Validate("Name", nameValue);
        if (validationError != null)
        {
            errors.Add(validationError);
        }

        // --- Validation for Description ---
        // Description is optional - no validation needed when null or empty

        // --- Validation for CategoryId ---
        if (topic.CategoryId == Guid.Empty)
        {
            errors.Add("CategoryId is required");
        }

        // --- Validation for PersonaId ---
        if (topic.PersonaId == Guid.Empty)
        {
            errors.Add("PersonaId is required");
        }

        // --- Validation for ContentUrl ---
        // Skip ContentUrl validation for NEW topics (Id == Guid.Empty)
        // It will be populated after creation when training content is saved
        // For existing topics (updates), ContentUrl is required
        if (topic.Id != Guid.Empty)
        {
            var contentUrlValue = topic.ContentUrl;
            var contentUrlValidationError = ValidatorString.Validate("ContentUrl", contentUrlValue);
            if (contentUrlValidationError != null)
            {
                errors.Add(contentUrlValidationError);
            }
        }

        // --- Validation for ContributionNotes ---
        // ContributionNotes is optional - no validation needed

        // --- Validation for CreatedBy ---
        // CreatedBy is optional string - no validation needed

        if (errors.Any())
        {
            throw new TopicValidationException(string.Join("; ", errors));
        }
    }
}

