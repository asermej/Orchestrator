namespace HireologyTestAts.Api.ResourceModels;

public class OrganizationResource
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? State { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateOrganizationResource
{
    public Guid GroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? State { get; set; }
}

public class UpdateOrganizationResource
{
    public Guid? GroupId { get; set; }
    public string? Name { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
}
