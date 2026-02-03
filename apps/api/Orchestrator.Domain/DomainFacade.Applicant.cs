namespace Orchestrator.Domain;

public sealed partial class DomainFacade
{
    /// <summary>
    /// Creates a new Applicant
    /// </summary>
    public async Task<Applicant> CreateApplicant(Applicant applicant)
    {
        return await ApplicantManager.CreateApplicant(applicant).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets an Applicant by ID
    /// </summary>
    public async Task<Applicant?> GetApplicantById(Guid id)
    {
        return await ApplicantManager.GetApplicantById(id).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets an Applicant by external ID
    /// </summary>
    public async Task<Applicant?> GetApplicantByExternalId(Guid organizationId, string externalApplicantId)
    {
        return await ApplicantManager.GetApplicantByExternalId(organizationId, externalApplicantId).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets or creates an Applicant by external ID
    /// </summary>
    public async Task<Applicant> GetOrCreateApplicant(Guid organizationId, string externalApplicantId, string? firstName, string? lastName, string? email, string? phone)
    {
        return await ApplicantManager.GetOrCreateApplicant(organizationId, externalApplicantId, firstName, lastName, email, phone).ConfigureAwait(false);
    }

    /// <summary>
    /// Searches for Applicants
    /// </summary>
    public async Task<PaginatedResult<Applicant>> SearchApplicants(Guid? organizationId, string? email, string? name, int pageNumber, int pageSize)
    {
        return await ApplicantManager.SearchApplicants(organizationId, email, name, pageNumber, pageSize).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates an Applicant
    /// </summary>
    public async Task<Applicant> UpdateApplicant(Applicant applicant)
    {
        return await ApplicantManager.UpdateApplicant(applicant).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes an Applicant
    /// </summary>
    public async Task<bool> DeleteApplicant(Guid id)
    {
        return await ApplicantManager.DeleteApplicant(id).ConfigureAwait(false);
    }
}
