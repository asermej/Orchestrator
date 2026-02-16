namespace Orchestrator.Domain;

/// <summary>
/// Domain model representing a user's access within the ATS.
/// Returned by the ATS gateway to the domain layer.
/// </summary>
public class AtsUserAccess
{
    public Guid UserId { get; set; }
    public string Auth0Sub { get; set; } = string.Empty;
    public bool IsSuperadmin { get; set; }
    public bool IsGroupAdmin { get; set; }
    public IReadOnlyList<Guid> AdminGroupIds { get; set; } = Array.Empty<Guid>();
    public IReadOnlyList<AtsGroupAccess> AccessibleGroups { get; set; } = Array.Empty<AtsGroupAccess>();
    public IReadOnlyList<AtsOrganizationAccess> AccessibleOrganizations { get; set; } = Array.Empty<AtsOrganizationAccess>();
}

public class AtsGroupAccess
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class AtsOrganizationAccess
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string Name { get; set; } = string.Empty;
}
