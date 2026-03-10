using System.ComponentModel.DataAnnotations;
using Orchestrator.Api.Common;

namespace Orchestrator.Api.ResourcesModels;

public class InterviewTemplateResource
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Guid? OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public Guid? RoleTemplateId { get; set; }
    public Guid? AgentId { get; set; }
    public string? OpeningTemplate { get; set; }
    public string? ClosingTemplate { get; set; }
    public AgentResource? Agent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}

public class CreateInterviewTemplateResource
{
    [Required(ErrorMessage = "GroupId is required")]
    public Guid GroupId { get; set; }

    public Guid? OrganizationId { get; set; }

    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? RoleTemplateId { get; set; }
    public Guid? AgentId { get; set; }
    public string? OpeningTemplate { get; set; }
    public string? ClosingTemplate { get; set; }
    public string? CreatedBy { get; set; }
}

public class UpdateInterviewTemplateResource
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
    public Guid? RoleTemplateId { get; set; }
    public Guid? AgentId { get; set; }
    public string? OpeningTemplate { get; set; }
    public string? ClosingTemplate { get; set; }
    public string? UpdatedBy { get; set; }
}

public class SearchInterviewTemplateRequest : PaginatedRequest
{
    public Guid? GroupId { get; set; }
    public Guid? AgentId { get; set; }
    public string? Name { get; set; }
    public bool? IsActive { get; set; }
    public new string? SortBy { get; set; }
}

/// <summary>
/// Lightweight interview template resource for ATS integration endpoints
/// </summary>
public class AtsInterviewTemplateResource
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? AgentId { get; set; }
    public string? AgentDisplayName { get; set; }
}
