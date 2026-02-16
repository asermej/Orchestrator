namespace HireologyTestAts.Domain;

internal sealed class ApplicantManager : IDisposable
{
    private readonly DataFacade _dataFacade;
    private bool _disposed;

    public ApplicantManager(ServiceLocatorBase serviceLocator)
    {
        var configProvider = serviceLocator.CreateConfigurationProvider();
        _dataFacade = new DataFacade(configProvider.GetDbConnectionString());
    }

    public async Task<IReadOnlyList<Applicant>> GetApplicants(int pageNumber, int pageSize, IReadOnlyList<Guid>? allowedOrganizationIds = null)
    {
        return await _dataFacade.GetApplicants(pageNumber, pageSize, allowedOrganizationIds).ConfigureAwait(false);
    }

    public async Task<int> GetApplicantCount(IReadOnlyList<Guid>? allowedOrganizationIds = null)
    {
        return await _dataFacade.GetApplicantCount(allowedOrganizationIds).ConfigureAwait(false);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetApplicantCountByJobIds(IReadOnlyList<Guid> jobIds)
    {
        return await _dataFacade.GetApplicantCountByJobIds(jobIds).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Applicant>> GetApplicantsByJobId(Guid jobId)
    {
        return await _dataFacade.GetApplicantsByJobId(jobId).ConfigureAwait(false);
    }

    public async Task<Applicant> GetApplicantById(Guid id)
    {
        var applicant = await _dataFacade.GetApplicantById(id).ConfigureAwait(false);
        if (applicant == null) throw new ApplicantNotFoundException();
        return applicant;
    }

    public async Task<Applicant> CreateApplicant(Applicant applicant)
    {
        if (string.IsNullOrWhiteSpace(applicant.FirstName))
            throw new ApplicantValidationException("First name is required");
        if (string.IsNullOrWhiteSpace(applicant.LastName))
            throw new ApplicantValidationException("Last name is required");
        if (string.IsNullOrWhiteSpace(applicant.Email))
            throw new ApplicantValidationException("Email is required");
        if (applicant.JobId == Guid.Empty)
            throw new ApplicantValidationException("Job is required");

        applicant.FirstName = applicant.FirstName.Trim();
        applicant.LastName = applicant.LastName.Trim();
        applicant.Email = applicant.Email.Trim();
        applicant.Phone = applicant.Phone?.Trim();

        return await _dataFacade.CreateApplicant(applicant).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}
