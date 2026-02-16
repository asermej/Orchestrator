using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;

namespace Orchestrator.Api.Mappers;

public static class JobMapper
{
    public static JobResource ToResource(Job job)
    {
        ArgumentNullException.ThrowIfNull(job);
        return new JobResource
        {
            Id = job.Id,
            GroupId = job.GroupId,
            OrganizationId = job.OrganizationId,
            ExternalJobId = job.ExternalJobId,
            Title = job.Title,
            Description = job.Description,
            Status = job.Status,
            Location = job.Location,
            CreatedAt = job.CreatedAt,
            UpdatedAt = job.UpdatedAt
        };
    }

    public static IEnumerable<JobResource> ToResource(IEnumerable<Job> jobs)
    {
        ArgumentNullException.ThrowIfNull(jobs);
        return jobs.Select(ToResource);
    }

    public static Job ToDomain(CreateJobResource resource, Guid groupId)
    {
        ArgumentNullException.ThrowIfNull(resource);
        return new Job
        {
            GroupId = groupId,
            OrganizationId = resource.OrganizationId,
            ExternalJobId = resource.ExternalJobId,
            Title = resource.Title,
            Description = resource.Description,
            Location = resource.Location
        };
    }

    public static Job ToDomain(UpdateJobResource resource, Job existing)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(existing);
        return new Job
        {
            Id = existing.Id,
            GroupId = existing.GroupId,
            OrganizationId = existing.OrganizationId,
            ExternalJobId = existing.ExternalJobId,
            Title = resource.Title ?? existing.Title,
            Description = resource.Description ?? existing.Description,
            Status = resource.Status ?? existing.Status,
            Location = resource.Location ?? existing.Location,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = existing.UpdatedAt,
            CreatedBy = existing.CreatedBy
        };
    }
}
