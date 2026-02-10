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

    public async Task<PaginatedResult<Chat>> SearchChats(Guid? agentId, Guid? userId, string? title, int pageNumber, int pageSize)
    {
        return await ChatManager.SearchChats(agentId, userId, title, pageNumber, pageSize);
    }

    public async Task<Chat> UpdateChat(Chat chat)
    {
        return await ChatManager.UpdateChat(chat);
    }

    public async Task<bool> DeleteChat(Guid id)
    {
        return await ChatManager.DeleteChat(id);
    }
}

