using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Orchestrator.Domain;
using Orchestrator.Api.ResourcesModels;
using Orchestrator.Api.Mappers;
using Orchestrator.Api.Common;

namespace Orchestrator.Api.Controllers;

/// <summary>
/// Controller for Chat management operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly DomainFacade _domainFacade;

    public ChatController(DomainFacade domainFacade)
    {
        _domainFacade = domainFacade;
    }

    /// <summary>
    /// Creates a new chat
    /// </summary>
    /// <param name="resource">The chat data</param>
    /// <returns>The created chat with its ID</returns>
    /// <response code="201">Returns the newly created chat</response>
    /// <response code="400">If the resource is invalid</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost]
    [ProducesResponseType(typeof(ChatResource), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<ChatResource>> Create([FromBody] CreateChatResource resource)
    {
        var chat = ChatMapper.ToDomain(resource);
        var createdChat = await _domainFacade.CreateChat(chat);

        var response = ChatMapper.ToResource(createdChat);
        
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    /// <summary>
    /// Gets a chat by ID
    /// </summary>
    /// <param name="id">The ID of the chat</param>
    /// <returns>The chat if found</returns>
    /// <response code="200">Returns the chat</response>
    /// <response code="404">If the chat is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ChatResource), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<ChatResource>> GetById(Guid id)
    {
        var chat = await _domainFacade.GetChatById(id);
        if (chat == null)
        {
            return NotFound($"Chat with ID {id} not found");
        }

        var response = ChatMapper.ToResource(chat);
        
        return Ok(response);
    }

    /// <summary>
    /// Searches for chats with pagination
    /// </summary>
    /// <param name="request">The search criteria</param>
    /// <returns>A paginated list of chats</returns>
    /// <response code="200">Returns the paginated results</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<ChatResource>), 200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<PaginatedResponse<ChatResource>>> Search([FromQuery] SearchChatRequest request)
    {
        var result = await _domainFacade.SearchChats(
            request.PersonaId, 
            request.UserId, 
            request.Title, 
            request.PageNumber, 
            request.PageSize);

        var response = new PaginatedResponse<ChatResource>
        {
            Items = ChatMapper.ToResource(result.Items),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };

        return Ok(response);
    }

    /// <summary>
    /// Updates a chat
    /// </summary>
    /// <param name="id">The ID of the chat</param>
    /// <param name="resource">The updated chat data</param>
    /// <returns>The updated chat</returns>
    /// <response code="200">Returns the updated chat</response>
    /// <response code="404">If the chat is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ChatResource), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<ChatResource>> Update(Guid id, [FromBody] UpdateChatResource resource)
    {
        // Get existing chat first
        var existingChat = await _domainFacade.GetChatById(id);
        if (existingChat == null)
        {
            return NotFound($"Chat with ID {id} not found");
        }

        // Map update to domain object
        var chatToUpdate = ChatMapper.ToDomain(resource, existingChat);
        var updatedChat = await _domainFacade.UpdateChat(chatToUpdate);

        var response = ChatMapper.ToResource(updatedChat);
        
        return Ok(response);
    }

    /// <summary>
    /// Deletes a chat
    /// </summary>
    /// <param name="id">The ID of the chat</param>
    /// <returns>No content on success</returns>
    /// <response code="204">If the chat was deleted</response>
    /// <response code="404">If the chat is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<ActionResult> Delete(Guid id)
    {
        var deleted = await _domainFacade.DeleteChat(id);
        if (!deleted)
        {
            return NotFound($"Chat with ID {id} not found");
        }

        return NoContent();
    }

    /// <summary>
    /// Gets all topics for a chat
    /// </summary>
    /// <param name="chatId">The chat ID</param>
    /// <returns>A list of topics</returns>
    /// <response code="200">Returns the topics for the chat</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet("{chatId}/topics")]
    [ProducesResponseType(typeof(IEnumerable<TopicResource>), 200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<IEnumerable<TopicResource>>> GetChatTopics(Guid chatId)
    {
        var chatTopics = await _domainFacade.GetChatTopics(chatId);
        
        // Load actual Topic entities for each ChatTopic
        var topics = new List<Topic>();
        foreach (var chatTopic in chatTopics)
        {
            var topic = await _domainFacade.GetTopicById(chatTopic.TopicId);
            if (topic != null)
            {
                topics.Add(topic);
            }
        }
        
        var topicResources = TopicMapper.ToResource(topics).ToList();
        
        // Load tags for all topics
        foreach (var topicResource in topicResources)
        {
            var tags = await _domainFacade.GetTopicTags(topicResource.Id);
            topicResource.Tags = TagMapper.ToResource(tags).ToArray();
        }

        return Ok(topicResources);
    }

    /// <summary>
    /// Adds a topic to a chat
    /// </summary>
    /// <param name="chatId">The chat ID</param>
    /// <param name="resource">The topic to add</param>
    /// <returns>The added topic</returns>
    /// <response code="201">Returns the added topic</response>
    /// <response code="400">If the resource is invalid</response>
    /// <response code="404">If the chat or topic is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("{chatId}/topics")]
    [ProducesResponseType(typeof(TopicResource), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<TopicResource>> AddTopicToChat(
        Guid chatId, 
        [FromBody] AddTopicToChatResource resource)
    {
        var chatTopic = await _domainFacade.AddTopicToChat(chatId, resource.TopicId);
        
        // Load the actual Topic entity
        var topic = await _domainFacade.GetTopicById(chatTopic.TopicId);
        if (topic == null)
        {
            return NotFound($"Topic with ID {chatTopic.TopicId} not found");
        }
        
        var response = TopicMapper.ToResource(topic);
        
        // Load tags for this topic
        var tags = await _domainFacade.GetTopicTags(response.Id);
        response.Tags = TagMapper.ToResource(tags).ToArray();

        return CreatedAtAction(nameof(GetChatTopics), new { chatId }, response);
    }

    /// <summary>
    /// Removes a topic from a chat
    /// </summary>
    /// <param name="chatId">The chat ID</param>
    /// <param name="topicId">The topic ID</param>
    /// <returns>No content on success</returns>
    /// <response code="204">If the topic was removed</response>
    /// <response code="404">If the link is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpDelete("{chatId}/topics/{topicId}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<ActionResult> RemoveTopicFromChat(Guid chatId, Guid topicId)
    {
        var deleted = await _domainFacade.RemoveTopicFromChat(chatId, topicId);
        if (!deleted)
        {
            return NotFound($"ChatTopic not found for chat {chatId} and topic {topicId}");
        }

        return NoContent();
    }
}

