using System.Collections.Generic;
using System.Linq;

namespace Orchestrator.Domain;

/// <summary>
/// Provides validation methods for the <see cref="Category"/> entity.
/// </summary>
internal static class CategoryValidator
{
    private static readonly string[] ValidCategoryTypes = { "Standard", "DailyBlog" };

    /// <summary>
    /// Validates the specified Category instance.
    /// </summary>
    /// <param name="category">The instance to validate.</param>
    /// <exception cref="CategoryValidationException">Thrown when validation fails.</exception>
    public static void Validate(Category category)
    {
        var errors = new List<string>();

        // --- Validation for Name ---
        var nameValue = category.Name;
        
        // Name is required
        var validationError = ValidatorString.Validate("Name", nameValue);
        if (validationError != null)
        {
            errors.Add(validationError);
        }

        // --- Validation for CategoryType ---
        if (!ValidCategoryTypes.Contains(category.CategoryType))
        {
            errors.Add($"CategoryType must be one of: {string.Join(", ", ValidCategoryTypes)}");
        }

        // --- Validation for Description ---
        // Description is optional - no validation needed when null or empty

        // --- Validation for DisplayOrder ---
        // DisplayOrder is an int with default value - no validation needed

        // --- Validation for IsActive ---
        // IsActive is a bool with default value - no validation needed

        if (errors.Any())
        {
            throw new CategoryValidationException(string.Join("; ", errors));
        }
    }
}

