using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Orchestrator.Domain;

/// <summary>
/// Manages business operations for Topic entities
/// </summary>
internal sealed class TopicManager : IDisposable
{
    private readonly ServiceLocatorBase _serviceLocator;
    private DataFacade? _dataFacade;
    private TrainingStorageManager? _storageManager;
    
    private DataFacade DataFacade => _dataFacade ??= new DataFacade(_serviceLocator.CreateConfigurationProvider().GetDbConnectionString());
    private TrainingStorageManager StorageManager => _storageManager ??= new TrainingStorageManager();

    public TopicManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    /// <summary>
    /// Creates a new Topic
    /// </summary>
    /// <param name="topic">The Topic entity to create</param>
    /// <returns>The created Topic</returns>
    public async Task<Topic> CreateTopic(Topic topic)
    {
        TopicValidator.Validate(topic);
        
        return await DataFacade.AddTopic(topic).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a Topic by ID
    /// </summary>
    /// <param name="id">The ID of the Topic to get</param>
    /// <returns>The Topic if found, null otherwise</returns>
    public async Task<Topic?> GetTopicById(Guid id)
    {
        return await DataFacade.GetTopicById(id).ConfigureAwait(false);
    }

    /// <summary>
    /// Searches for Topics
    /// </summary>
    /// <param name="name">Optional name to search for</param>
    /// <param name="personaId">Optional persona ID to filter by</param>
    /// <param name="pageNumber">Page number for pagination</param>
    /// <param name="pageSize">Page size for pagination</param>
    /// <returns>A paginated list of Topics</returns>
    public async Task<PaginatedResult<Topic>> SearchTopics(string? name, Guid? personaId, int pageNumber, int pageSize)
    {
        return await DataFacade.SearchTopics(name, personaId, pageNumber, pageSize).ConfigureAwait(false);
    }

    /// <summary>
    /// Searches for Topics with their tags included (optimized to avoid N+1 queries)
    /// </summary>
    /// <param name="name">Optional name to search for</param>
    /// <param name="personaId">Optional persona ID to filter by</param>
    /// <param name="pageNumber">Page number for pagination</param>
    /// <param name="pageSize">Page size for pagination</param>
    /// <returns>A tuple containing the paginated list of Topics and a dictionary mapping topic IDs to their tags</returns>
    public async Task<(PaginatedResult<Topic> topics, Dictionary<Guid, List<Tag>> tagsByTopicId)> SearchTopicsWithTags(string? name, Guid? personaId, int pageNumber, int pageSize)
    {
        // Get topics
        var topicsResult = await DataFacade.SearchTopics(name, personaId, pageNumber, pageSize).ConfigureAwait(false);
        
        // Get all tags for these topics in a single query (avoids N+1 problem)
        var topicIds = topicsResult.Items.Select(t => t.Id).ToArray();
        var tagsByTopicId = await DataFacade.GetTagsByTopicIds(topicIds).ConfigureAwait(false);
        
        return (topicsResult, tagsByTopicId);
    }

    /// <summary>
    /// Updates a Topic
    /// </summary>
    /// <param name="topic">The Topic entity with updated data</param>
    /// <returns>The updated Topic</returns>
    public async Task<Topic> UpdateTopic(Topic topic)
    {
        TopicValidator.Validate(topic);
        
        return await DataFacade.UpdateTopic(topic).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes a Topic
    /// </summary>
    /// <param name="id">The ID of the Topic to delete</param>
    /// <returns>True if the Topic was deleted, false if not found</returns>
    public async Task<bool> DeleteTopic(Guid id)
    {
        return await DataFacade.DeleteTopic(id).ConfigureAwait(false);
    }

    /// <summary>
    /// Adds a tag to a topic (creates or finds the tag, then links it)
    /// </summary>
    /// <param name="topicId">The topic ID</param>
    /// <param name="tagName">The tag name (will be normalized)</param>
    /// <param name="createdBy">The user creating the association</param>
    /// <returns>The tag that was added</returns>
    public async Task<Tag> AddTagToTopic(Guid topicId, string tagName, string? createdBy = null)
    {
        // Verify topic exists
        var topic = await GetTopicById(topicId);
        if (topic == null)
        {
            throw new TopicNotFoundException($"Topic with ID {topicId} not found.");
        }

        // Get or create the tag
        var tag = await DataFacade.GetOrCreateTag(tagName, createdBy).ConfigureAwait(false);

        // Link tag to topic
        await DataFacade.AddTopicTag(topicId, tag.Id).ConfigureAwait(false);

        return tag;
    }

    /// <summary>
    /// Removes a tag from a topic
    /// </summary>
    /// <param name="topicId">The topic ID</param>
    /// <param name="tagId">The tag ID</param>
    /// <returns>True if the tag was removed, false if the association didn't exist</returns>
    public async Task<bool> RemoveTagFromTopic(Guid topicId, Guid tagId)
    {
        return await DataFacade.RemoveTopicTag(topicId, tagId).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets all tags for a topic
    /// </summary>
    /// <param name="topicId">The topic ID</param>
    /// <returns>A collection of tags</returns>
    public async Task<IEnumerable<Tag>> GetTopicTags(Guid topicId)
    {
        return await DataFacade.GetTagsByTopicId(topicId).ConfigureAwait(false);
    }

    /// <summary>
    /// Replaces all tags for a topic
    /// </summary>
    /// <param name="topicId">The topic ID</param>
    /// <param name="tagNames">The new set of tag names</param>
    /// <param name="createdBy">The user making the update</param>
    /// <returns>The collection of tags now associated with the topic</returns>
    public async Task<IEnumerable<Tag>> UpdateTopicTags(Guid topicId, IEnumerable<string> tagNames, string? createdBy = null)
    {
        // Verify topic exists
        var topic = await GetTopicById(topicId);
        if (topic == null)
        {
            throw new TopicNotFoundException($"Topic with ID {topicId} not found.");
        }

        // Remove all existing tags
        await DataFacade.DeleteAllTopicTags(topicId).ConfigureAwait(false);

        // Add new tags
        var tags = new List<Tag>();
        foreach (var tagName in tagNames.Distinct())
        {
            if (!string.IsNullOrWhiteSpace(tagName))
            {
                var tag = await DataFacade.GetOrCreateTag(tagName, createdBy).ConfigureAwait(false);
                await DataFacade.AddTopicTag(topicId, tag.Id).ConfigureAwait(false);
                tags.Add(tag);
            }
        }

        return tags;
    }

    /// <summary>
    /// Searches for topics with enriched feed data (author info, engagement metrics)
    /// </summary>
    /// <param name="categoryId">Optional category ID to filter by</param>
    /// <param name="tagIds">Optional tag IDs to filter by (topics must have at least one)</param>
    /// <param name="searchTerm">Optional search term for topic name/description</param>
    /// <param name="sortBy">Sort order: "popular" (default), "recent", "chat_count"</param>
    /// <param name="pageNumber">Page number for pagination</param>
    /// <param name="pageSize">Page size for pagination</param>
    /// <returns>A paginated list of TopicFeedData with associated tags</returns>
    public async Task<(PaginatedResult<TopicFeedData> feedData, Dictionary<Guid, IEnumerable<Tag>> tagsByTopicId)> SearchTopicsFeed(
        Guid? categoryId, 
        Guid[]? tagIds, 
        string? searchTerm,
        string? sortBy,
        int pageNumber, 
        int pageSize)
    {
        // Get feed data from data layer
        var feedResult = await DataFacade.SearchTopicsFeed(categoryId, tagIds, searchTerm, sortBy, pageNumber, pageSize).ConfigureAwait(false);

        // Get tags for all topics in the result
        var tagsByTopicId = new Dictionary<Guid, IEnumerable<Tag>>();
        foreach (var feedData in feedResult.Items)
        {
            var tags = await GetTopicTags(feedData.Id).ConfigureAwait(false);
            tagsByTopicId[feedData.Id] = tags;
        }

        return (feedResult, tagsByTopicId);
    }

    /// <summary>
    /// Saves topic training content to storage and updates the topic's ContentUrl
    /// </summary>
    /// <param name="topicId">The topic ID</param>
    /// <param name="content">The training content</param>
    /// <returns>The file URL where the content was saved</returns>
    public async Task<string> SaveTopicTrainingContent(Guid topicId, string content)
    {
        // Get the topic to verify it exists and get persona ID
        var topic = await GetTopicById(topicId);
        if (topic == null)
        {
            throw new TopicNotFoundException($"Topic with ID {topicId} not found.");
        }

        // Save the training content and get the file URL
        var contentUrl = await StorageManager.SaveTopicTraining(topic.PersonaId, topicId, content);

        // Update the topic's ContentUrl with the file URL
        topic.ContentUrl = contentUrl;
        await UpdateTopic(topic).ConfigureAwait(false);

        return contentUrl;
    }

    /// <summary>
    /// Retrieves topic training content from storage
    /// </summary>
    /// <param name="topicId">The topic ID</param>
    /// <returns>The training content, or empty string if not found</returns>
    public async Task<string> GetTopicTrainingContent(Guid topicId)
    {
        // Get the topic
        var topic = await GetTopicById(topicId);
        if (topic == null)
        {
            throw new TopicNotFoundException($"Topic with ID {topicId} not found.");
        }

        // Retrieve content from the URL
        return await StorageManager.GetTrainingFromUrl(topic.ContentUrl);
    }

    /// <summary>
    /// Deletes training content by URL (used for cleanup when topic creation fails)
    /// </summary>
    /// <param name="contentUrl">The content URL to delete</param>
    public void DeleteTrainingContentByUrl(string contentUrl)
    {
        if (string.IsNullOrWhiteSpace(contentUrl))
        {
            return;
        }

        StorageManager.DeleteTraining(contentUrl);
    }

    public void Dispose()
    {
        // DataFacade doesn't implement IDisposable, so no disposal needed
    }
}

