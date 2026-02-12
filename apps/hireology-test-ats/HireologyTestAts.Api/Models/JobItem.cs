namespace HireologyTestAts.Api.Models;

public class JobItem
{
    public Guid Id { get; set; }
    public string ExternalJobId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string Status { get; set; } = "active";
    public Guid? OrganizationId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
