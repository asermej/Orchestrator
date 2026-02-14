namespace HireologyTestAts.Api.ResourceModels;

public class InviteUserResource
{
    public string Email { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public IReadOnlyList<OrganizationAccessEntryResource>? OrganizationAccess { get; set; }
}

public class OrganizationAccessEntryResource
{
    public Guid OrganizationId { get; set; }
    public bool IncludeChildren { get; set; }
}

public class GroupUserResource
{
    public UserResource User { get; set; } = null!;
    public bool IsAdmin { get; set; }
    public IReadOnlyList<OrganizationAccessEntryResource> OrganizationAccess { get; set; } = Array.Empty<OrganizationAccessEntryResource>();
}

public class GroupUserListResponse
{
    public IReadOnlyList<GroupUserResource> Items { get; set; } = Array.Empty<GroupUserResource>();
}

public class SetGroupUserAccessResource
{
    public IReadOnlyList<OrganizationAccessEntryResource> OrganizationAccess { get; set; } = Array.Empty<OrganizationAccessEntryResource>();
}
