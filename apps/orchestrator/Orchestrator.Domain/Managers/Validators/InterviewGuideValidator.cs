namespace Orchestrator.Domain;

/// <summary>
/// Provides validation methods for the InterviewGuide entity.
/// </summary>
internal static class InterviewGuideValidator
{
    public static void Validate(InterviewGuide guide)
    {
        var errors = new List<string>();

        // GroupId is required
        if (guide.GroupId == Guid.Empty)
        {
            errors.Add("GroupId is required.");
        }

        // Name is required
        var nameError = ValidatorString.Validate("Name", guide.Name);
        if (nameError != null)
        {
            errors.Add(nameError);
        }

        // Validate questions if present
        if (guide.Questions != null && guide.Questions.Count > 0)
        {
            for (int i = 0; i < guide.Questions.Count; i++)
            {
                var question = guide.Questions[i];
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
            throw new InterviewGuideValidationException(string.Join("; ", errors));
        }
    }

    public static void ValidateQuestion(InterviewGuideQuestion question)
    {
        var errors = new List<string>();

        if (question.InterviewGuideId == Guid.Empty)
        {
            errors.Add("InterviewGuideId is required.");
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
            throw new InterviewGuideValidationException(string.Join("; ", errors));
        }
    }
}
