using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;
using Orchestrator.Api.Mappers;
using Orchestrator.Api.Common;

namespace Orchestrator.Api.Controllers;

/// <summary>
/// Controller for Topic management operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class TopicController : ControllerBase
{
    private readonly DomainFacade _domainFacade;

    public TopicController(DomainFacade domainFacade)
    {
        _domainFacade = domainFacade;
    }

    /// <summary>
    /// Creates a new topic with training content
    /// </summary>
    /// <param name="resource">The topic data including training content</param>
    /// <returns>The created topic with its ID</returns>
    /// <response code="201">Returns the newly created topic</response>
    /// <response code="400">If the resource is invalid</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost]
    [ProducesResponseType(typeof(TopicResource), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<TopicResource>> Create([FromBody] CreateTopicResource resource)
    {
        // Step 1: Map resource to domain model
        var topic = TopicMapper.ToDomain(resource);
        topic.ContentUrl = string.Empty; // Will be set after saving content
        
        // Step 2: Create the topic first (so it exists in DB)
        var createdTopic = await _domainFacade.CreateTopic(topic);

        try
        {
            // Step 3: Save training content now that topic exists
            var contentUrl = await _domainFacade.SaveTopicTrainingContent(createdTopic.Id, resource.Content);
            
            // Step 4: Content URL is already updated by SaveTopicTrainingContent, fetch the updated topic
            createdTopic = await _domainFacade.GetTopicById(createdTopic.Id) ?? createdTopic;

            // Step 5: Return created topic
            var response = TopicMapper.ToResource(createdTopic);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }
        catch (Exception)
        {
            // Step 6: CLEANUP - Delete the topic if content save fails
            await _domainFacade.DeleteTopic(createdTopic.Id);
            throw; // Re-throw to let middleware handle error response
        }
    }

    /// <summary>
    /// Gets a topic by ID
    /// </summary>
    /// <param name="id">The ID of the topic</param>
    /// <returns>The topic if found</returns>
    /// <response code="200">Returns the topic</response>
    /// <response code="404">If the topic is not found</response>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TopicResource), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<TopicResource>> GetById(Guid id)
    {
        var topic = await _domainFacade.GetTopicById(id);
        if (topic == null)
        {
            return NotFound($"Topic with ID {id} not found");
        }

        var response = TopicMapper.ToResource(topic);
        
        // Load tags for this topic
        var tags = await _domainFacade.GetTopicTags(id);
        response.Tags = TagMapper.ToResource(tags).ToArray();
        
        return Ok(response);
    }

    /// <summary>
    /// Searches for topics with pagination
    /// </summary>
    /// <param name="request">The search criteria</param>
    /// <returns>A paginated list of topics</returns>
    /// <response code="200">Returns the paginated results</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PaginatedResponse<TopicResource>), 200)]
    public async Task<ActionResult<PaginatedResponse<TopicResource>>> Search([FromQuery] SearchTopicRequest request)
    {
        // Get topics with tags in a single optimized query (avoids N+1 problem)
        var (result, tagsByTopicId) = await _domainFacade.SearchTopicsWithTags(
            request.Name, 
            request.AgentId,
            request.PageNumber, 
            request.PageSize);

        var topicResources = TopicMapper.ToResource(result.Items).ToList();
        
        // Attach tags from the dictionary (already loaded)
        foreach (var topicResource in topicResources)
        {
            if (tagsByTopicId.TryGetValue(topicResource.Id, out var tags))
            {
                topicResource.Tags = TagMapper.ToResource(tags).ToArray();
            }
            else
            {
                topicResource.Tags = Array.Empty<TagResource>();
            }
        }

        var response = new PaginatedResponse<TopicResource>
        {
            Items = topicResources,
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };

        return Ok(response);
    }

    /// <summary>
    /// Updates a topic
    /// </summary>
    /// <param name="id">The ID of the topic</param>
    /// <param name="resource">The updated topic data</param>
    /// <returns>The updated topic</returns>
    /// <response code="200">Returns the updated topic</response>
    /// <response code="404">If the topic is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(TopicResource), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<TopicResource>> Update(Guid id, [FromBody] UpdateTopicResource resource)
    {
        // Get existing topic first
        var existingTopic = await _domainFacade.GetTopicById(id);
        if (existingTopic == null)
        {
            return NotFound($"Topic with ID {id} not found");
        }

        // Map update to domain object
        var topicToUpdate = TopicMapper.ToDomain(resource, existingTopic);
        
        var updatedTopic = await _domainFacade.UpdateTopic(topicToUpdate);

        var response = TopicMapper.ToResource(updatedTopic);
        
        return Ok(response);
    }

    /// <summary>
    /// Deletes a topic
    /// </summary>
    /// <param name="id">The ID of the topic</param>
    /// <returns>No content on success</returns>
    /// <response code="204">If the topic was deleted</response>
    /// <response code="404">If the topic is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<ActionResult> Delete(Guid id)
    {
        var deleted = await _domainFacade.DeleteTopic(id);
        if (!deleted)
        {
            return NotFound($"Topic with ID {id} not found");
        }

        return NoContent();
    }

    /// <summary>
    /// Gets or creates a Daily Blog topic for the specified date
    /// </summary>
    /// <param name="date">The date for the daily blog (format: yyyy-MM-dd)</param>
    /// <param name="createdBy">Optional: The ID of the user creating the topic</param>
    /// <returns>The existing or newly created daily blog topic</returns>
    /// <response code="200">Returns the daily blog topic</response>
    /// <response code="400">If the date format is invalid</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("daily-blog/{date}")]
    [ProducesResponseType(typeof(TopicResource), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<TopicResource>> GetOrCreateDailyBlog(string date, [FromQuery] Guid agentId, [FromQuery] string contentUrl, [FromQuery] string? createdBy = null)
    {
        if (!DateTime.TryParse(date, out var parsedDate))
        {
            return BadRequest($"Invalid date format: {date}. Expected format: yyyy-MM-dd");
        }

        var topic = await _domainFacade.GetOrCreateDailyBlogTopic(parsedDate, agentId, contentUrl, createdBy);
        var response = TopicMapper.ToResource(topic);
        
        return Ok(response);
    }

    /// <summary>
    /// Adds a tag to a topic
    /// </summary>
    /// <param name="topicId">The ID of the topic</param>
    /// <param name="resource">The tag data</param>
    /// <returns>The tag that was added</returns>
    /// <response code="200">Returns the added tag</response>
    /// <response code="404">If the topic is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("{topicId}/tags")]
    [ProducesResponseType(typeof(TagResource), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<TagResource>> AddTagToTopic(Guid topicId, [FromBody] AddTagToTopicResource resource)
    {
        var tag = await _domainFacade.AddTagToTopic(topicId, resource.TagName);
        var response = TagMapper.ToResource(tag);
        
        return Ok(response);
    }

    /// <summary>
    /// Gets all tags for a topic
    /// </summary>
    /// <param name="topicId">The ID of the topic</param>
    /// <returns>A list of tags associated with the topic</returns>
    /// <response code="200">Returns the list of tags</response>
    /// <response code="404">If the topic is not found</response>
    [HttpGet("{topicId}/tags")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TagResource[]), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<TagResource[]>> GetTopicTags(Guid topicId)
    {
        // Verify topic exists
        var topic = await _domainFacade.GetTopicById(topicId);
        if (topic == null)
        {
            return NotFound(new ErrorResponse { Message = $"Topic with ID {topicId} not found" });
        }

        var tags = await _domainFacade.GetTopicTags(topicId);
        var response = TagMapper.ToResource(tags).ToArray();
        
        return Ok(response);
    }

    /// <summary>
    /// Replaces all tags for a topic
    /// </summary>
    /// <param name="topicId">The ID of the topic</param>
    /// <param name="resource">The list of tag names</param>
    /// <returns>The new list of tags for the topic</returns>
    /// <response code="200">Returns the updated list of tags</response>
    /// <response code="404">If the topic is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPut("{topicId}/tags")]
    [ProducesResponseType(typeof(TagResource[]), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<TagResource[]>> UpdateTopicTags(Guid topicId, [FromBody] UpdateTopicTagsResource resource)
    {
        var tags = await _domainFacade.UpdateTopicTags(topicId, resource.TagNames);
        var response = TagMapper.ToResource(tags).ToArray();
        
        return Ok(response);
    }

    /// <summary>
    /// Removes a tag from a topic
    /// </summary>
    /// <param name="topicId">The ID of the topic</param>
    /// <param name="tagId">The ID of the tag to remove</param>
    /// <returns>No content on success</returns>
    /// <response code="204">If the tag was removed successfully</response>
    /// <response code="404">If the topic or tag association is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpDelete("{topicId}/tags/{tagId}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<ActionResult> RemoveTagFromTopic(Guid topicId, Guid tagId)
    {
        var result = await _domainFacade.RemoveTagFromTopic(topicId, tagId);
        
        if (!result)
        {
            return NotFound(new ErrorResponse { Message = $"Tag association not found for topic {topicId} and tag {tagId}" });
        }

        return NoContent();
    }

    /// <summary>
    /// Gets topics for the feed view with enriched data (author info, engagement metrics)
    /// </summary>
    /// <param name="request">The search criteria with pagination</param>
    /// <returns>A paginated list of topics with feed data</returns>
    /// <response code="200">Returns the paginated feed results</response>
    [HttpGet("feed")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PaginatedResponse<TopicFeedResource>), 200)]
    public async Task<ActionResult<PaginatedResponse<TopicFeedResource>>> GetFeed([FromQuery] SearchTopicFeedRequest request)
    {
        var (feedResult, tagsByTopicId) = await _domainFacade.SearchTopicsFeed(
            request.CategoryId,
            request.TagIds,
            request.SearchTerm,
            request.SortBy,
            request.PageNumber,
            request.PageSize
        );

        // Map feed data to resources
        var feedResourceItems = feedResult.Items.Select(feedData =>
        {
            var tags = tagsByTopicId.ContainsKey(feedData.Id) ? tagsByTopicId[feedData.Id] : Enumerable.Empty<Tag>();
            return TopicMapper.ToFeedResource(feedData, tags);
        }).ToList();

        var response = new PaginatedResponse<TopicFeedResource>
        {
            Items = feedResourceItems,
            TotalCount = feedResult.TotalCount,
            PageNumber = feedResult.PageNumber,
            PageSize = feedResult.PageSize
        };

        return Ok(response);
    }

    /// <summary>
    /// Saves training content for a topic
    /// </summary>
    /// <param name="topicId">The ID of the topic</param>
    /// <param name="resource">The training content to save</param>
    /// <returns>The storage URL where content was saved</returns>
    /// <response code="200">Returns the storage URL (file://, s3://, etc.)</response>
    /// <response code="404">If the topic is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("{topicId}/training")]
    [ProducesResponseType(typeof(SaveTopicTrainingResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<SaveTopicTrainingResponse>> SaveTrainingContent(
        Guid topicId, 
        [FromBody] SaveTopicTrainingResource resource)
    {
        var contentUrl = await _domainFacade.SaveTopicTrainingContent(topicId, resource.Content);
        
        return Ok(new SaveTopicTrainingResponse { ContentUrl = contentUrl });
    }

    /// <summary>
    /// Gets training content for a topic
    /// </summary>
    /// <param name="topicId">The ID of the topic</param>
    /// <returns>The training content</returns>
    /// <response code="200">Returns the training content</response>
    /// <response code="404">If the topic is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet("{topicId}/training")]
    [ProducesResponseType(typeof(GetTopicTrainingResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<GetTopicTrainingResponse>> GetTrainingContent(Guid topicId)
    {
        var content = await _domainFacade.GetTopicTrainingContent(topicId);
        
        return Ok(new GetTopicTrainingResponse { Content = content });
    }
}

