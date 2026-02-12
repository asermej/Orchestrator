using HireologyTestAts.Api.ResourceModels;
using HireologyTestAts.Domain;

namespace HireologyTestAts.Api.Mappers;

public static class GroupMapper
{
    public static GroupResource ToResource(Group group)
    {
        return new GroupResource
        {
            Id = group.Id,
            Name = group.Name,
            CreatedAt = group.CreatedAt,
            UpdatedAt = group.UpdatedAt
        };
    }

    public static IReadOnlyList<GroupResource> ToResource(IEnumerable<Group> groups)
    {
        return groups.Select(ToResource).ToList();
    }

    public static Group ToDomain(CreateGroupResource resource)
    {
        return new Group
        {
            Name = resource.Name
        };
    }

    public static Group ToDomain(UpdateGroupResource resource)
    {
        return new Group
        {
            Name = resource.Name ?? string.Empty
        };
    }
}
