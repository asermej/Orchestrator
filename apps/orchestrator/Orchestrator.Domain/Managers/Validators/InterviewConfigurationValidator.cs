namespace Orchestrator.Domain;

/// <summary>
/// Provides validation methods for the InterviewConfiguration entity.
/// </summary>
internal static class InterviewConfigurationValidator
{
    public static void Validate(InterviewConfiguration config)
    {
        var errors = new List<string>();

        // GroupId is required
        if (config.GroupId == Guid.Empty)
        {
            errors.Add("GroupId is required.");
        }

        // InterviewGuideId is required
        if (config.InterviewGuideId == Guid.Empty)
        {
            errors.Add("InterviewGuideId is required.");
        }

        // AgentId is required
        if (config.AgentId == Guid.Empty)
        {
            errors.Add("AgentId is required.");
        }

        // Name is required
        var nameError = ValidatorString.Validate("Name", config.Name);
        if (nameError != null)
        {
            errors.Add(nameError);
        }

        if (errors.Any())
        {
            throw new InterviewConfigurationValidationException(string.Join("; ", errors));
        }
    }
}
