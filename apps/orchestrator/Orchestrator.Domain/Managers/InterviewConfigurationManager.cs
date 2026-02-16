using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Orchestrator.Domain;

/// <summary>
/// Manages business operations for InterviewConfiguration entities
/// </summary>
internal sealed class InterviewConfigurationManager : IDisposable
{
    private readonly ServiceLocatorBase _serviceLocator;
    private DataFacade? _dataFacade;
    private DataFacade DataFacade => _dataFacade ??= new DataFacade(_serviceLocator.CreateConfigurationProvider().GetDbConnectionString());

    public InterviewConfigurationManager(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    /// <summary>
    /// Creates a new InterviewConfiguration
    /// </summary>
    public async Task<InterviewConfiguration> CreateConfiguration(InterviewConfiguration config)
    {
        InterviewConfigurationValidator.Validate(config);
        return await DataFacade.AddInterviewConfiguration(config).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets an InterviewConfiguration by ID
    /// </summary>
    public async Task<InterviewConfiguration?> GetConfigurationById(Guid id)
    {
        return await DataFacade.GetInterviewConfigurationById(id).ConfigureAwait(false);
    }

    /// <summary>
    /// Searches for InterviewConfigurations
    /// </summary>
    public async Task<PaginatedResult<InterviewConfiguration>> SearchConfigurations(
        Guid? groupId, 
        Guid? agentId, 
        string? name, 
        bool? isActive,
        string? sortBy, 
        int pageNumber, 
        int pageSize,
        IReadOnlyList<Guid>? organizationIds = null)
    {
        return await DataFacade.SearchInterviewConfigurations(groupId, agentId, name, isActive, sortBy, pageNumber, pageSize, organizationIds).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates an InterviewConfiguration
    /// </summary>
    public async Task<InterviewConfiguration> UpdateConfiguration(InterviewConfiguration config)
    {
        InterviewConfigurationValidator.Validate(config);
        return await DataFacade.UpdateInterviewConfiguration(config).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes an InterviewConfiguration (soft delete)
    /// </summary>
    public async Task<bool> DeleteConfiguration(Guid id, string? deletedBy = null)
    {
        return await DataFacade.DeleteInterviewConfiguration(id, deletedBy).ConfigureAwait(false);
    }

    public void Dispose()
    {
        // DataFacade doesn't implement IDisposable, so no disposal needed
    }
}
