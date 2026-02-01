using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

public sealed partial class DomainFacade
{
    public async Task<Tag> CreateTag(Tag tag)
    {
        return await TagManager.CreateTag(tag);
    }

    public async Task<Tag?> GetTagById(Guid id)
    {
        return await TagManager.GetTagById(id);
    }

    public async Task<Tag?> GetTagByName(string name)
    {
        return await TagManager.GetTagByName(name);
    }

    public async Task<Tag> GetOrCreateTag(string name, string? createdBy = null)
    {
        return await TagManager.GetOrCreateTag(name, createdBy);
    }

    public async Task<PaginatedResult<Tag>> SearchTags(string? searchTerm, int pageNumber, int pageSize)
    {
        return await TagManager.SearchTags(searchTerm, pageNumber, pageSize);
    }

    public async Task<bool> DeleteTag(Guid id)
    {
        return await TagManager.DeleteTag(id);
    }

    public async Task<IEnumerable<Tag>> GetTagsByTopicId(Guid topicId)
    {
        return await TagManager.GetTagsByTopicId(topicId);
    }
}

