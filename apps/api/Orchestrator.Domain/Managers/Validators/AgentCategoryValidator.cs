using System;
using System.Collections.Generic;
using System.Linq;

namespace Orchestrator.Domain;

/// <summary>
/// Provides validation methods for the <see cref="AgentCategory"/> entity.
/// </summary>
internal static class AgentCategoryValidator
{
    /// <summary>
    /// Validates the specified AgentCategory instance.
    /// </summary>
    /// <param name="agentCategory">The instance to validate.</param>
    /// <exception cref="AgentCategoryValidationException">Thrown when validation fails.</exception>
    public static void Validate(AgentCategory agentCategory)
    {
        var errors = new List<string>();

        // --- Validation for AgentId ---
        if (agentCategory.AgentId == Guid.Empty)
        {
            errors.Add("AgentId is required");
        }

        // --- Validation for CategoryId ---
        if (agentCategory.CategoryId == Guid.Empty)
        {
            errors.Add("CategoryId is required");
        }

        if (errors.Any())
        {
            throw new AgentCategoryValidationException(string.Join("; ", errors));
        }
    }
}

