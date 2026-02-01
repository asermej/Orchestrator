using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

internal sealed partial class DataFacade
{
    private TagDataManager TagDataManager => new(_dbConnectionString);

    public Task<Tag> AddTag(Tag tag)
    {
        return TagDataManager.Add(tag);
    }

    public async Task<Tag?> GetTagById(Guid id)
    {
        return await TagDataManager.GetById(id);
    }

    public async Task<Tag?> GetTagByName(string name)
    {
        return await TagDataManager.GetByName(name);
    }

    public Task<Tag> GetOrCreateTag(string name, string? createdBy = null)
    {
        return TagDataManager.GetOrCreate(name, createdBy);
    }

    public Task<bool> DeleteTag(Guid id)
    {
        return TagDataManager.Delete(id);
    }

    public Task<PaginatedResult<Tag>> SearchTags(string? searchTerm, int pageNumber, int pageSize)
    {
        return TagDataManager.Search(searchTerm, pageNumber, pageSize);
    }

    public Task<IEnumerable<Tag>> GetTagsByTopicId(Guid topicId)
    {
        return TagDataManager.GetTagsByTopicId(topicId);
    }

    public Task<Dictionary<Guid, List<Tag>>> GetTagsByTopicIds(Guid[] topicIds)
    {
        return TagDataManager.GetTagsByTopicIds(topicIds);
    }
}

