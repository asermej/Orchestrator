namespace HireologyTestAts.Domain;

public class Organization
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Guid? ParentOrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? State { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
