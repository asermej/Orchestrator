namespace HireologyTestAts.Api.ResourceModels;

public class SuperadminResource
{
    public Guid Id { get; set; }
    public string Auth0Sub { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Name { get; set; }
    public bool IsSuperadmin { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class PromoteSuperadminResource
{
    public Guid UserId { get; set; }
}
