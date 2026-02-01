using System;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

public sealed partial class DomainFacade
{
    public async Task<Topic> CreateTopic(Topic topic)
    {
        return await TopicManager.CreateTopic(topic);
    }

    public async Task<Topic?> GetTopicById(Guid id)
    {
        return await TopicManager.GetTopicById(id);
    }

    public async Task<PaginatedResult<Topic>> SearchTopics(string? name, Guid? personaId, int pageNumber, int pageSize)
    {
        return await TopicManager.SearchTopics(name, personaId, pageNumber, pageSize);
    }

    public async Task<(PaginatedResult<Topic> topics, Dictionary<Guid, List<Tag>> tagsByTopicId)> SearchTopicsWithTags(string? name, Guid? personaId, int pageNumber, int pageSize)
    {
        return await TopicManager.SearchTopicsWithTags(name, personaId, pageNumber, pageSize);
    }

    public async Task<Topic> UpdateTopic(Topic topic)
    {
        return await TopicManager.UpdateTopic(topic);
    }

    public async Task<bool> DeleteTopic(Guid id)
    {
        return await TopicManager.DeleteTopic(id);
    }

    public async Task<Topic> GetOrCreateDailyBlogTopic(DateTime date, Guid personaId, string contentUrl, string? createdBy = null)
    {
        // Get the DailyBlog category
        var categories = await CategoryManager.SearchCategories(null, "DailyBlog", true, 1, 10);
        var dailyBlogCategory = categories.Items.FirstOrDefault();
        
        if (dailyBlogCategory == null)
        {
            throw new InvalidOperationException("DailyBlog category not found");
        }

        // Format topic name with date
        var topicName = $"Daily Blog - {date:MMMM dd, yyyy}";

        // Search for existing topic with this name in DailyBlog category
        var existingTopics = await TopicManager.SearchTopics(topicName, personaId, 1, 10);
        var existingTopic = existingTopics.Items.FirstOrDefault(t => t.Name == topicName && t.CategoryId == dailyBlogCategory.Id);

        if (existingTopic != null)
        {
            return existingTopic;
        }

        // Create new daily blog topic
        var newTopic = new Topic
        {
            Name = topicName,
            Description = $"Daily blog entry for {date:MMMM dd, yyyy}",
            PersonaId = personaId,
            ContentUrl = contentUrl,
            CategoryId = dailyBlogCategory.Id,
            CreatedBy = createdBy
        };

        return await TopicManager.CreateTopic(newTopic);
    }

    public async Task<Tag> AddTagToTopic(Guid topicId, string tagName, string? createdBy = null)
    {
        return await TopicManager.AddTagToTopic(topicId, tagName, createdBy);
    }

    public async Task<bool> RemoveTagFromTopic(Guid topicId, Guid tagId)
    {
        return await TopicManager.RemoveTagFromTopic(topicId, tagId);
    }

    public async Task<IEnumerable<Tag>> GetTopicTags(Guid topicId)
    {
        return await TopicManager.GetTopicTags(topicId);
    }

    public async Task<IEnumerable<Tag>> UpdateTopicTags(Guid topicId, IEnumerable<string> tagNames, string? createdBy = null)
    {
        return await TopicManager.UpdateTopicTags(topicId, tagNames, createdBy);
    }

    public async Task<(PaginatedResult<TopicFeedData> feedData, System.Collections.Generic.Dictionary<Guid, IEnumerable<Tag>> tagsByTopicId)> SearchTopicsFeed(
        Guid? categoryId, 
        Guid[]? tagIds, 
        string? searchTerm,
        string? sortBy,
        int pageNumber, 
        int pageSize)
    {
        return await TopicManager.SearchTopicsFeed(categoryId, tagIds, searchTerm, sortBy, pageNumber, pageSize);
    }

    public async Task<string> SaveTopicTrainingContent(Guid topicId, string content)
    {
        return await TopicManager.SaveTopicTrainingContent(topicId, content);
    }

    public async Task<string> GetTopicTrainingContent(Guid topicId)
    {
        return await TopicManager.GetTopicTrainingContent(topicId);
    }

    /// <summary>
    /// Deletes training content by URL (used for cleanup when topic creation fails)
    /// </summary>
    /// <param name="contentUrl">The content URL to delete</param>
    public void DeleteTopicTrainingContentByUrl(string contentUrl)
    {
        TopicManager.DeleteTrainingContentByUrl(contentUrl);
    }
}

