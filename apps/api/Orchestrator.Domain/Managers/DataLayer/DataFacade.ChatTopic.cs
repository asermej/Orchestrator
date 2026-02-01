using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

internal sealed partial class DataFacade
{
    private ChatTopicDataManager ChatTopicDataManager => new(_dbConnectionString);

    public Task<ChatTopic> AddChatTopic(ChatTopic chatTopic)
    {
        return ChatTopicDataManager.Add(chatTopic);
    }

    public async Task<ChatTopic?> GetChatTopicById(Guid id)
    {
        return await ChatTopicDataManager.GetById(id);
    }

    public async Task<ChatTopic?> GetChatTopicByChatAndTopic(Guid chatId, Guid topicId)
    {
        return await ChatTopicDataManager.GetByChatAndTopic(chatId, topicId);
    }

    public Task<bool> DeleteChatTopic(Guid id)
    {
        return ChatTopicDataManager.Delete(id);
    }

    public Task<bool> DeleteChatTopicByChatAndTopic(Guid chatId, Guid topicId)
    {
        return ChatTopicDataManager.DeleteByChatAndTopic(chatId, topicId);
    }

    public Task<IEnumerable<ChatTopic>> GetChatTopicsByChat(Guid chatId)
    {
        return ChatTopicDataManager.GetByChat(chatId);
    }
}

