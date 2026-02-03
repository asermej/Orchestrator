namespace Orchestrator.Domain;

/// <summary>
/// Provides validation methods for the Interview entity.
/// </summary>
internal static class InterviewValidator
{
    public static void Validate(Interview interview)
    {
        var errors = new List<string>();

        if (interview.JobId == Guid.Empty)
        {
            errors.Add("JobId is required.");
        }

        if (interview.ApplicantId == Guid.Empty)
        {
            errors.Add("ApplicantId is required.");
        }

        if (interview.AgentId == Guid.Empty)
        {
            errors.Add("AgentId is required.");
        }

        var tokenError = ValidatorString.Validate("Token", interview.Token);
        if (tokenError != null)
        {
            errors.Add(tokenError);
        }

        if (errors.Any())
        {
            throw new InterviewValidationException(string.Join("; ", errors));
        }
    }

    public static void ValidateResponse(InterviewResponse response)
    {
        var errors = new List<string>();

        if (response.InterviewId == Guid.Empty)
        {
            errors.Add("InterviewId is required.");
        }

        // QuestionId is optional for test interviews (uses configuration questions instead)
        if (response.QuestionId.HasValue && response.QuestionId.Value == Guid.Empty)
        {
            errors.Add("QuestionId cannot be empty Guid.");
        }

        var questionTextError = ValidatorString.Validate("QuestionText", response.QuestionText);
        if (questionTextError != null)
        {
            errors.Add(questionTextError);
        }

        if (errors.Any())
        {
            throw new InterviewValidationException(string.Join("; ", errors));
        }
    }

    public static void ValidateResult(InterviewResult result)
    {
        var errors = new List<string>();

        if (result.InterviewId == Guid.Empty)
        {
            errors.Add("InterviewId is required.");
        }

        if (errors.Any())
        {
            throw new InterviewValidationException(string.Join("; ", errors));
        }
    }
}
