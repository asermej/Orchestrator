namespace Orchestrator.Api.ResourcesModels;

/// <summary>
/// Summary resource for role template list views (no children loaded)
/// </summary>
public class RoleTemplateResource
{
    public Guid Id { get; set; }
    public string RoleKey { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string Source { get; set; } = "system";
    public Guid? GroupId { get; set; }
    public Guid? OrganizationId { get; set; }
    public string VisibilityScope { get; set; } = "organization_only";
    public bool IsInherited { get; set; }
    public string? OwnerOrganizationName { get; set; }
    public int MaxFollowUpsPerQuestion { get; set; }
    public int ScoringScaleMin { get; set; }
    public int ScoringScaleMax { get; set; }
    public int FlagThreshold { get; set; }
    public int CompetencyCount { get; set; }
}

/// <summary>
/// Full role template with nested competencies
/// </summary>
public class RoleTemplateDetailResource
{
    public Guid Id { get; set; }
    public string RoleKey { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string Source { get; set; } = "system";
    public Guid? GroupId { get; set; }
    public Guid? OrganizationId { get; set; }
    public string VisibilityScope { get; set; } = "organization_only";
    public bool IsInherited { get; set; }
    public string? OwnerOrganizationName { get; set; }
    public int MaxFollowUpsPerQuestion { get; set; }
    public int ScoringScaleMin { get; set; }
    public int ScoringScaleMax { get; set; }
    public int FlagThreshold { get; set; }
    public List<CompetencyResource> Competencies { get; set; } = new();
}

// --- Create/Update Request Models ---

public class CreateRoleTemplateRequest
{
    public string RoleName { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public Guid? OrganizationId { get; set; }
    public string? VisibilityScope { get; set; }
}

public class UpdateRoleTemplateRequest
{
    public string RoleName { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string? VisibilityScope { get; set; }
}

public class CreateCompetencyRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CanonicalExample { get; set; } = string.Empty;
    public int DefaultWeight { get; set; }
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
}

public class UpdateCompetencyRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CanonicalExample { get; set; } = string.Empty;
    public int DefaultWeight { get; set; }
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// Read-only level of the universal 1-5 behavioral rubric (not stored per competency).
/// </summary>
public class UniversalRubricLevelResource
{
    public int Level { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

// --- AI Generation Request/Response Models ---

public class AISuggestCompetenciesRequest
{
    public string RoleName { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
}

public class AISuggestedCompetencyResource
{
    public string Name { get; set; } = string.Empty;
    public int DefaultWeight { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class AISuggestCanonicalExampleRequest
{
    public string CompetencyName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string RoleContext { get; set; } = string.Empty;
}

public class AISuggestCanonicalExampleResponse
{
    public string SuggestedExample { get; set; } = string.Empty;
}

public class CompetencyResource
{
    public Guid Id { get; set; }
    public string CompetencyKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CanonicalExample { get; set; }
    public int DefaultWeight { get; set; }
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
}
