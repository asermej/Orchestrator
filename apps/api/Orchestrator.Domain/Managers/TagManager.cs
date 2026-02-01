using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Orchestrator.Domain;

/// <summary>
/// Manages business operations for Tag entities
/// </summary>
internal sealed class TagManager : IDisposable
{
    private readonly ServiceLocatorBase _serviceLocator;
    private DataFacade? _dataFacade;
    private DataFacade DataFacade => _dataFacade ??= new DataFacade(_serviceLocator.CreateConfigurationProvider().GetDbConnectionString());

    public TagManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    /// <summary>
    /// Creates a new Tag
    /// </summary>
    /// <param name="tag">The Tag entity to create</param>
    /// <returns>The created Tag</returns>
    public async Task<Tag> CreateTag(Tag tag)
    {
        TagValidator.Validate(tag);
        
        // Check for duplicate tag name (case-insensitive)
        var existingTag = await DataFacade.GetTagByName(tag.Name).ConfigureAwait(false);
        if (existingTag != null)
        {
            throw new TagDuplicateException($"A tag with name '{tag.Name}' already exists.");
        }
        
        return await DataFacade.AddTag(tag).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a Tag by ID
    /// </summary>
    /// <param name="id">The ID of the Tag to get</param>
    /// <returns>The Tag if found, null otherwise</returns>
    public async Task<Tag?> GetTagById(Guid id)
    {
        return await DataFacade.GetTagById(id).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a Tag by name (case-insensitive)
    /// </summary>
    /// <param name="name">The name of the Tag to get</param>
    /// <returns>The Tag if found, null otherwise</returns>
    public async Task<Tag?> GetTagByName(string name)
    {
        return await DataFacade.GetTagByName(name).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets or creates a tag with the given name
    /// </summary>
    /// <param name="name">The tag name</param>
    /// <param name="createdBy">The user creating the tag</param>
    /// <returns>The existing or newly created Tag</returns>
    public async Task<Tag> GetOrCreateTag(string name, string? createdBy = null)
    {
        // Create a temporary tag for validation
        var tempTag = new Tag { Name = name };
        TagValidator.Validate(tempTag);
        
        // Use the normalized name from validation
        return await DataFacade.GetOrCreateTag(tempTag.Name, createdBy).ConfigureAwait(false);
    }

    /// <summary>
    /// Searches for Tags
    /// </summary>
    /// <param name="searchTerm">Optional search term to filter tags</param>
    /// <param name="pageNumber">Page number for pagination</param>
    /// <param name="pageSize">Page size for pagination</param>
    /// <returns>A paginated list of Tags</returns>
    public async Task<PaginatedResult<Tag>> SearchTags(string? searchTerm, int pageNumber, int pageSize)
    {
        return await DataFacade.SearchTags(searchTerm, pageNumber, pageSize).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes a Tag
    /// </summary>
    /// <param name="id">The ID of the Tag to delete</param>
    /// <returns>True if the Tag was deleted, false if not found</returns>
    public async Task<bool> DeleteTag(Guid id)
    {
        return await DataFacade.DeleteTag(id).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets all tags for a specific topic
    /// </summary>
    /// <param name="topicId">The ID of the topic</param>
    /// <returns>A collection of Tags</returns>
    public async Task<IEnumerable<Tag>> GetTagsByTopicId(Guid topicId)
    {
        return await DataFacade.GetTagsByTopicId(topicId).ConfigureAwait(false);
    }

    public void Dispose()
    {
        // DataFacade doesn't implement IDisposable, so no disposal needed
    }
}

