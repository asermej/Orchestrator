using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;

namespace Orchestrator.Api.Mappers;

public static class GroupMapper
{
    public static GroupResource ToResource(Group group)
    {
        ArgumentNullException.ThrowIfNull(group);
        return new GroupResource
        {
            Id = group.Id,
            Name = group.Name,
            ApiKey = group.ApiKey,
            WebhookUrl = group.WebhookUrl,
            IsActive = group.IsActive,
            ExternalGroupId = group.ExternalGroupId,
            AtsBaseUrl = group.AtsBaseUrl,
            CreatedAt = group.CreatedAt,
            UpdatedAt = group.UpdatedAt
        };
    }

    public static IEnumerable<GroupResource> ToResource(IEnumerable<Group> groups)
    {
        ArgumentNullException.ThrowIfNull(groups);
        return groups.Select(ToResource);
    }

    public static Group ToDomain(CreateGroupResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        return new Group
        {
            Name = resource.Name,
            WebhookUrl = resource.WebhookUrl,
            ExternalGroupId = resource.ExternalGroupId,
            AtsBaseUrl = resource.AtsBaseUrl
        };
    }

    public static Group ToDomain(UpdateGroupResource resource, Group existing)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(existing);
        return new Group
        {
            Id = existing.Id,
            Name = resource.Name ?? existing.Name,
            ApiKey = existing.ApiKey,
            WebhookUrl = resource.WebhookUrl ?? existing.WebhookUrl,
            IsActive = resource.IsActive ?? existing.IsActive,
            ExternalGroupId = resource.ExternalGroupId ?? existing.ExternalGroupId,
            AtsBaseUrl = resource.AtsBaseUrl ?? existing.AtsBaseUrl,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = existing.UpdatedAt,
            CreatedBy = existing.CreatedBy
        };
    }
}
