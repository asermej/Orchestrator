namespace HireologyTestAts.Domain;

internal sealed partial class DataFacade
{
    private ApplicantDataManager? _applicantDataManager;
    private ApplicantDataManager ApplicantDataManager => _applicantDataManager ??= new ApplicantDataManager(_dbConnectionString);

    public async Task<IReadOnlyList<Applicant>> GetApplicants(int pageNumber, int pageSize, IReadOnlyList<Guid>? allowedOrganizationIds = null)
    {
        return await ApplicantDataManager.ListAsync(pageNumber, pageSize, allowedOrganizationIds).ConfigureAwait(false);
    }

    public async Task<int> GetApplicantCount(IReadOnlyList<Guid>? allowedOrganizationIds = null)
    {
        return await ApplicantDataManager.CountAsync(allowedOrganizationIds).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Applicant>> GetApplicantsByJobId(Guid jobId)
    {
        return await ApplicantDataManager.ListByJobIdAsync(jobId).ConfigureAwait(false);
    }

    public async Task<Applicant?> GetApplicantById(Guid id)
    {
        return await ApplicantDataManager.GetByIdAsync(id).ConfigureAwait(false);
    }

    public async Task<Applicant> CreateApplicant(Applicant applicant)
    {
        return await ApplicantDataManager.CreateAsync(applicant).ConfigureAwait(false);
    }
}
