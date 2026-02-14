namespace HireologyTestAts.Api.ResourceModels;

public class MeResponse
{
    public UserResource User { get; set; } = null!;
    public bool IsSuperadmin { get; set; }
    public bool IsGroupAdmin { get; set; }
    public IReadOnlyList<Guid> AdminGroupIds { get; set; } = Array.Empty<Guid>();
    public IReadOnlyList<GroupResource> AccessibleGroups { get; set; } = Array.Empty<GroupResource>();
    public IReadOnlyList<OrganizationResource> AccessibleOrganizations { get; set; } = Array.Empty<OrganizationResource>();
    public MeContextResponse CurrentContext { get; set; } = null!;
}

public class MeContextResponse
{
    public Guid? SelectedOrganizationId { get; set; }
}

public class SetContextResource
{
    public Guid? SelectedOrganizationId { get; set; }
}
