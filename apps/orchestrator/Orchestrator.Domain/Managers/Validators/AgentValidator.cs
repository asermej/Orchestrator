using System.Text.RegularExpressions;

namespace Orchestrator.Domain;

/// <summary>
/// Provides validation methods for the Agent entity.
/// </summary>
internal static class AgentValidator
{
    private static readonly Regex UrlRegex = new(@"^(https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}(\.[a-zA-Z0-9()]{1,6})?(:[0-9]{1,5})?(\/[-a-zA-Z0-9()@:%_\+.~#?&//=]*)?|\/[-a-zA-Z0-9()@:%_\+.~#?&//=]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static void Validate(Agent agent)
    {
        var errors = new List<string>();

        // OrganizationId is required
        if (agent.OrganizationId == Guid.Empty)
        {
            errors.Add("OrganizationId is required.");
        }

        // DisplayName is required
        var displayNameError = ValidatorString.Validate("DisplayName", agent.DisplayName);
        if (displayNameError != null)
        {
            errors.Add(displayNameError);
        }

        // ProfileImageUrl is optional - only validate format when value is not empty
        if (!string.IsNullOrWhiteSpace(agent.ProfileImageUrl))
        {
            if (!UrlRegex.IsMatch(agent.ProfileImageUrl))
            {
                errors.Add("ProfileImageUrl has an invalid URL format.");
            }
        }

        if (errors.Any())
        {
            throw new AgentValidationException(string.Join("; ", errors));
        }
    }
}
