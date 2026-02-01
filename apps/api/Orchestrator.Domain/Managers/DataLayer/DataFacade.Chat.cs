namespace Orchestrator.Domain;

internal sealed partial class DataFacade
{
    private ChatDataManager ChatDataManager => new(_dbConnectionString);

    public Task<Chat> AddChat(Chat chat)
    {
        return ChatDataManager.Add(chat);
    }

    public async Task<Chat?> GetChatById(System.Guid id)
    {
        return await ChatDataManager.GetById(id);
    }
    
    public Task<Chat> UpdateChat(Chat chat)
    {
        return ChatDataManager.Update(chat);
    }

    public Task<bool> DeleteChat(System.Guid id)
    {
        return ChatDataManager.Delete(id);
    }

    public Task<PaginatedResult<Chat>> SearchChats(Guid? personaId, Guid? userId, string? title, int pageNumber, int pageSize)
    {
        return ChatDataManager.Search(personaId, userId, title, pageNumber, pageSize);
    }
}

