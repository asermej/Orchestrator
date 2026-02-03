using System.Collections.Concurrent;

namespace Orchestrator.Domain;

/// <summary>
/// Widget-related methods for career site chatbot
/// </summary>
public sealed partial class DomainFacade
{
    // In-memory session storage for widget conversations
    // In production, this should use Redis or similar distributed cache
    private static readonly ConcurrentDictionary<string, List<Message>> _widgetSessions = new();

    /// <summary>
    /// Generates a chat response for the widget chatbot
    /// </summary>
    /// <param name="agentId">The agent ID to use for generating the response</param>
    /// <param name="sessionId">The session ID for conversation continuity</param>
    /// <param name="userMessage">The user's message</param>
    /// <returns>The AI-generated response</returns>
    public async Task<string> GenerateChatResponse(Guid agentId, string sessionId, string userMessage)
    {
        // Get the agent
        var agent = await AgentManager.GetAgentById(agentId).ConfigureAwait(false);
        if (agent == null)
        {
            throw new AgentNotFoundException($"Agent with ID {agentId} not found.");
        }

        // Get or create session history
        var sessionKey = $"{agentId}:{sessionId}";
        var history = _widgetSessions.GetOrAdd(sessionKey, _ => new List<Message>());

        // Add user message to history
        lock (history)
        {
            history.Add(new Message
            {
                Role = "user",
                Content = userMessage
            });
        }

        // Build system prompt
        var systemPrompt = BuildWidgetSystemPrompt(agent);

        // Generate AI response
        IEnumerable<Message> historySnapshot;
        lock (history)
        {
            // Take last 20 messages to keep context manageable
            historySnapshot = history.TakeLast(20).ToList();
        }

        var response = await GatewayFacade.GenerateChatCompletion(systemPrompt, historySnapshot).ConfigureAwait(false);

        // Add AI response to history
        lock (history)
        {
            history.Add(new Message
            {
                Role = "assistant",
                Content = response
            });
        }

        return response;
    }

    /// <summary>
    /// Builds a system prompt for the widget chatbot
    /// </summary>
    private static string BuildWidgetSystemPrompt(Agent agent)
    {
        var promptParts = new List<string>();

        // Start with identity
        promptParts.Add($"You are {agent.DisplayName}, an AI assistant helping candidates learn about job opportunities.");

        // Add custom system prompt if available
        if (!string.IsNullOrWhiteSpace(agent.SystemPrompt))
        {
            promptParts.Add(agent.SystemPrompt);
        }

        // Add interview guidelines if available
        if (!string.IsNullOrWhiteSpace(agent.InterviewGuidelines))
        {
            promptParts.Add($"Follow these guidelines: {agent.InterviewGuidelines}");
        }

        // Default chatbot instructions
        promptParts.Add("Be helpful, professional, and friendly.");
        promptParts.Add("Answer questions about the company and job opportunities.");
        promptParts.Add("If you don't know something, say so honestly.");
        promptParts.Add("Keep responses concise but informative.");

        return string.Join(" ", promptParts);
    }
}
