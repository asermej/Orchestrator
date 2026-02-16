namespace Orchestrator.Domain;

/// <summary>
/// Provides validation methods for the Job entity.
/// </summary>
internal static class JobValidator
{
    public static void Validate(Job job)
    {
        var errors = new List<string>();

        if (job.GroupId == Guid.Empty)
        {
            errors.Add("GroupId is required.");
        }

        var externalIdError = ValidatorString.Validate("ExternalJobId", job.ExternalJobId);
        if (externalIdError != null)
        {
            errors.Add(externalIdError);
        }

        var titleError = ValidatorString.Validate("Title", job.Title);
        if (titleError != null)
        {
            errors.Add(titleError);
        }

        if (errors.Any())
        {
            throw new JobValidationException(string.Join("; ", errors));
        }
    }
}
