using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

public sealed partial class DomainFacade
{
    /// <summary>
    /// Creates a new message
    /// </summary>
    public async Task<Message> CreateMessage(Message message)
    {
        return await MessageManager.CreateMessage(message);
    }

    /// <summary>
    /// Creates a user message and generates an AI response using LLM
    /// This is the main method for sending messages in a chat
    /// </summary>
    public async Task<Message> CreateUserMessageAndGetAIResponse(Message userMessage)
    {
        return await MessageManager.CreateUserMessageAndGetAIResponse(userMessage);
    }

    /// <summary>
    /// Gets a message by ID
    /// </summary>
    public async Task<Message?> GetMessageById(Guid id)
    {
        return await MessageManager.GetMessageById(id);
    }

    /// <summary>
    /// Searches for messages with optional filters
    /// </summary>
    public async Task<PaginatedResult<Message>> SearchMessages(Guid? chatId, string? role, string? content, int pageNumber, int pageSize)
    {
        return await MessageManager.SearchMessages(chatId, role, content, pageNumber, pageSize);
    }

    /// <summary>
    /// Deletes a message
    /// </summary>
    public async Task<bool> DeleteMessage(Guid id)
    {
        return await MessageManager.DeleteMessage(id);
    }

    /// <summary>
    /// Gets or generates audio for a message (for replay/play button).
    /// Checks cache first, generates via ElevenLabs if not cached.
    /// </summary>
    /// <param name="messageId">The message ID</param>
    /// <returns>Audio bytes (MP3)</returns>
    public async Task<byte[]> GetMessageAudioAsync(Guid messageId)
    {
        // Get the message
        var message = await MessageManager.GetMessageById(messageId).ConfigureAwait(false);
        if (message == null)
        {
            throw new MessageNotFoundException($"Message with ID {messageId} not found");
        }

        // Only generate audio for assistant messages
        if (message.Role != "assistant")
        {
            throw new MessageValidationException("Audio can only be generated for assistant messages");
        }

        // Get the chat to find the persona
        var chat = await ChatManager.GetChatById(message.ChatId).ConfigureAwait(false);
        if (chat == null)
        {
            throw new ChatNotFoundException($"Chat with ID {message.ChatId} not found");
        }

        // Get persona for voice settings
        var persona = await PersonaManager.GetPersonaById(chat.PersonaId).ConfigureAwait(false);
        if (persona == null)
        {
            throw new PersonaNotFoundException($"Persona with ID {chat.PersonaId} not found");
        }

        // Get voice settings
        var config = new GatewayFacade(_serviceLocator).GetElevenLabsConfig();
        var voiceId = persona.ElevenLabsVoiceId ?? config.DefaultVoiceId;
        var stability = persona.VoiceStability;
        var similarityBoost = persona.VoiceSimilarityBoost;

        // Get or generate audio via cache manager
        return await AudioCacheManager.GetOrGenerateAudioAsync(
            message.Content,
            voiceId,
            stability,
            similarityBoost).ConfigureAwait(false);
    }
}

