using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;

namespace Orchestrator.Api.Mappers;

/// <summary>
/// Mapper class for converting between Chat domain objects and ChatResource API models.
/// </summary>
public static class ChatMapper
{
    /// <summary>
    /// Maps a Chat domain object to a ChatResource for API responses.
    /// </summary>
    /// <param name="chat">The domain Chat object to map</param>
    /// <returns>A ChatResource object suitable for API responses</returns>
    /// <exception cref="ArgumentNullException">Thrown when chat is null</exception>
    public static ChatResource ToResource(Chat chat)
    {
        ArgumentNullException.ThrowIfNull(chat);

        return new ChatResource
        {
            Id = chat.Id,
            PersonaId = chat.PersonaId,
            UserId = chat.UserId,
            Title = chat.Title,
            LastMessageAt = chat.LastMessageAt,
            CreatedAt = chat.CreatedAt,
            UpdatedAt = chat.UpdatedAt
        };
    }

    /// <summary>
    /// Maps a collection of Chat domain objects to ChatResource objects.
    /// </summary>
    /// <param name="chats">The collection of domain Chat objects to map</param>
    /// <returns>A collection of ChatResource objects suitable for API responses</returns>
    /// <exception cref="ArgumentNullException">Thrown when chats is null</exception>
    public static IEnumerable<ChatResource> ToResource(IEnumerable<Chat> chats)
    {
        ArgumentNullException.ThrowIfNull(chats);

        return chats.Select(ToResource);
    }

    /// <summary>
    /// Maps a CreateChatResource to a Chat domain object for creation.
    /// </summary>
    /// <param name="createResource">The CreateChatResource from API request</param>
    /// <returns>A Chat domain object ready for creation</returns>
    /// <exception cref="ArgumentNullException">Thrown when createResource is null</exception>
    public static Chat ToDomain(CreateChatResource createResource)
    {
        ArgumentNullException.ThrowIfNull(createResource);

        return new Chat
        {
            PersonaId = createResource.PersonaId,
            UserId = createResource.UserId,
            Title = createResource.Title,
            LastMessageAt = createResource.LastMessageAt
        };
    }

    /// <summary>
    /// Maps an UpdateChatResource to a Chat domain object for updates.
    /// </summary>
    /// <param name="updateResource">The UpdateChatResource from API request</param>
    /// <param name="existingChat">The existing Chat domain object to update</param>
    /// <returns>A Chat domain object with updated values</returns>
    /// <exception cref="ArgumentNullException">Thrown when updateResource or existingChat is null</exception>
    public static Chat ToDomain(UpdateChatResource updateResource, Chat existingChat)
    {
        ArgumentNullException.ThrowIfNull(updateResource);
        ArgumentNullException.ThrowIfNull(existingChat);

        return new Chat
        {
            Id = existingChat.Id,
            PersonaId = existingChat.PersonaId,
            UserId = existingChat.UserId,
            Title = updateResource.Title ?? existingChat.Title,
            LastMessageAt = updateResource.LastMessageAt ?? existingChat.LastMessageAt,
            CreatedAt = existingChat.CreatedAt,
            UpdatedAt = existingChat.UpdatedAt,
            CreatedBy = existingChat.CreatedBy,
            UpdatedBy = existingChat.UpdatedBy,
            IsDeleted = existingChat.IsDeleted,
            DeletedAt = existingChat.DeletedAt,
            DeletedBy = existingChat.DeletedBy
        };
    }
}

