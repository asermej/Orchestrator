namespace Orchestrator.Domain;

/// <summary>
/// Provides validation methods for the Group entity.
/// </summary>
internal static class GroupValidator
{
    public static void Validate(Group group)
    {
        var errors = new List<string>();

        var nameError = ValidatorString.Validate("Name", group.Name);
        if (nameError != null)
        {
            errors.Add(nameError);
        }

        var apiKeyError = ValidatorString.Validate("ApiKey", group.ApiKey);
        if (apiKeyError != null)
        {
            errors.Add(apiKeyError);
        }

        if (errors.Any())
        {
            throw new GroupValidationException(string.Join("; ", errors));
        }
    }
}
