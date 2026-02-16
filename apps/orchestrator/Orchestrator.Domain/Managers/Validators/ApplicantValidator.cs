using System.Text.RegularExpressions;

namespace Orchestrator.Domain;

/// <summary>
/// Provides validation methods for the Applicant entity.
/// </summary>
internal static class ApplicantValidator
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static void Validate(Applicant applicant)
    {
        var errors = new List<string>();

        if (applicant.GroupId == Guid.Empty)
        {
            errors.Add("GroupId is required.");
        }

        var externalIdError = ValidatorString.Validate("ExternalApplicantId", applicant.ExternalApplicantId);
        if (externalIdError != null)
        {
            errors.Add(externalIdError);
        }

        // Email is optional but must be valid if provided
        if (!string.IsNullOrWhiteSpace(applicant.Email) && !EmailRegex.IsMatch(applicant.Email))
        {
            errors.Add("Email has an invalid format.");
        }

        if (errors.Any())
        {
            throw new ApplicantValidationException(string.Join("; ", errors));
        }
    }
}
