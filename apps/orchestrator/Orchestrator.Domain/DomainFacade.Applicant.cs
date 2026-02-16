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
    public async Task<Applicant?> GetApplicantByExternalId(Guid groupId, string externalApplicantId)
    {
        return await ApplicantManager.GetApplicantByExternalId(groupId, externalApplicantId).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets or creates an Applicant by external ID
    /// </summary>
    public async Task<Applicant> GetOrCreateApplicant(Guid groupId, string externalApplicantId, string? firstName, string? lastName, string? email, string? phone)
    {
        return await ApplicantManager.GetOrCreateApplicant(groupId, externalApplicantId, firstName, lastName, email, phone).ConfigureAwait(false);
    }

    /// <summary>
    /// Searches for Applicants
    /// </summary>
    public async Task<PaginatedResult<Applicant>> SearchApplicants(Guid? groupId, string? email, string? name, int pageNumber, int pageSize, IReadOnlyList<Guid>? organizationIds = null)
    {
        return await ApplicantManager.SearchApplicants(groupId, email, name, pageNumber, pageSize, organizationIds).ConfigureAwait(false);
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
