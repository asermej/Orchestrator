using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Orchestrator.Domain;

/// <summary>
/// Manages business operations for Agent (AI Interviewer) entities
/// </summary>
internal sealed class AgentManager : IDisposable
{
    private readonly ServiceLocatorBase _serviceLocator;
    private DataFacade? _dataFacade;
    private DataFacade DataFacade => _dataFacade ??= new DataFacade(_serviceLocator.CreateConfigurationProvider().GetDbConnectionString());

    public AgentManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    /// <summary>
    /// Creates a new Agent
    /// </summary>
    public async Task<Agent> CreateAgent(Agent agent)
    {
        AgentValidator.Validate(agent);
        
        // Check for duplicate display name within the group
        var existingAgents = await DataFacade.SearchAgents(agent.GroupId, agent.DisplayName, null, null, 1, 1).ConfigureAwait(false);
        if (existingAgents.Items.Any())
        {
            throw new AgentDuplicateDisplayNameException($"An agent with display name '{agent.DisplayName}' already exists in this group.");
        }
        
        return await DataFacade.AddAgent(agent).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets an Agent by ID
    /// </summary>
    public async Task<Agent?> GetAgentById(Guid id)
    {
        return await DataFacade.GetAgentById(id).ConfigureAwait(false);
    }

    /// <summary>
    /// Searches for Agents
    /// </summary>
    public async Task<PaginatedResult<Agent>> SearchAgents(Guid? groupId, string? displayName, string? createdBy, string? sortBy, int pageNumber, int pageSize, IReadOnlyList<Guid>? organizationIds = null)
    {
        return await DataFacade.SearchAgents(groupId, displayName, createdBy, sortBy, pageNumber, pageSize, organizationIds).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates an Agent
    /// </summary>
    public async Task<Agent> UpdateAgent(Agent agent)
    {
        AgentValidator.Validate(agent);
        
        // Check for duplicate display name (excluding the current agent)
        var existingAgents = await DataFacade.SearchAgents(agent.GroupId, agent.DisplayName, null, null, 1, 1).ConfigureAwait(false);
        var duplicateAgent = existingAgents.Items.FirstOrDefault();
        if (duplicateAgent != null && duplicateAgent.Id != agent.Id)
        {
            throw new AgentDuplicateDisplayNameException($"An agent with display name '{agent.DisplayName}' already exists in this group.");
        }
        
        return await DataFacade.UpdateAgent(agent).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes an Agent
    /// </summary>
    public async Task<bool> DeleteAgent(Guid id)
    {
        return await DataFacade.DeleteAgent(id).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets training data for an Agent (system prompt and guidelines)
    /// </summary>
    public async Task<string?> GetAgentTraining(Guid agentId)
    {
        var agent = await DataFacade.GetAgentById(agentId).ConfigureAwait(false);
        if (agent == null)
        {
            return null;
        }

        // Combine system prompt and guidelines as training data
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(agent.SystemPrompt))
        {
            parts.Add(agent.SystemPrompt);
        }
        if (!string.IsNullOrWhiteSpace(agent.InterviewGuidelines))
        {
            parts.Add(agent.InterviewGuidelines);
        }

        return parts.Count > 0 ? string.Join("\n\n", parts) : null;
    }

    public void Dispose()
    {
        // DataFacade doesn't implement IDisposable, so no disposal needed
    }
}
