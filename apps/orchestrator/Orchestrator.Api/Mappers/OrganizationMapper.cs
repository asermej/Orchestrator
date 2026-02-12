using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;

namespace Orchestrator.Api.Mappers;

public static class OrganizationMapper
{
    public static OrganizationResource ToResource(Organization organization)
    {
        ArgumentNullException.ThrowIfNull(organization);
        return new OrganizationResource
        {
            Id = organization.Id,
            Name = organization.Name,
            ApiKey = organization.ApiKey,
            WebhookUrl = organization.WebhookUrl,
            IsActive = organization.IsActive,
            CreatedAt = organization.CreatedAt,
            UpdatedAt = organization.UpdatedAt
        };
    }

    public static IEnumerable<OrganizationResource> ToResource(IEnumerable<Organization> organizations)
    {
        ArgumentNullException.ThrowIfNull(organizations);
        return organizations.Select(ToResource);
    }

    public static Organization ToDomain(CreateOrganizationResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        return new Organization
        {
            Name = resource.Name,
            WebhookUrl = resource.WebhookUrl
        };
    }

    public static Organization ToDomain(UpdateOrganizationResource resource, Organization existing)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(existing);
        return new Organization
        {
            Id = existing.Id,
            Name = resource.Name ?? existing.Name,
            ApiKey = existing.ApiKey,
            WebhookUrl = resource.WebhookUrl ?? existing.WebhookUrl,
            IsActive = resource.IsActive ?? existing.IsActive,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = existing.UpdatedAt,
            CreatedBy = existing.CreatedBy
        };
    }
}
