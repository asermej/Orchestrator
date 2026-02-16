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

    public async Task<Applicant?> GetApplicantByExternalId(Guid groupId, string externalApplicantId)
    {
        return await ApplicantDataManager.GetByExternalId(groupId, externalApplicantId);
    }

    public Task<Applicant> UpdateApplicant(Applicant applicant)
    {
        return ApplicantDataManager.Update(applicant);
    }

    public Task<bool> DeleteApplicant(Guid id)
    {
        return ApplicantDataManager.Delete(id);
    }

    public Task<PaginatedResult<Applicant>> SearchApplicants(Guid? groupId, string? email, string? name, int pageNumber, int pageSize, IReadOnlyList<Guid>? organizationIds = null)
    {
        return ApplicantDataManager.Search(groupId, email, name, pageNumber, pageSize, organizationIds);
    }
}
