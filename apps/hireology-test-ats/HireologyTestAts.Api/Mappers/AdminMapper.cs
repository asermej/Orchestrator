using HireologyTestAts.Api.ResourceModels;
using HireologyTestAts.Domain;

namespace HireologyTestAts.Api.Mappers;

public static class AdminMapper
{
    public static SuperadminResource ToSuperadminResource(User user)
    {
        return new SuperadminResource
        {
            Id = user.Id,
            Auth0Sub = user.Auth0Sub,
            Email = user.Email,
            Name = user.Name,
            IsSuperadmin = user.IsSuperadmin,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    public static IReadOnlyList<SuperadminResource> ToSuperadminResource(IEnumerable<User> users)
    {
        return users.Select(ToSuperadminResource).ToList();
    }
}
