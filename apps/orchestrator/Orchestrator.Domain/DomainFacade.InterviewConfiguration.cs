using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

public sealed partial class DomainFacade
{
    /// <summary>
    /// Creates a new InterviewConfiguration
    /// </summary>
    public async Task<InterviewConfiguration> CreateInterviewConfiguration(InterviewConfiguration config)
    {
        return await InterviewConfigurationManager.CreateConfiguration(config).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets an InterviewConfiguration by ID
    /// </summary>
    public async Task<InterviewConfiguration?> GetInterviewConfigurationById(Guid id)
    {
        return await InterviewConfigurationManager.GetConfigurationById(id).ConfigureAwait(false);
    }

    /// <summary>
    /// Searches for InterviewConfigurations
    /// </summary>
    public async Task<PaginatedResult<InterviewConfiguration>> SearchInterviewConfigurations(
        Guid? groupId, 
        Guid? agentId, 
        string? name, 
        bool? isActive,
        string? sortBy, 
        int pageNumber, 
        int pageSize,
        IReadOnlyList<Guid>? organizationIds = null)
    {
        return await InterviewConfigurationManager.SearchConfigurations(groupId, agentId, name, isActive, sortBy, pageNumber, pageSize, organizationIds).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates an InterviewConfiguration
    /// </summary>
    public async Task<InterviewConfiguration> UpdateInterviewConfiguration(InterviewConfiguration config)
    {
        return await InterviewConfigurationManager.UpdateConfiguration(config).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes an InterviewConfiguration (soft delete)
    /// </summary>
    public async Task<bool> DeleteInterviewConfiguration(Guid id, string? deletedBy = null)
    {
        return await InterviewConfigurationManager.DeleteConfiguration(id, deletedBy).ConfigureAwait(false);
    }
}
