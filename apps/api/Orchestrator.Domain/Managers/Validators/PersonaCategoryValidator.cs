using System;
using System.Collections.Generic;
using System.Linq;

namespace Orchestrator.Domain;

/// <summary>
/// Provides validation methods for the <see cref="PersonaCategory"/> entity.
/// </summary>
internal static class PersonaCategoryValidator
{
    /// <summary>
    /// Validates the specified PersonaCategory instance.
    /// </summary>
    /// <param name="personaCategory">The instance to validate.</param>
    /// <exception cref="PersonaCategoryValidationException">Thrown when validation fails.</exception>
    public static void Validate(PersonaCategory personaCategory)
    {
        var errors = new List<string>();

        // --- Validation for PersonaId ---
        if (personaCategory.PersonaId == Guid.Empty)
        {
            errors.Add("PersonaId is required");
        }

        // --- Validation for CategoryId ---
        if (personaCategory.CategoryId == Guid.Empty)
        {
            errors.Add("CategoryId is required");
        }

        if (errors.Any())
        {
            throw new PersonaCategoryValidationException(string.Join("; ", errors));
        }
    }
}

