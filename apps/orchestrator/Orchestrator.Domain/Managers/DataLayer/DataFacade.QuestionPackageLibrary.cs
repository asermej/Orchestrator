namespace Orchestrator.Domain;

internal sealed partial class DataFacade
{
    private QuestionPackageLibraryDataManager? _questionPackageLibraryDataManager;
    private QuestionPackageLibraryDataManager QuestionPackageLibraryDataManager =>
        _questionPackageLibraryDataManager ??= new QuestionPackageLibraryDataManager(_dbConnectionString);

    // --- Role Template reads ---

    public Task<List<RoleTemplate>> GetAllRoleTemplates()
    {
        return QuestionPackageLibraryDataManager.GetAllRoleTemplates();
    }

    public Task<List<RoleTemplate>> GetRoleTemplatesByFilter(string? source = null, Guid? groupId = null)
    {
        return QuestionPackageLibraryDataManager.GetRoleTemplatesByFilter(source, groupId);
    }

    public Task<RoleTemplate?> GetRoleTemplateByKey(string roleKey)
    {
        return QuestionPackageLibraryDataManager.GetRoleTemplateByKey(roleKey);
    }

    public Task<RoleTemplate?> GetRoleTemplateById(Guid id)
    {
        return QuestionPackageLibraryDataManager.GetRoleTemplateById(id);
    }

    public Task<RoleTemplate?> GetRoleTemplateWithFullDetails(string roleKey)
    {
        return QuestionPackageLibraryDataManager.GetRoleTemplateWithFullDetails(roleKey);
    }

    public Task<RoleTemplate?> GetRoleTemplateWithFullDetailsById(Guid id)
    {
        return QuestionPackageLibraryDataManager.GetRoleTemplateWithFullDetailsById(id);
    }

    // --- Org-scoped search ---

    public Task<List<RoleTemplate>> SearchLocalRoleTemplates(Guid groupId, Guid organizationId)
    {
        return QuestionPackageLibraryDataManager.SearchLocal(groupId, organizationId);
    }

    public Task<List<RoleTemplate>> SearchInheritedRoleTemplates(Guid groupId, IReadOnlyList<Guid> ancestorOrgIds)
    {
        return QuestionPackageLibraryDataManager.SearchInherited(groupId, ancestorOrgIds);
    }

    public Task<List<RoleTemplate>> SearchSystemRoleTemplates()
    {
        return QuestionPackageLibraryDataManager.SearchSystem();
    }

    public Task<RoleTemplate> CloneRoleTemplate(Guid roleTemplateId, Guid targetOrganizationId, Guid targetGroupId, string? createdBy = null)
    {
        return QuestionPackageLibraryDataManager.CloneRoleTemplate(roleTemplateId, targetOrganizationId, targetGroupId, createdBy);
    }

    // --- Role Template CRUD ---

    public Task<RoleTemplate> CreateRoleTemplate(RoleTemplate roleTemplate)
    {
        return QuestionPackageLibraryDataManager.CreateRoleTemplate(roleTemplate);
    }

    public Task<RoleTemplate> UpdateRoleTemplate(RoleTemplate roleTemplate)
    {
        return QuestionPackageLibraryDataManager.UpdateRoleTemplate(roleTemplate);
    }

    public Task<bool> DeleteRoleTemplate(Guid id, string? deletedBy = null)
    {
        return QuestionPackageLibraryDataManager.DeleteRoleTemplate(id, deletedBy);
    }

    public Task SoftDeleteChildrenOfRoleTemplate(Guid roleTemplateId, string? deletedBy = null)
    {
        return QuestionPackageLibraryDataManager.SoftDeleteChildrenOfRoleTemplate(roleTemplateId, deletedBy);
    }

    // --- Child entity reads ---

    public Task<List<Competency>> GetCompetenciesByRoleTemplateId(Guid roleTemplateId)
    {
        return QuestionPackageLibraryDataManager.GetCompetenciesByRoleTemplateId(roleTemplateId);
    }

    // --- Competency CRUD ---

    public Task<Competency> CreateCompetency(Competency competency)
    {
        return QuestionPackageLibraryDataManager.CreateCompetency(competency);
    }

    public Task<Competency> UpdateCompetency(Competency competency)
    {
        return QuestionPackageLibraryDataManager.UpdateCompetency(competency);
    }

    public Task<bool> DeleteCompetency(Guid id, string? deletedBy = null)
    {
        return QuestionPackageLibraryDataManager.DeleteCompetency(id, deletedBy);
    }

}
