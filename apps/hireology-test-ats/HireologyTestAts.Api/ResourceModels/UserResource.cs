namespace HireologyTestAts.Api.ResourceModels;

public class UserResource
{
    public Guid Id { get; set; }
    public string Auth0Sub { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpdateUserResource
{
    public string? Email { get; set; }
    public string? Name { get; set; }
}

public class SetUserAccessResource
{
    public IReadOnlyList<Guid>? GroupIds { get; set; }
    public IReadOnlyList<Guid>? OrganizationIds { get; set; }
}

public class UserWithAccessResponse
{
    public UserResource User { get; set; } = null!;
    public IReadOnlyList<Guid> GroupIds { get; set; } = Array.Empty<Guid>();
    public IReadOnlyList<Guid> OrganizationIds { get; set; } = Array.Empty<Guid>();
}

public class UserListResponse
{
    public IReadOnlyList<UserResource> Items { get; set; } = Array.Empty<UserResource>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}
