namespace Orchestrator.Domain;

/// <summary>
/// Manages business operations for Applicant entities
/// </summary>
internal sealed class ApplicantManager : IDisposable
{
    private readonly ServiceLocatorBase _serviceLocator;
    private DataFacade? _dataFacade;
    private DataFacade DataFacade => _dataFacade ??= new DataFacade(_serviceLocator.CreateConfigurationProvider().GetDbConnectionString());

    public ApplicantManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    public async Task<Applicant> CreateApplicant(Applicant applicant)
    {
        ApplicantValidator.Validate(applicant);
        return await DataFacade.AddApplicant(applicant).ConfigureAwait(false);
    }

    public async Task<Applicant?> GetApplicantById(Guid id)
    {
        return await DataFacade.GetApplicantById(id).ConfigureAwait(false);
    }

    public async Task<Applicant?> GetApplicantByExternalId(Guid organizationId, string externalApplicantId)
    {
        return await DataFacade.GetApplicantByExternalId(organizationId, externalApplicantId).ConfigureAwait(false);
    }

    public async Task<Applicant> GetOrCreateApplicant(Guid organizationId, string externalApplicantId, string? firstName, string? lastName, string? email, string? phone)
    {
        var existing = await DataFacade.GetApplicantByExternalId(organizationId, externalApplicantId).ConfigureAwait(false);
        if (existing != null)
        {
            return existing;
        }

        var applicant = new Applicant
        {
            OrganizationId = organizationId,
            ExternalApplicantId = externalApplicantId,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = phone
        };

        ApplicantValidator.Validate(applicant);
        return await DataFacade.AddApplicant(applicant).ConfigureAwait(false);
    }

    public async Task<PaginatedResult<Applicant>> SearchApplicants(Guid? organizationId, string? email, string? name, int pageNumber, int pageSize)
    {
        return await DataFacade.SearchApplicants(organizationId, email, name, pageNumber, pageSize).ConfigureAwait(false);
    }

    public async Task<Applicant> UpdateApplicant(Applicant applicant)
    {
        ApplicantValidator.Validate(applicant);
        return await DataFacade.UpdateApplicant(applicant).ConfigureAwait(false);
    }

    public async Task<bool> DeleteApplicant(Guid id)
    {
        return await DataFacade.DeleteApplicant(id).ConfigureAwait(false);
    }

    public void Dispose()
    {
        // DataFacade doesn't implement IDisposable, so no disposal needed
    }
}
