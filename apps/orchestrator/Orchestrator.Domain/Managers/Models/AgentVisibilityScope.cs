namespace Orchestrator.Domain;

/// <summary>
/// Defines the allowed visibility scope values for agents.
/// Controls whether an agent is visible only at its owning organization,
/// at the owning org plus descendants, or only at descendant organizations.
/// </summary>
public static class AgentVisibilityScope
{
    /// <summary>Visible only at the creating organization</summary>
    public const string OrganizationOnly = "organization_only";

    /// <summary>Visible at the creating organization and all descendant organizations</summary>
    public const string OrganizationAndDescendants = "organization_and_descendants";

    /// <summary>NOT visible at the creating organization, only at descendant organizations</summary>
    public const string DescendantsOnly = "descendants_only";

    /// <summary>All allowed visibility scope values</summary>
    public static readonly string[] AllValues = { OrganizationOnly, OrganizationAndDescendants, DescendantsOnly };

    /// <summary>Returns true if the value is a valid visibility scope</summary>
    public static bool IsValid(string? value) => value != null && AllValues.Contains(value);
}
