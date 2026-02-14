using HireologyTestAts.Api.ResourceModels;
using HireologyTestAts.Domain;

namespace HireologyTestAts.Api.Mappers;

public static class OrganizationMapper
{
    public static OrganizationResource ToResource(Organization org)
    {
        return new OrganizationResource
        {
            Id = org.Id,
            GroupId = org.GroupId,
            ParentOrganizationId = org.ParentOrganizationId,
            Name = org.Name,
            City = org.City,
            State = org.State,
            CreatedAt = org.CreatedAt,
            UpdatedAt = org.UpdatedAt
        };
    }

    public static IReadOnlyList<OrganizationResource> ToResource(IEnumerable<Organization> orgs)
    {
        return orgs.Select(ToResource).ToList();
    }

    public static Organization ToDomain(CreateOrganizationResource resource)
    {
        return new Organization
        {
            GroupId = resource.GroupId,
            ParentOrganizationId = resource.ParentOrganizationId,
            Name = resource.Name,
            City = resource.City,
            State = resource.State
        };
    }

    public static Organization ToDomain(UpdateOrganizationResource resource)
    {
        return new Organization
        {
            GroupId = resource.GroupId ?? Guid.Empty,
            ParentOrganizationId = resource.ParentOrganizationId,
            Name = resource.Name ?? string.Empty,
            City = resource.City,
            State = resource.State
        };
    }
}
