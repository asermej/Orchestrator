namespace HireologyTestAts.Api.ResourceModels;

public class JobResource
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

public class CreateJobResource
{
    public string ExternalJobId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? Status { get; set; }
    public Guid? OrganizationId { get; set; }
}

public class UpdateJobResource
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? Status { get; set; }
    public Guid? OrganizationId { get; set; }
}

public class JobListResponse
{
    public IReadOnlyList<JobResource> Items { get; set; } = Array.Empty<JobResource>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}
