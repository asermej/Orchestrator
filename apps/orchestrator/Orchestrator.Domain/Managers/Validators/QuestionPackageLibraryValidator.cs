namespace Orchestrator.Domain;

internal static class QuestionPackageLibraryValidator
{
    public static void ValidateRoleTemplate(RoleTemplate roleTemplate)
    {
        var errors = new List<string>();

        var nameError = ValidatorString.Validate("RoleName", roleTemplate.RoleName);
        if (nameError != null)
            errors.Add(nameError);

        var industryError = ValidatorString.Validate("Industry", roleTemplate.Industry);
        if (industryError != null)
            errors.Add(industryError);

        if (roleTemplate.Source != "system" && roleTemplate.Source != "custom")
            errors.Add("Source must be either 'system' or 'custom'.");

        if (roleTemplate.Source == "custom" && (!roleTemplate.GroupId.HasValue || roleTemplate.GroupId == Guid.Empty))
            errors.Add("GroupId is required for custom role templates.");

        if (errors.Any())
            throw new QuestionPackageLibraryValidationException(string.Join("; ", errors));
    }

    public static void ValidateCompetency(Competency competency)
    {
        var errors = new List<string>();

        var nameError = ValidatorString.Validate("Name", competency.Name);
        if (nameError != null)
            errors.Add(nameError);

        if (competency.RoleTemplateId == Guid.Empty)
            errors.Add("RoleTemplateId is required.");

        if (competency.DefaultWeight < 0 || competency.DefaultWeight > 100)
            errors.Add("DefaultWeight must be between 0 and 100.");

        if (string.IsNullOrWhiteSpace(competency.CanonicalExample))
            errors.Add("CanonicalExample (example question) is required.");

        if (errors.Any())
            throw new QuestionPackageLibraryValidationException(string.Join("; ", errors));
    }

}
