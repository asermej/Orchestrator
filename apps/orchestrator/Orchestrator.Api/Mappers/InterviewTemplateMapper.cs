using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;

namespace Orchestrator.Api.Mappers;

public static class InterviewTemplateMapper
{
    public static InterviewTemplateResource ToResource(InterviewTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);

        return new InterviewTemplateResource
        {
            Id = template.Id,
            GroupId = template.GroupId,
            OrganizationId = template.OrganizationId,
            Name = template.Name,
            Description = template.Description,
            IsActive = template.IsActive,
            RoleTemplateId = template.RoleTemplateId,
            AgentId = template.AgentId,
            OpeningTemplate = template.OpeningTemplate,
            ClosingTemplate = template.ClosingTemplate,
            Agent = template.Agent != null ? AgentMapper.ToResource(template.Agent) : null,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt,
            CreatedBy = template.CreatedBy,
            UpdatedBy = template.UpdatedBy
        };
    }

    public static IEnumerable<InterviewTemplateResource> ToResource(IEnumerable<InterviewTemplate> templates)
    {
        ArgumentNullException.ThrowIfNull(templates);
        return templates.Select(ToResource);
    }

    public static InterviewTemplate ToDomain(CreateInterviewTemplateResource resource, Guid groupId)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return new InterviewTemplate
        {
            GroupId = groupId,
            OrganizationId = resource.OrganizationId,
            Name = resource.Name,
            Description = resource.Description,
            IsActive = resource.IsActive,
            RoleTemplateId = resource.RoleTemplateId,
            AgentId = resource.AgentId,
            OpeningTemplate = resource.OpeningTemplate,
            ClosingTemplate = resource.ClosingTemplate,
            CreatedBy = resource.CreatedBy
        };
    }

    public static InterviewTemplate ToDomain(UpdateInterviewTemplateResource resource, InterviewTemplate existing)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(existing);

        return new InterviewTemplate
        {
            Id = existing.Id,
            GroupId = existing.GroupId,
            OrganizationId = existing.OrganizationId,
            Name = resource.Name ?? existing.Name,
            Description = resource.Description ?? existing.Description,
            IsActive = resource.IsActive ?? existing.IsActive,
            RoleTemplateId = resource.RoleTemplateId ?? existing.RoleTemplateId,
            AgentId = resource.AgentId ?? existing.AgentId,
            OpeningTemplate = resource.OpeningTemplate ?? existing.OpeningTemplate,
            ClosingTemplate = resource.ClosingTemplate ?? existing.ClosingTemplate,
            CreatedAt = existing.CreatedAt,
            CreatedBy = existing.CreatedBy,
            UpdatedBy = resource.UpdatedBy
        };
    }
}
