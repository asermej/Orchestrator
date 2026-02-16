using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;

namespace Orchestrator.Api.Mappers;

public static class ApplicantMapper
{
    public static ApplicantResource ToResource(Applicant applicant)
    {
        ArgumentNullException.ThrowIfNull(applicant);
        return new ApplicantResource
        {
            Id = applicant.Id,
            GroupId = applicant.GroupId,
            OrganizationId = applicant.OrganizationId,
            ExternalApplicantId = applicant.ExternalApplicantId,
            FirstName = applicant.FirstName,
            LastName = applicant.LastName,
            Email = applicant.Email,
            Phone = applicant.Phone,
            CreatedAt = applicant.CreatedAt,
            UpdatedAt = applicant.UpdatedAt
        };
    }

    public static IEnumerable<ApplicantResource> ToResource(IEnumerable<Applicant> applicants)
    {
        ArgumentNullException.ThrowIfNull(applicants);
        return applicants.Select(ToResource);
    }

    public static Applicant ToDomain(CreateApplicantResource resource, Guid groupId)
    {
        ArgumentNullException.ThrowIfNull(resource);
        return new Applicant
        {
            GroupId = groupId,
            OrganizationId = resource.OrganizationId,
            ExternalApplicantId = resource.ExternalApplicantId,
            FirstName = resource.FirstName,
            LastName = resource.LastName,
            Email = resource.Email,
            Phone = resource.Phone
        };
    }

    public static Applicant ToDomain(UpdateApplicantResource resource, Applicant existing)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(existing);
        return new Applicant
        {
            Id = existing.Id,
            GroupId = existing.GroupId,
            OrganizationId = existing.OrganizationId,
            ExternalApplicantId = existing.ExternalApplicantId,
            FirstName = resource.FirstName ?? existing.FirstName,
            LastName = resource.LastName ?? existing.LastName,
            Email = resource.Email ?? existing.Email,
            Phone = resource.Phone ?? existing.Phone,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = existing.UpdatedAt,
            CreatedBy = existing.CreatedBy
        };
    }
}
