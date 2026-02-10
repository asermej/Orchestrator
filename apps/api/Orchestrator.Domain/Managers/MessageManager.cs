using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Orchestrator.Domain;

/// <summary>
/// Manages business operations for Message entities
/// </summary>
internal sealed class MessageManager : IDisposable
{
    private readonly ServiceLocatorBase _serviceLocator;
    private DataFacade? _dataFacade;
    private DataFacade DataFacade => _dataFacade ??= new DataFacade(_serviceLocator.CreateConfigurationProvider().GetDbConnectionString());
    private GatewayFacade? _gatewayFacade;
    private GatewayFacade GatewayFacade => _gatewayFacade ??= new GatewayFacade(_serviceLocator);
    private AgentManager? _agentManager;
    private AgentManager AgentManager => _agentManager ??= new AgentManager(_serviceLocator);
    private TrainingStorageManager? _storageManager;
    private TrainingStorageManager StorageManager => _storageManager ??= new TrainingStorageManager();
    private ChatManager? _chatManager;
    private ChatManager ChatManager => _chatManager ??= new ChatManager(_serviceLocator);

    public MessageManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    /// <summary>
    /// Creates a new Message
    /// </summary>
    /// <param name="message">The Message entity to create</param>
    /// <returns>The created Message</returns>
    public async Task<Message> CreateMessage(Message message)
    {
        MessageValidator.Validate(message);
        
        return await DataFacade.AddMessage(message);
    }

    /// <summary>
    /// Creates a new user message and generates an AI response using LLM
    /// </summary>
    /// <param name="userMessage">The user's Message entity</param>
    /// <returns>The AI assistant's response Message</returns>
    public async Task<Message> CreateUserMessageAndGetAIResponse(Message userMessage)
    {
        // Validate and save the user's message
        MessageValidator.Validate(userMessage);
        
        if (userMessage.Role != "user")
        {
            throw new MessageValidationException("This method is only for user messages. Role must be 'user'.");
        }
        
        var savedUserMessage = await DataFacade.AddMessage(userMessage);

        // Get the Chat to extract the AgentId
        var chat = await DataFacade.GetChatById(userMessage.ChatId);
        if (chat == null)
        {
            throw new MessageValidationException($"Chat with ID {userMessage.ChatId} not found.");
        }

        // Get chat history for context
        var chatHistory = await DataFacade.SearchMessages(userMessage.ChatId, null, null, 1, 50);
        
        // Generate AI response using OpenAI Gateway (passing chatId for topic context)
        var aiResponseContent = await GenerateAIResponse(chat.AgentId, userMessage.ChatId, chatHistory.Items);
        
        // Create and save the AI's response
        var aiMessage = new Message
        {
            ChatId = userMessage.ChatId,
            Role = "assistant",
            Content = aiResponseContent
        };
        
        MessageValidator.Validate(aiMessage);
        var savedAiMessage = await DataFacade.AddMessage(aiMessage);
        
        // Update the chat's LastMessageAt timestamp
        var chatToUpdate = await DataFacade.GetChatById(userMessage.ChatId);
        if (chatToUpdate != null)
        {
            chatToUpdate.LastMessageAt = DateTime.UtcNow;
            await DataFacade.UpdateChat(chatToUpdate);
        }
        
        return savedAiMessage;
    }

    /// <summary>
    /// Generates an AI response using OpenAI based on agent characteristics, chat topics, and chat history
    /// </summary>
    /// <param name="agentId">The agent ID to get characteristics from</param>
    /// <param name="chatId">The chat ID to get topic context from</param>
    /// <param name="chatHistory">Previous messages in the conversation</param>
    /// <returns>The AI-generated response</returns>
    private async Task<string> GenerateAIResponse(Guid agentId, Guid chatId, IEnumerable<Message> chatHistory)
    {
        
        // Get the Agent details to inform the AI about the character
        var agent = await AgentManager.GetAgentById(agentId);
        if (agent == null)
        {
            throw new MessageValidationException($"Agent with ID {agentId} not found.");
        }

        // Get general training data for the agent
        var generalTraining = await AgentManager.GetAgentTraining(agentId);
        
        // Load topic content for each topic
        var topicContexts = new List<(string TopicName, string Content)>();
        
        // Build system prompt from agent characteristics, training, and topic context
        var systemPrompt = BuildSystemPrompt(agent, generalTraining, topicContexts);

        // Call OpenAI Gateway to generate response
        var aiResponse = await GatewayFacade.GenerateChatCompletion(systemPrompt, chatHistory);
        
        return aiResponse;
    }

    /// <summary>
    /// Builds a system prompt that defines the agent's behavior for the AI
    /// </summary>
    /// <param name="agent">The agent to build the prompt for</param>
    /// <param name="generalTraining">Optional general training data for the agent</param>
    /// <param name="topicContexts">Optional list of topic contexts loaded into the conversation</param>
    /// <returns>A system prompt string</returns>
    private string BuildSystemPrompt(Agent agent, string? generalTraining = null, List<(string TopicName, string Content)>? topicContexts = null)
    {
        var promptParts = new List<string>();

        // Start with the main identity
        promptParts.Add($"You are {agent.DisplayName}.");

        // Inject general training data if available
        if (!string.IsNullOrWhiteSpace(generalTraining))
        {
            promptParts.Add("\n\n## Background and Training");
            promptParts.Add(generalTraining);
        }

        // Inject topic-specific context if available
        if (topicContexts != null && topicContexts.Any())
        {
            promptParts.Add("\n\n## Conversation Topic Context");
            promptParts.Add("The following topics have been loaded into this conversation. Use this information to provide informed and contextually relevant responses:");
            
            foreach (var (topicName, content) in topicContexts)
            {
                promptParts.Add($"\n### {topicName}");
                promptParts.Add(content);
            }
        }

        // Add instructions for response style
        promptParts.Add("\n\n## Response Guidelines");
        promptParts.Add("Respond in character, maintaining your personality and characteristics throughout the conversation.");
        promptParts.Add("Be natural, engaging, and stay true to your character.");
        if (topicContexts != null && topicContexts.Any())
        {
            promptParts.Add("When relevant to the conversation, draw upon the topic context provided above to give more informed and detailed responses.");
        }

        return string.Join(" ", promptParts);
    }

    /// <summary>
    /// Gets a Message by ID
    /// </summary>
    /// <param name="id">The ID of the Message to get</param>
    /// <returns>The Message if found, null otherwise</returns>
    public async Task<Message?> GetMessageById(Guid id)
    {
        return await DataFacade.GetMessageById(id);
    }

    /// <summary>
    /// Searches for Messages
    /// </summary>
    /// <param name="chatId">Optional chat ID to filter by</param>
    /// <param name="role">Optional role to filter by</param>
    /// <param name="content">Optional content to search for</param>
    /// <param name="pageNumber">Page number for pagination</param>
    /// <param name="pageSize">Page size for pagination</param>
    /// <returns>A paginated list of Messages</returns>
    public async Task<PaginatedResult<Message>> SearchMessages(Guid? chatId, string? role, string? content, int pageNumber, int pageSize)
    {
        return await DataFacade.SearchMessages(chatId, role, content, pageNumber, pageSize);
    }

    /// <summary>
    /// Deletes a Message
    /// </summary>
    /// <param name="id">The ID of the Message to delete</param>
    /// <returns>True if the Message was deleted, false if not found</returns>
    public async Task<bool> DeleteMessage(Guid id)
    {
        return await DataFacade.DeleteMessage(id);
    }

    public void Dispose()
    {
        _gatewayFacade?.Dispose();
        _agentManager?.Dispose();
        _chatManager?.Dispose();
        // DataFacade doesn't implement IDisposable, so no disposal needed
    }
}

