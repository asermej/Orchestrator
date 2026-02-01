using System.Text.RegularExpressions;

namespace Orchestrator.Domain;

/// <summary>
/// Provides validation methods for the <see cref="Persona"/> entity.
/// This validator uses conventions based on property names to apply specific format validations.
/// Only includes regex patterns for properties that actually exist and only validates required fields.
/// </summary>
internal static class PersonaValidator
{
    // Regex for URL validation - accepts both full URLs and relative paths
    // Full URL: https://example.com/path or http://localhost:5000/path
    // Relative path: /uploads/personas/xxx.jpg
    private static readonly Regex UrlRegex = new(@"^(https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}(\.[a-zA-Z0-9()]{1,6})?(:[0-9]{1,5})?(\/[-a-zA-Z0-9()@:%_\+.~#?&//=]*)?|\/[-a-zA-Z0-9()@:%_\+.~#?&//=]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Validates the specified Persona instance, applying format checks based on property name conventions.
    /// Review the generated validation logic and adjust required vs optional field validation as needed.
    /// </summary>
    /// <param name="persona">The instance to validate.</param>
    /// <exception cref="PersonaValidationException">Thrown when validation fails.</exception>
    public static void Validate(Persona persona)
    {
        var errors = new List<string>();

        // --- Validation for FirstName ---
        var firstNameValue = persona.FirstName;
        
        // FirstName is optional - no validation needed when null or empty

        // --- Validation for LastName ---
        var lastNameValue = persona.LastName;
        
        // LastName is optional - no validation needed when null or empty

        // --- Validation for DisplayName ---
        var displayNameValue = persona.DisplayName;
        
        // DisplayName is required
        var validationError = ValidatorString.Validate("DisplayName", displayNameValue);
        if (validationError != null)
        {
            errors.Add(validationError);
        }

        // --- Validation for ProfileImageUrl ---
        var profileImageUrlValue = persona.ProfileImageUrl;
        
        // ProfileImageUrl is optional - only validate format when value is not empty
        if (!string.IsNullOrWhiteSpace(profileImageUrlValue))
        {
            if (!UrlRegex.IsMatch(profileImageUrlValue)) { errors.Add("ProfileImageUrl has an invalid URL format."); }
        }

        if (errors.Any())
        {
            throw new PersonaValidationException(string.Join("; ", errors));
        }
    }
}

