using HireologyTestAts.Api.ResourceModels;
using HireologyTestAts.Domain;

namespace HireologyTestAts.Api.Mappers;

public static class ApplicantMapper
{
    public static ApplicantResource ToResource(Applicant applicant)
    {
        return new ApplicantResource
        {
            Id = applicant.Id,
            JobId = applicant.JobId,
            OrganizationId = applicant.OrganizationId,
            FirstName = applicant.FirstName,
            LastName = applicant.LastName,
            Email = applicant.Email,
            Phone = applicant.Phone,
            CreatedAt = applicant.CreatedAt,
            UpdatedAt = applicant.UpdatedAt
        };
    }

    public static IReadOnlyList<ApplicantResource> ToResource(IEnumerable<Applicant> applicants)
    {
        return applicants.Select(ToResource).ToList();
    }

    public static Applicant ToDomain(CreateApplicantResource resource, Guid jobId, Guid? organizationId)
    {
        return new Applicant
        {
            JobId = jobId,
            OrganizationId = organizationId,
            FirstName = resource.FirstName,
            LastName = resource.LastName,
            Email = resource.Email,
            Phone = resource.Phone
        };
    }
}
