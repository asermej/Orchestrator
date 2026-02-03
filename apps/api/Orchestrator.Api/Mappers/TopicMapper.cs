using System;
using System.Collections.Generic;
using System.Linq;
using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;

namespace Orchestrator.Api.Mappers;

/// <summary>
/// Mapper class for converting between Topic domain objects and TopicResource API models.
/// </summary>
public static class TopicMapper
{
    /// <summary>
    /// Maps a Topic domain object to a TopicResource for API responses.
    /// </summary>
    /// <param name="topic">The domain Topic object to map</param>
    /// <returns>A TopicResource object suitable for API responses</returns>
    /// <exception cref="ArgumentNullException">Thrown when topic is null</exception>
    public static TopicResource ToResource(Topic topic)
    {
        ArgumentNullException.ThrowIfNull(topic);

        return new TopicResource
        {
            Id = topic.Id,
            Name = topic.Name,
            Description = topic.Description,
            CategoryId = topic.CategoryId,
            AgentId = topic.AgentId,
            ContentUrl = topic.ContentUrl,
            ContributionNotes = topic.ContributionNotes,
            CreatedAt = topic.CreatedAt,
            UpdatedAt = topic.UpdatedAt
        };
    }

    /// <summary>
    /// Maps a collection of Topic domain objects to TopicResource objects.
    /// </summary>
    /// <param name="topics">The collection of domain Topic objects to map</param>
    /// <returns>A collection of TopicResource objects suitable for API responses</returns>
    /// <exception cref="ArgumentNullException">Thrown when topics is null</exception>
    public static IEnumerable<TopicResource> ToResource(IEnumerable<Topic> topics)
    {
        ArgumentNullException.ThrowIfNull(topics);

        return topics.Select(ToResource);
    }

    /// <summary>
    /// Maps a CreateTopicResource to a Topic domain object for creation.
    /// Note: ContentUrl is not mapped here - it will be set by the controller after saving training content
    /// </summary>
    /// <param name="createResource">The CreateTopicResource from API request</param>
    /// <returns>A Topic domain object ready for creation</returns>
    /// <exception cref="ArgumentNullException">Thrown when createResource is null</exception>
    public static Topic ToDomain(CreateTopicResource createResource)
    {
        ArgumentNullException.ThrowIfNull(createResource);

        return new Topic
        {
            Name = createResource.Name,
            Description = createResource.Description,
            CategoryId = createResource.CategoryId,
            AgentId = createResource.AgentId,
            ContentUrl = string.Empty, // Will be set by controller after saving training content
            ContributionNotes = createResource.ContributionNotes
        };
    }

    /// <summary>
    /// Maps an UpdateTopicResource to a Topic domain object for updates.
    /// Note: ContentUrl is not mapped here - it will be set by the controller if new content is provided
    /// </summary>
    /// <param name="updateResource">The UpdateTopicResource from API request</param>
    /// <param name="existingTopic">The existing Topic domain object to update</param>
    /// <returns>A Topic domain object with updated values</returns>
    /// <exception cref="ArgumentNullException">Thrown when updateResource or existingTopic is null</exception>
    public static Topic ToDomain(UpdateTopicResource updateResource, Topic existingTopic)
    {
        ArgumentNullException.ThrowIfNull(updateResource);
        ArgumentNullException.ThrowIfNull(existingTopic);

        return new Topic
        {
            Id = existingTopic.Id,
            Name = updateResource.Name ?? existingTopic.Name,
            Description = updateResource.Description ?? existingTopic.Description,
            CategoryId = updateResource.CategoryId ?? existingTopic.CategoryId,
            AgentId = existingTopic.AgentId,
            ContentUrl = existingTopic.ContentUrl, // Keep existing - controller will update if new content provided
            ContributionNotes = updateResource.ContributionNotes ?? existingTopic.ContributionNotes,
            CreatedBy = existingTopic.CreatedBy,
            CreatedAt = existingTopic.CreatedAt,
            UpdatedAt = existingTopic.UpdatedAt,
            UpdatedBy = existingTopic.UpdatedBy,
            IsDeleted = existingTopic.IsDeleted,
            DeletedAt = existingTopic.DeletedAt,
            DeletedBy = existingTopic.DeletedBy
        };
    }

    /// <summary>
    /// Maps a TopicFeedData domain object to a TopicFeedResource for API feed responses.
    /// </summary>
    /// <param name="feedData">The TopicFeedData object to map</param>
    /// <param name="tags">The tags associated with this topic</param>
    /// <returns>A TopicFeedResource object suitable for feed API responses</returns>
    /// <exception cref="ArgumentNullException">Thrown when feedData is null</exception>
    public static TopicFeedResource ToFeedResource(TopicFeedData feedData, IEnumerable<Tag> tags)
    {
        ArgumentNullException.ThrowIfNull(feedData);
        ArgumentNullException.ThrowIfNull(tags);

        var author = new TopicAuthorResource
        {
            Id = feedData.AuthorId,
            FirstName = feedData.AuthorFirstName ?? string.Empty,
            LastName = feedData.AuthorLastName ?? string.Empty,
            ProfileImageUrl = feedData.AuthorProfileImageUrl
        };

        return new TopicFeedResource
        {
            Id = feedData.Id,
            Name = feedData.Name,
            Description = feedData.Description,
            AgentId = feedData.AgentId,
            Author = author,
            ChatCount = feedData.ChatCount,
            Category = new CategoryResource 
            { 
                Id = feedData.CategoryId,
                Name = feedData.CategoryName
            },
            Tags = TagMapper.ToResource(tags).ToArray(),
            CreatedAt = feedData.CreatedAt,
            UpdatedAt = feedData.UpdatedAt
        };
    }

    /// <summary>
    /// Maps a collection of TopicFeedData objects to TopicFeedResource objects.
    /// </summary>
    /// <param name="feedDataWithTags">A collection of tuples containing feed data and their associated tags</param>
    /// <returns>A collection of TopicFeedResource objects suitable for feed API responses</returns>
    public static IEnumerable<TopicFeedResource> ToFeedResource(IEnumerable<(TopicFeedData feedData, IEnumerable<Tag> tags)> feedDataWithTags)
    {
        ArgumentNullException.ThrowIfNull(feedDataWithTags);

        return feedDataWithTags.Select(item => ToFeedResource(item.feedData, item.tags));
    }
}

