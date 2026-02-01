using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

public sealed partial class DomainFacade
{
    public async Task<Chat> CreateChat(Chat chat)
    {
        return await ChatManager.CreateChat(chat);
    }

    public async Task<Chat?> GetChatById(Guid id)
    {
        return await ChatManager.GetChatById(id);
    }

    public async Task<PaginatedResult<Chat>> SearchChats(Guid? personaId, Guid? userId, string? title, int pageNumber, int pageSize)
    {
        return await ChatManager.SearchChats(personaId, userId, title, pageNumber, pageSize);
    }

    public async Task<Chat> UpdateChat(Chat chat)
    {
        return await ChatManager.UpdateChat(chat);
    }

    public async Task<bool> DeleteChat(Guid id)
    {
        return await ChatManager.DeleteChat(id);
    }

    public async Task<IEnumerable<ChatTopic>> GetChatTopics(Guid chatId)
    {
        return await ChatManager.GetChatTopics(chatId);
    }

    public async Task<ChatTopic> AddTopicToChat(Guid chatId, Guid topicId)
    {
        return await ChatManager.AddTopicToChat(chatId, topicId);
    }

    public async Task<bool> RemoveTopicFromChat(Guid chatId, Guid topicId)
    {
        return await ChatManager.RemoveTopicFromChat(chatId, topicId);
    }
}

