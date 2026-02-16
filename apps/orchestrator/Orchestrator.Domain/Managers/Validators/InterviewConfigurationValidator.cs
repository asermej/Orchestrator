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

        // Validate questions if present
        if (config.Questions != null && config.Questions.Count > 0)
        {
            for (int i = 0; i < config.Questions.Count; i++)
            {
                var question = config.Questions[i];
                if (string.IsNullOrWhiteSpace(question.Question))
                {
                    errors.Add($"Question at position {i + 1} cannot be empty.");
                }
                if (question.ScoringWeight < 0)
                {
                    errors.Add($"Question at position {i + 1} has an invalid scoring weight (must be >= 0).");
                }
            }
        }

        if (errors.Any())
        {
            throw new InterviewConfigurationValidationException(string.Join("; ", errors));
        }
    }

    public static void ValidateQuestion(InterviewConfigurationQuestion question)
    {
        var errors = new List<string>();

        if (question.InterviewConfigurationId == Guid.Empty)
        {
            errors.Add("InterviewConfigurationId is required.");
        }

        if (string.IsNullOrWhiteSpace(question.Question))
        {
            errors.Add("Question text is required.");
        }

        if (question.ScoringWeight < 0)
        {
            errors.Add("ScoringWeight must be >= 0.");
        }

        if (errors.Any())
        {
            throw new InterviewConfigurationValidationException(string.Join("; ", errors));
        }
    }
}
