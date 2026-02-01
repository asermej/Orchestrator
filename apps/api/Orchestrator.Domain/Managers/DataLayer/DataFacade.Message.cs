namespace Orchestrator.Domain;

internal sealed partial class DataFacade
{
    private MessageDataManager MessageDataManager => new(_dbConnectionString);

    public Task<Message> AddMessage(Message message)
    {
        return MessageDataManager.Add(message);
    }

    public async Task<Message?> GetMessageById(System.Guid id)
    {
        return await MessageDataManager.GetById(id);
    }

    public Task<bool> DeleteMessage(System.Guid id)
    {
        return MessageDataManager.Delete(id);
    }

    public Task<PaginatedResult<Message>> SearchMessages(Guid? chatId, string? role, string? content, int pageNumber, int pageSize)
    {
        return MessageDataManager.Search(chatId, role, content, pageNumber, pageSize);
    }
}

