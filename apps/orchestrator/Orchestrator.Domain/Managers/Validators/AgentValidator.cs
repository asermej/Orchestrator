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

        // GroupId is required
        if (agent.GroupId == Guid.Empty)
        {
            errors.Add("GroupId is required.");
        }

        // OrganizationId is required for new agents
        if (!agent.OrganizationId.HasValue || agent.OrganizationId.Value == Guid.Empty)
        {
            errors.Add("OrganizationId is required. Please select an organization.");
        }

        // DisplayName is required
        var displayNameError = ValidatorString.Validate("DisplayName", agent.DisplayName);
        if (displayNameError != null)
        {
            errors.Add(displayNameError);
        }

        // VisibilityScope must be a valid value
        if (!Domain.VisibilityScope.IsValid(agent.VisibilityScope))
        {
            errors.Add($"VisibilityScope must be one of: {string.Join(", ", Domain.VisibilityScope.AllValues)}.");
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
