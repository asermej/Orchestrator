using HireologyTestAts.Api.ResourceModels;
using HireologyTestAts.Domain;

namespace HireologyTestAts.Api.Mappers;

public static class JobMapper
{
    public static JobResource ToResource(Job job)
    {
        return new JobResource
        {
            Id = job.Id,
            ExternalJobId = job.ExternalJobId,
            Title = job.Title,
            Description = job.Description,
            Location = job.Location,
            Status = job.Status,
            OrganizationId = job.OrganizationId,
            CreatedAt = job.CreatedAt,
            UpdatedAt = job.UpdatedAt
        };
    }

    public static IReadOnlyList<JobResource> ToResource(IEnumerable<Job> jobs)
    {
        return jobs.Select(ToResource).ToList();
    }

    public static Job ToDomain(CreateJobResource resource)
    {
        return new Job
        {
            ExternalJobId = resource.ExternalJobId,
            Title = resource.Title,
            Description = resource.Description,
            Location = resource.Location,
            Status = resource.Status ?? "active",
            OrganizationId = resource.OrganizationId
        };
    }

    public static Job ToDomain(UpdateJobResource resource)
    {
        return new Job
        {
            Title = resource.Title ?? string.Empty,
            Description = resource.Description,
            Location = resource.Location,
            Status = resource.Status ?? "active",
            OrganizationId = resource.OrganizationId
        };
    }
}
