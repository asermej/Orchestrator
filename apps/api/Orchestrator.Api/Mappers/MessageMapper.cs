using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;

namespace Orchestrator.Api.Mappers;

/// <summary>
/// Mapper class for converting between Message domain objects and MessageResource API models.
/// </summary>
public static class MessageMapper
{
    /// <summary>
    /// Maps a Message domain object to a MessageResource for API responses.
    /// </summary>
    /// <param name="message">The domain Message object to map</param>
    /// <returns>A MessageResource object suitable for API responses</returns>
    /// <exception cref="ArgumentNullException">Thrown when message is null</exception>
    public static MessageResource ToResource(Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return new MessageResource
        {
            Id = message.Id,
            ChatId = message.ChatId,
            Role = message.Role,
            Content = message.Content,
            CreatedAt = message.CreatedAt
        };
    }

    /// <summary>
    /// Maps a collection of Message domain objects to MessageResource objects.
    /// </summary>
    /// <param name="messages">The collection of domain Message objects to map</param>
    /// <returns>A collection of MessageResource objects suitable for API responses</returns>
    /// <exception cref="ArgumentNullException">Thrown when messages is null</exception>
    public static IEnumerable<MessageResource> ToResource(IEnumerable<Message> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);

        return messages.Select(ToResource);
    }

    /// <summary>
    /// Maps a CreateMessageResource to a Message domain object for creation.
    /// </summary>
    /// <param name="createResource">The CreateMessageResource from API request</param>
    /// <returns>A Message domain object ready for creation</returns>
    /// <exception cref="ArgumentNullException">Thrown when createResource is null</exception>
    public static Message ToDomain(CreateMessageResource createResource)
    {
        ArgumentNullException.ThrowIfNull(createResource);

        return new Message
        {
            ChatId = createResource.ChatId,
            Role = createResource.Role,
            Content = createResource.Content
        };
    }

    /// <summary>
    /// Maps a SendMessageResource to a Message domain object for LLM processing.
    /// This creates a user message that will be sent to the LLM for response generation.
    /// </summary>
    /// <param name="sendMessageResource">The SendMessageResource from API request</param>
    /// <returns>A Message domain object with role set to "user"</returns>
    /// <exception cref="ArgumentNullException">Thrown when sendMessageResource is null</exception>
    public static Message ToDomain(SendMessageResource sendMessageResource)
    {
        ArgumentNullException.ThrowIfNull(sendMessageResource);

        return new Message
        {
            ChatId = sendMessageResource.ChatId,
            Role = "user", // Always user role for this endpoint
            Content = sendMessageResource.Content
        };
    }
}

