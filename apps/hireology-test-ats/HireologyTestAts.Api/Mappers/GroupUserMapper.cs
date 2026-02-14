using HireologyTestAts.Api.ResourceModels;
using HireologyTestAts.Domain;

namespace HireologyTestAts.Api.Mappers;

public static class GroupUserMapper
{
    public static OrganizationAccessEntry ToDomain(OrganizationAccessEntryResource resource)
    {
        return new OrganizationAccessEntry
        {
            OrganizationId = resource.OrganizationId,
            IncludeChildren = resource.IncludeChildren
        };
    }

    public static IReadOnlyList<OrganizationAccessEntry> ToDomain(IEnumerable<OrganizationAccessEntryResource> resources)
    {
        return resources.Select(ToDomain).ToList();
    }

    public static OrganizationAccessEntryResource ToResource(OrganizationAccessEntry entry)
    {
        return new OrganizationAccessEntryResource
        {
            OrganizationId = entry.OrganizationId,
            IncludeChildren = entry.IncludeChildren
        };
    }

    public static IReadOnlyList<OrganizationAccessEntryResource> ToResource(IEnumerable<OrganizationAccessEntry> entries)
    {
        return entries.Select(ToResource).ToList();
    }
}
