using System.Text.RegularExpressions;

namespace Orchestrator.Domain;

/// <summary>
/// Provides validation methods for the <see cref="Tag"/> entity.
/// </summary>
internal static class TagValidator
{
    // Regex for tag name - only lowercase alphanumeric, hyphens, and underscores
    private static readonly Regex TagNameRegex = new(@"^[a-z0-9_-]+$", RegexOptions.Compiled);
    private const int MaxTagNameLength = 50;

    /// <summary>
    /// Validates and normalizes the specified Tag instance.
    /// Tag names are automatically normalized to lowercase and trimmed.
    /// </summary>
    /// <param name="tag">The instance to validate.</param>
    /// <exception cref="TagValidationException">Thrown when validation fails.</exception>
    public static void Validate(Tag tag)
    {
        var errors = new List<string>();

        // --- Validation for Name ---
        var nameValue = tag.Name;
        
        // Normalize: trim and convert to lowercase
        nameValue = nameValue?.Trim().ToLowerInvariant() ?? string.Empty;
        tag.Name = nameValue;
        
        // Name is required
        var validationError = ValidatorString.Validate("Name", nameValue, MaxTagNameLength);
        if (validationError != null)
        {
            errors.Add(validationError);
        }
        else if (!TagNameRegex.IsMatch(nameValue))
        {
            errors.Add("Name can only contain lowercase letters, numbers, hyphens, and underscores.");
        }

        if (errors.Any())
        {
            throw new TagValidationException(string.Join("; ", errors));
        }
    }
}

