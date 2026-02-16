namespace HireologyTestAts.Domain;

public sealed partial class DomainFacade
{
    public async Task<IReadOnlyList<Applicant>> GetApplicants(int pageNumber, int pageSize, IReadOnlyList<Guid>? allowedOrganizationIds = null)
    {
        return await ApplicantManager.GetApplicants(pageNumber, pageSize, allowedOrganizationIds).ConfigureAwait(false);
    }

    public async Task<int> GetApplicantCount(IReadOnlyList<Guid>? allowedOrganizationIds = null)
    {
        return await ApplicantManager.GetApplicantCount(allowedOrganizationIds).ConfigureAwait(false);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetApplicantCountByJobIds(IReadOnlyList<Guid> jobIds)
    {
        return await ApplicantManager.GetApplicantCountByJobIds(jobIds).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Applicant>> GetApplicantsByJobId(Guid jobId)
    {
        return await ApplicantManager.GetApplicantsByJobId(jobId).ConfigureAwait(false);
    }

    public async Task<Applicant> GetApplicantById(Guid id)
    {
        return await ApplicantManager.GetApplicantById(id).ConfigureAwait(false);
    }

    public async Task<Applicant> CreateApplicant(Applicant applicant)
    {
        return await ApplicantManager.CreateApplicant(applicant).ConfigureAwait(false);
    }
}
