namespace Orchestrator.Domain;

public sealed partial class DomainFacade
{
    private QuestionPackageLibraryManager? _questionPackageLibraryManager;
    private QuestionPackageLibraryManager QuestionPackageLibraryManager =>
        _questionPackageLibraryManager ??= new QuestionPackageLibraryManager(_serviceLocator);

    // --- Reads ---

    public async Task<List<RoleTemplate>> GetAllRoleTemplatesAsync()
    {
        return await QuestionPackageLibraryManager.GetAllRoleTemplates().ConfigureAwait(false);
    }

    public async Task<List<RoleTemplate>> GetRoleTemplatesByFilterAsync(string? source = null, Guid? groupId = null)
    {
        return await QuestionPackageLibraryManager.GetRoleTemplatesByFilter(source, groupId).ConfigureAwait(false);
    }

    public async Task<RoleTemplate?> GetRoleTemplateByKeyAsync(string roleKey)
    {
        return await QuestionPackageLibraryManager.GetRoleTemplateByKey(roleKey).ConfigureAwait(false);
    }

    public async Task<RoleTemplate?> GetRoleTemplateByIdAsync(Guid id)
    {
        return await QuestionPackageLibraryManager.GetRoleTemplateById(id).ConfigureAwait(false);
    }

    public async Task<RoleTemplate?> GetRoleTemplateWithFullDetailsAsync(string roleKey)
    {
        return await QuestionPackageLibraryManager.GetRoleTemplateWithFullDetails(roleKey).ConfigureAwait(false);
    }

    public async Task<RoleTemplate?> GetRoleTemplateWithFullDetailsByIdAsync(Guid id)
    {
        return await QuestionPackageLibraryManager.GetRoleTemplateWithFullDetailsById(id).ConfigureAwait(false);
    }

    // --- Org-scoped searches ---

    public async Task<List<RoleTemplate>> SearchLocalRoleTemplatesAsync(Guid groupId, Guid organizationId)
    {
        return await QuestionPackageLibraryManager.SearchLocal(groupId, organizationId).ConfigureAwait(false);
    }

    public async Task<List<RoleTemplate>> SearchInheritedRoleTemplatesAsync(Guid groupId, IReadOnlyList<Guid> ancestorOrgIds)
    {
        return await QuestionPackageLibraryManager.SearchInherited(groupId, ancestorOrgIds).ConfigureAwait(false);
    }

    public async Task<List<RoleTemplate>> SearchSystemRoleTemplatesAsync()
    {
        return await QuestionPackageLibraryManager.SearchSystem().ConfigureAwait(false);
    }

    public async Task<RoleTemplate> CloneRoleTemplateAsync(Guid roleTemplateId, Guid targetOrganizationId, Guid targetGroupId, string? createdBy = null)
    {
        return await QuestionPackageLibraryManager.CloneRoleTemplate(roleTemplateId, targetOrganizationId, targetGroupId, createdBy).ConfigureAwait(false);
    }

    // --- Role Template CRUD ---

    public async Task<RoleTemplate> CreateRoleTemplateAsync(RoleTemplate roleTemplate)
    {
        return await QuestionPackageLibraryManager.CreateRoleTemplate(roleTemplate).ConfigureAwait(false);
    }

    public async Task<RoleTemplate> UpdateRoleTemplateAsync(RoleTemplate roleTemplate)
    {
        return await QuestionPackageLibraryManager.UpdateRoleTemplate(roleTemplate).ConfigureAwait(false);
    }

    public async Task<bool> DeleteRoleTemplateAsync(Guid id, string? deletedBy = null)
    {
        return await QuestionPackageLibraryManager.DeleteRoleTemplate(id, deletedBy).ConfigureAwait(false);
    }

    // --- Competency CRUD ---

    public async Task<Competency> CreateCompetencyAsync(Competency competency)
    {
        return await QuestionPackageLibraryManager.CreateCompetency(competency).ConfigureAwait(false);
    }

    public async Task<Competency> UpdateCompetencyAsync(Competency competency)
    {
        return await QuestionPackageLibraryManager.UpdateCompetency(competency).ConfigureAwait(false);
    }

    public async Task<bool> DeleteCompetencyAsync(Guid id, Guid roleTemplateId, string? deletedBy = null)
    {
        return await QuestionPackageLibraryManager.DeleteCompetency(id, roleTemplateId, deletedBy).ConfigureAwait(false);
    }

}
