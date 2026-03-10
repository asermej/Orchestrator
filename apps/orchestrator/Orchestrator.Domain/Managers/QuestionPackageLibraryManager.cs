using System.Text.RegularExpressions;

namespace Orchestrator.Domain;

internal sealed class QuestionPackageLibraryManager : IDisposable
{
    private readonly ServiceLocatorBase _serviceLocator;
    private DataFacade? _dataFacade;
    private DataFacade DataFacade => _dataFacade ??=
        new DataFacade(_serviceLocator.CreateConfigurationProvider().GetDbConnectionString());

    public QuestionPackageLibraryManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    // --- Reads ---

    public async Task<List<RoleTemplate>> GetAllRoleTemplates()
    {
        return await DataFacade.GetAllRoleTemplates().ConfigureAwait(false);
    }

    public async Task<List<RoleTemplate>> GetRoleTemplatesByFilter(string? source = null, Guid? groupId = null)
    {
        return await DataFacade.GetRoleTemplatesByFilter(source, groupId).ConfigureAwait(false);
    }

    public async Task<RoleTemplate?> GetRoleTemplateByKey(string roleKey)
    {
        return await DataFacade.GetRoleTemplateByKey(roleKey).ConfigureAwait(false);
    }

    public async Task<RoleTemplate?> GetRoleTemplateById(Guid id)
    {
        return await DataFacade.GetRoleTemplateById(id).ConfigureAwait(false);
    }

    public async Task<RoleTemplate?> GetRoleTemplateWithFullDetails(string roleKey)
    {
        return await DataFacade.GetRoleTemplateWithFullDetails(roleKey).ConfigureAwait(false);
    }

    public async Task<RoleTemplate?> GetRoleTemplateWithFullDetailsById(Guid id)
    {
        return await DataFacade.GetRoleTemplateWithFullDetailsById(id).ConfigureAwait(false);
    }

    // --- Org-scoped searches ---

    public async Task<List<RoleTemplate>> SearchLocal(Guid groupId, Guid organizationId)
    {
        return await DataFacade.SearchLocalRoleTemplates(groupId, organizationId).ConfigureAwait(false);
    }

    public async Task<List<RoleTemplate>> SearchInherited(Guid groupId, IReadOnlyList<Guid> ancestorOrgIds)
    {
        return await DataFacade.SearchInheritedRoleTemplates(groupId, ancestorOrgIds).ConfigureAwait(false);
    }

    public async Task<List<RoleTemplate>> SearchSystem()
    {
        return await DataFacade.SearchSystemRoleTemplates().ConfigureAwait(false);
    }

    public async Task<RoleTemplate> CloneRoleTemplate(Guid roleTemplateId, Guid targetOrganizationId, Guid targetGroupId, string? createdBy = null)
    {
        return await DataFacade.CloneRoleTemplate(roleTemplateId, targetOrganizationId, targetGroupId, createdBy).ConfigureAwait(false);
    }

    // --- Role Template CRUD ---

    public async Task<RoleTemplate> CreateRoleTemplate(RoleTemplate roleTemplate)
    {
        roleTemplate.Source = "custom";
        roleTemplate.RoleKey = GenerateKey(roleTemplate.RoleName);

        if (!VisibilityScope.IsValid(roleTemplate.VisibilityScope))
            roleTemplate.VisibilityScope = VisibilityScope.OrganizationOnly;

        QuestionPackageLibraryValidator.ValidateRoleTemplate(roleTemplate);

        return await DataFacade.CreateRoleTemplate(roleTemplate).ConfigureAwait(false);
    }

    public async Task<RoleTemplate> UpdateRoleTemplate(RoleTemplate roleTemplate)
    {
        var existing = await DataFacade.GetRoleTemplateById(roleTemplate.Id).ConfigureAwait(false);
        if (existing == null)
            throw new QuestionPackageLibraryNotFoundException($"Role template {roleTemplate.Id} not found.");
        EnsureCustom(existing);

        roleTemplate.Source = existing.Source;
        roleTemplate.GroupId = existing.GroupId;
        roleTemplate.OrganizationId = existing.OrganizationId;
        roleTemplate.RoleKey = existing.RoleKey;

        if (!VisibilityScope.IsValid(roleTemplate.VisibilityScope))
            roleTemplate.VisibilityScope = existing.VisibilityScope;

        QuestionPackageLibraryValidator.ValidateRoleTemplate(roleTemplate);

        return await DataFacade.UpdateRoleTemplate(roleTemplate).ConfigureAwait(false);
    }

    public async Task<bool> DeleteRoleTemplate(Guid id, string? deletedBy = null)
    {
        var existing = await DataFacade.GetRoleTemplateById(id).ConfigureAwait(false);
        if (existing == null)
            throw new QuestionPackageLibraryNotFoundException($"Role template {id} not found.");
        EnsureCustom(existing);

        await DataFacade.SoftDeleteChildrenOfRoleTemplate(id, deletedBy).ConfigureAwait(false);
        return await DataFacade.DeleteRoleTemplate(id, deletedBy).ConfigureAwait(false);
    }

    // --- Competency CRUD ---

    public async Task<Competency> CreateCompetency(Competency competency)
    {
        await EnsureParentIsCustom(competency.RoleTemplateId).ConfigureAwait(false);
        competency.CompetencyKey = GenerateKey(competency.Name);
        QuestionPackageLibraryValidator.ValidateCompetency(competency);
        return await DataFacade.CreateCompetency(competency).ConfigureAwait(false);
    }

    public async Task<Competency> UpdateCompetency(Competency competency)
    {
        await EnsureParentIsCustom(competency.RoleTemplateId).ConfigureAwait(false);
        QuestionPackageLibraryValidator.ValidateCompetency(competency);
        return await DataFacade.UpdateCompetency(competency).ConfigureAwait(false);
    }

    public async Task<bool> DeleteCompetency(Guid id, Guid roleTemplateId, string? deletedBy = null)
    {
        await EnsureParentIsCustom(roleTemplateId).ConfigureAwait(false);
        return await DataFacade.DeleteCompetency(id, deletedBy).ConfigureAwait(false);
    }

    // --- Helpers ---

    private static void EnsureCustom(RoleTemplate roleTemplate)
    {
        if (roleTemplate.Source == "system")
            throw new QuestionPackageLibraryValidationException("System role templates cannot be modified.");
    }

    private async Task EnsureParentIsCustom(Guid roleTemplateId)
    {
        var parent = await DataFacade.GetRoleTemplateById(roleTemplateId).ConfigureAwait(false);
        if (parent == null)
            throw new QuestionPackageLibraryNotFoundException($"Role template {roleTemplateId} not found.");
        EnsureCustom(parent);
    }

    private static readonly Regex NonAlphaNumeric = new(@"[^a-z0-9]+", RegexOptions.Compiled);

    private static string GenerateKey(string name)
    {
        var key = NonAlphaNumeric.Replace(name.ToLowerInvariant().Trim(), "_").Trim('_');
        return string.IsNullOrEmpty(key) ? Guid.NewGuid().ToString("N")[..8] : key;
    }

    public void Dispose()
    {
    }
}
