namespace Orchestrator.Domain;

internal sealed partial class DataFacade
{
    private ApplicantDataManager ApplicantDataManager => new(_dbConnectionString);

    public Task<Applicant> AddApplicant(Applicant applicant)
    {
        return ApplicantDataManager.Add(applicant);
    }

    public async Task<Applicant?> GetApplicantById(Guid id)
    {
        return await ApplicantDataManager.GetById(id);
    }

    public async Task<Applicant?> GetApplicantByExternalId(Guid organizationId, string externalApplicantId)
    {
        return await ApplicantDataManager.GetByExternalId(organizationId, externalApplicantId);
    }

    public Task<Applicant> UpdateApplicant(Applicant applicant)
    {
        return ApplicantDataManager.Update(applicant);
    }

    public Task<bool> DeleteApplicant(Guid id)
    {
        return ApplicantDataManager.Delete(id);
    }

    public Task<PaginatedResult<Applicant>> SearchApplicants(Guid? organizationId, string? email, string? name, int pageNumber, int pageSize)
    {
        return ApplicantDataManager.Search(organizationId, email, name, pageNumber, pageSize);
    }
}
