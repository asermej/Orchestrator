using System.Text.RegularExpressions;

namespace Orchestrator.Domain;

/// <summary>
/// Provides validation methods for the <see cref="User"/> entity.
/// This validator uses conventions based on property names to apply specific format validations.
/// Only includes regex patterns for properties that actually exist and only validates required fields.
/// </summary>
internal static class UserValidator
{
    // Regex for email validation.
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    // Regex for phone number validation (supports international format).
    private static readonly Regex PhoneRegex = new(@"^[\+]?[1-9][\d]{0,15}$", RegexOptions.Compiled);

    /// <summary>
    /// Validates the specified User instance, applying format checks based on property name conventions.
    /// Review the generated validation logic and adjust required vs optional field validation as needed.
    /// </summary>
    /// <param name="user">The instance to validate.</param>
    /// <exception cref="UserValidationException">Thrown when validation fails.</exception>
    public static void Validate(User user)
    {
        var errors = new List<string>();

        // --- Validation for FirstName ---
        // FirstName is optional (Auth0 users may not provide it)
        var firstNameValue = user.FirstName;
        if (!string.IsNullOrWhiteSpace(firstNameValue))
        {
            // No additional format validation needed for first name
        }
        
        // --- Validation for LastName ---
        // LastName is optional (Auth0 users may not provide it)
        var lastNameValue = user.LastName;
        if (!string.IsNullOrWhiteSpace(lastNameValue))
        {
            // No additional format validation needed for last name
        }
        
        // --- Validation for Email ---
        var emailValue = user.Email;
        
        var validationError = ValidatorString.Validate("Email", emailValue);
        if (validationError != null)
        {
            errors.Add(validationError);
        }
        else
        {
            if (!EmailRegex.IsMatch(emailValue)) { errors.Add("Email has an invalid email format."); }
        }
        
        // --- Validation for Phone ---
        var phoneValue = user.Phone;
        
        // Phone is optional - only validate format when value is not empty
        if (!string.IsNullOrWhiteSpace(phoneValue))
        {
            if (!PhoneRegex.IsMatch(phoneValue)) { errors.Add("Phone has an invalid phone format."); }
        }

        if (errors.Any())
        {
            throw new UserValidationException(string.Join("; ", errors));
        }
    }
} 