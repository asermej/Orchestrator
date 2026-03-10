using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;

namespace Orchestrator.Api.Mappers;

public static class QuestionPackageLibraryMapper
{
    // --- ToResource (domain -> API) ---

    public static List<UniversalRubricLevelResource> ToResource(IReadOnlyList<UniversalRubricLevel> levels)
    {
        ArgumentNullException.ThrowIfNull(levels);
        return levels.Select(l => new UniversalRubricLevelResource
        {
            Level = l.Level,
            Label = l.Label,
            Description = l.Description
        }).ToList();
    }

    public static RoleTemplateResource ToResource(RoleTemplate roleTemplate, bool isInherited = false, string? ownerOrgName = null)
    {
        ArgumentNullException.ThrowIfNull(roleTemplate);

        return new RoleTemplateResource
        {
            Id = roleTemplate.Id,
            RoleKey = roleTemplate.RoleKey,
            RoleName = roleTemplate.RoleName,
            Industry = roleTemplate.Industry,
            Source = roleTemplate.Source,
            GroupId = roleTemplate.GroupId,
            OrganizationId = roleTemplate.OrganizationId,
            VisibilityScope = roleTemplate.VisibilityScope,
            IsInherited = isInherited,
            OwnerOrganizationName = ownerOrgName,
            MaxFollowUpsPerQuestion = roleTemplate.MaxFollowUpsPerQuestion,
            ScoringScaleMin = roleTemplate.ScoringScaleMin,
            ScoringScaleMax = roleTemplate.ScoringScaleMax,
            FlagThreshold = roleTemplate.FlagThreshold,
            CompetencyCount = roleTemplate.Competencies.Count > 0 ? roleTemplate.Competencies.Count : roleTemplate.CompetencyCount
        };
    }

    public static IEnumerable<RoleTemplateResource> ToResource(IEnumerable<RoleTemplate> roleTemplates)
    {
        ArgumentNullException.ThrowIfNull(roleTemplates);
        return roleTemplates.Select(r => ToResource(r));
    }

    public static IEnumerable<RoleTemplateResource> ToResource(IEnumerable<RoleTemplate> roleTemplates, bool isInherited, Dictionary<Guid, string>? orgNameLookup = null)
    {
        ArgumentNullException.ThrowIfNull(roleTemplates);
        return roleTemplates.Select(r =>
        {
            string? ownerOrgName = null;
            if (orgNameLookup != null && r.OrganizationId.HasValue)
                orgNameLookup.TryGetValue(r.OrganizationId.Value, out ownerOrgName);
            return ToResource(r, isInherited, ownerOrgName);
        });
    }

    public static RoleTemplateDetailResource ToDetailResource(RoleTemplate roleTemplate, bool isInherited = false, string? ownerOrgName = null)
    {
        ArgumentNullException.ThrowIfNull(roleTemplate);

        return new RoleTemplateDetailResource
        {
            Id = roleTemplate.Id,
            RoleKey = roleTemplate.RoleKey,
            RoleName = roleTemplate.RoleName,
            Industry = roleTemplate.Industry,
            Source = roleTemplate.Source,
            GroupId = roleTemplate.GroupId,
            OrganizationId = roleTemplate.OrganizationId,
            VisibilityScope = roleTemplate.VisibilityScope,
            IsInherited = isInherited,
            OwnerOrganizationName = ownerOrgName,
            MaxFollowUpsPerQuestion = roleTemplate.MaxFollowUpsPerQuestion,
            ScoringScaleMin = roleTemplate.ScoringScaleMin,
            ScoringScaleMax = roleTemplate.ScoringScaleMax,
            FlagThreshold = roleTemplate.FlagThreshold,
            Competencies = roleTemplate.Competencies.Select(ToResource).ToList()
        };
    }

    // --- FromRequest (API -> domain) ---

    public static RoleTemplate ToDomain(CreateRoleTemplateRequest request, Guid groupId)
    {
        return new RoleTemplate
        {
            RoleName = request.RoleName,
            Industry = request.Industry,
            GroupId = groupId,
            OrganizationId = request.OrganizationId,
            VisibilityScope = request.VisibilityScope ?? Domain.VisibilityScope.OrganizationOnly
        };
    }

    public static Competency ToDomain(CreateCompetencyRequest request, Guid roleTemplateId)
    {
        return new Competency
        {
            RoleTemplateId = roleTemplateId,
            Name = request.Name,
            Description = request.Description,
            CanonicalExample = request.CanonicalExample,
            DefaultWeight = request.DefaultWeight,
            IsRequired = request.IsRequired,
            DisplayOrder = request.DisplayOrder
        };
    }

    public static void ApplyUpdate(UpdateRoleTemplateRequest request, RoleTemplate existing)
    {
        existing.RoleName = request.RoleName;
        existing.Industry = request.Industry;
        if (!string.IsNullOrEmpty(request.VisibilityScope))
            existing.VisibilityScope = request.VisibilityScope;
    }

    public static void ApplyUpdate(UpdateCompetencyRequest request, Competency existing)
    {
        existing.Name = request.Name;
        existing.Description = request.Description;
        existing.CanonicalExample = request.CanonicalExample;
        existing.DefaultWeight = request.DefaultWeight;
        existing.IsRequired = request.IsRequired;
        existing.DisplayOrder = request.DisplayOrder;
    }

    // --- Competency and AI suggestion mappings ---

    public static CompetencyResource ToResource(Competency competency)
    {
        ArgumentNullException.ThrowIfNull(competency);

        return new CompetencyResource
        {
            Id = competency.Id,
            CompetencyKey = competency.CompetencyKey,
            Name = competency.Name,
            Description = competency.Description,
            CanonicalExample = competency.CanonicalExample,
            DefaultWeight = competency.DefaultWeight,
            IsRequired = competency.IsRequired,
            DisplayOrder = competency.DisplayOrder
        };
    }

    public static List<AISuggestedCompetencyResource> ToResource(List<AISuggestedCompetency> suggestions)
    {
        return suggestions.Select(s => new AISuggestedCompetencyResource
        {
            Name = s.Name,
            DefaultWeight = s.DefaultWeight,
            Description = s.Description
        }).ToList();
    }

}
