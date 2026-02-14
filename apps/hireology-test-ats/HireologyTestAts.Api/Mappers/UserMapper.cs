using HireologyTestAts.Api.ResourceModels;
using HireologyTestAts.Domain;

namespace HireologyTestAts.Api.Mappers;

public static class UserMapper
{
    public static UserResource ToResource(User user)
    {
        return new UserResource
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

    public static IReadOnlyList<UserResource> ToResource(IEnumerable<User> users)
    {
        return users.Select(ToResource).ToList();
    }

    public static User ToDomain(UpdateUserResource resource)
    {
        return new User
        {
            Email = resource.Email,
            Name = resource.Name
        };
    }
}
