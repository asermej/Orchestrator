using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;

namespace Orchestrator.Api.Mappers;

/// <summary>
/// Mapper class for converting between User domain objects and UserResource API models.
/// </summary>
public static class UserMapper
{
    /// <summary>
    /// Maps a User domain object to a UserResource for API responses.
    /// </summary>
    /// <param name="user">The domain User object to map</param>
    /// <returns>A UserResource object suitable for API responses</returns>
    /// <exception cref="ArgumentNullException">Thrown when user is null</exception>
    public static UserResource ToResource(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new UserResource
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Phone = user.Phone,
            Auth0Sub = user.Auth0Sub,
            ProfileImageUrl = user.ProfileImageUrl,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    /// <summary>
    /// Maps a collection of User domain objects to UserResource objects.
    /// </summary>
    /// <param name="users">The collection of domain User objects to map</param>
    /// <returns>A collection of UserResource objects suitable for API responses</returns>
    /// <exception cref="ArgumentNullException">Thrown when users is null</exception>
    public static IEnumerable<UserResource> ToResource(IEnumerable<User> users)
    {
        ArgumentNullException.ThrowIfNull(users);

        return users.Select(ToResource);
    }

    /// <summary>
    /// Maps a CreateUserResource to a User domain object for creation.
    /// </summary>
    /// <param name="createResource">The CreateUserResource from API request</param>
    /// <returns>A User domain object ready for creation</returns>
    /// <exception cref="ArgumentNullException">Thrown when createResource is null</exception>
    public static User ToDomain(CreateUserResource createResource)
    {
        ArgumentNullException.ThrowIfNull(createResource);

        return new User
        {
            FirstName = createResource.FirstName,
            LastName = createResource.LastName,
            Email = createResource.Email,
            Phone = createResource.Phone,
            Auth0Sub = createResource.Auth0Sub
        };
    }

    /// <summary>
    /// Maps an UpdateUserResource to a User domain object for updates.
    /// </summary>
    /// <param name="updateResource">The UpdateUserResource from API request</param>
    /// <param name="existingUser">The existing User domain object to update</param>
    /// <returns>A User domain object with updated values</returns>
    /// <exception cref="ArgumentNullException">Thrown when updateResource or existingUser is null</exception>
    public static User ToDomain(UpdateUserResource updateResource, User existingUser)
    {
        ArgumentNullException.ThrowIfNull(updateResource);
        ArgumentNullException.ThrowIfNull(existingUser);

        return new User
        {
            Id = existingUser.Id,
            FirstName = updateResource.FirstName ?? existingUser.FirstName,
            LastName = updateResource.LastName ?? existingUser.LastName,
            Email = updateResource.Email ?? existingUser.Email,
            Phone = updateResource.Phone ?? existingUser.Phone,
            Auth0Sub = updateResource.Auth0Sub ?? existingUser.Auth0Sub,
            CreatedAt = existingUser.CreatedAt,
            UpdatedAt = existingUser.UpdatedAt,
            CreatedBy = existingUser.CreatedBy,
            UpdatedBy = existingUser.UpdatedBy,
            IsDeleted = existingUser.IsDeleted,
            DeletedAt = existingUser.DeletedAt,
            DeletedBy = existingUser.DeletedBy
        };
    }
} 