namespace HireologyTestAts.Domain;

public class Group
{
    public Guid Id { get; set; }
    public Guid? RootOrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
