using System;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

internal sealed partial class DataFacade
{
    private TopicDataManager TopicDataManager => new(_dbConnectionString);

    public Task<Topic> AddTopic(Topic topic)
    {
        return TopicDataManager.Add(topic);
    }

    public async Task<Topic?> GetTopicById(System.Guid id)
    {
        return await TopicDataManager.GetById(id);
    }
    
    public Task<Topic> UpdateTopic(Topic topic)
    {
        return TopicDataManager.Update(topic);
    }

    public Task<bool> DeleteTopic(System.Guid id)
    {
        return TopicDataManager.Delete(id);
    }

    public Task<PaginatedResult<Topic>> SearchTopics(string? name, Guid? personaId, int pageNumber, int pageSize)
    {
        return TopicDataManager.Search(name, personaId, pageNumber, pageSize);
    }

    public Task AddTopicTag(Guid topicId, Guid tagId)
    {
        return TopicDataManager.AddTopicTag(topicId, tagId);
    }

    public Task<bool> RemoveTopicTag(Guid topicId, Guid tagId)
    {
        return TopicDataManager.RemoveTopicTag(topicId, tagId);
    }

    public Task DeleteAllTopicTags(Guid topicId)
    {
        return TopicDataManager.DeleteAllTopicTags(topicId);
    }

    public Task<PaginatedResult<TopicFeedData>> SearchTopicsFeed(
        Guid? categoryId, 
        Guid[]? tagIds, 
        string? searchTerm,
        string? sortBy,
        int pageNumber, 
        int pageSize)
    {
        return TopicDataManager.SearchFeed(categoryId, tagIds, searchTerm, sortBy, pageNumber, pageSize);
    }
}

