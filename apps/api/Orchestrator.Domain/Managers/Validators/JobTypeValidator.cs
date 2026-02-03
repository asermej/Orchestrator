namespace Orchestrator.Domain;

/// <summary>
/// Provides validation methods for the JobType entity.
/// </summary>
internal static class JobTypeValidator
{
    public static void Validate(JobType jobType)
    {
        var errors = new List<string>();

        var nameError = ValidatorString.Validate("Name", jobType.Name);
        if (nameError != null)
        {
            errors.Add(nameError);
        }

        if (jobType.OrganizationId == Guid.Empty)
        {
            errors.Add("OrganizationId is required.");
        }

        if (errors.Any())
        {
            throw new JobTypeValidationException(string.Join("; ", errors));
        }
    }

    public static void ValidateQuestion(InterviewQuestion question)
    {
        var errors = new List<string>();

        var questionTextError = ValidatorString.Validate("QuestionText", question.QuestionText);
        if (questionTextError != null)
        {
            errors.Add(questionTextError);
        }

        if (question.JobTypeId == Guid.Empty)
        {
            errors.Add("JobTypeId is required.");
        }

        if (question.QuestionOrder < 0)
        {
            errors.Add("QuestionOrder must be non-negative.");
        }

        if (errors.Any())
        {
            throw new JobTypeValidationException(string.Join("; ", errors));
        }
    }
}
