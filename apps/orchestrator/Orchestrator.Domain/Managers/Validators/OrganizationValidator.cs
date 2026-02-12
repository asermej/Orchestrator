namespace Orchestrator.Domain;

/// <summary>
/// Provides validation methods for the Organization entity.
/// </summary>
internal static class OrganizationValidator
{
    public static void Validate(Organization organization)
    {
        var errors = new List<string>();

        var nameError = ValidatorString.Validate("Name", organization.Name);
        if (nameError != null)
        {
            errors.Add(nameError);
        }

        var apiKeyError = ValidatorString.Validate("ApiKey", organization.ApiKey);
        if (apiKeyError != null)
        {
            errors.Add(apiKeyError);
        }

        if (errors.Any())
        {
            throw new OrganizationValidationException(string.Join("; ", errors));
        }
    }
}
