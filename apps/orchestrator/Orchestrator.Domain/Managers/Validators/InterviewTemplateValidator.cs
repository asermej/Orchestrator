namespace Orchestrator.Domain;

internal static class InterviewTemplateValidator
{
    public static void Validate(InterviewTemplate template)
    {
        var errors = new List<string>();

        if (template.GroupId == Guid.Empty)
        {
            errors.Add("GroupId is required.");
        }

        var nameError = ValidatorString.Validate("Name", template.Name);
        if (nameError != null)
        {
            errors.Add(nameError);
        }

        if (errors.Any())
        {
            throw new InterviewTemplateValidationException(string.Join("; ", errors));
        }
    }
}
