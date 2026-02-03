using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

public sealed partial class DomainFacade
{
    /// <summary>
    /// Creates a new InterviewConfiguration with optional questions
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
    /// Gets an InterviewConfiguration by ID with all questions
    /// </summary>
    public async Task<InterviewConfiguration?> GetInterviewConfigurationByIdWithQuestions(Guid id)
    {
        return await InterviewConfigurationManager.GetConfigurationByIdWithQuestions(id).ConfigureAwait(false);
    }

    /// <summary>
    /// Searches for InterviewConfigurations
    /// </summary>
    public async Task<PaginatedResult<InterviewConfiguration>> SearchInterviewConfigurations(
        Guid? organizationId, 
        Guid? agentId, 
        string? name, 
        bool? isActive,
        string? sortBy, 
        int pageNumber, 
        int pageSize)
    {
        return await InterviewConfigurationManager.SearchConfigurations(organizationId, agentId, name, isActive, sortBy, pageNumber, pageSize).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates an InterviewConfiguration
    /// </summary>
    public async Task<InterviewConfiguration> UpdateInterviewConfiguration(InterviewConfiguration config)
    {
        return await InterviewConfigurationManager.UpdateConfiguration(config).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates an InterviewConfiguration and replaces all its questions
    /// </summary>
    public async Task<InterviewConfiguration> UpdateInterviewConfigurationWithQuestions(InterviewConfiguration config, List<InterviewConfigurationQuestion> questions)
    {
        return await InterviewConfigurationManager.UpdateConfigurationWithQuestions(config, questions).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes an InterviewConfiguration (soft delete)
    /// </summary>
    public async Task<bool> DeleteInterviewConfiguration(Guid id, string? deletedBy = null)
    {
        return await InterviewConfigurationManager.DeleteConfiguration(id, deletedBy).ConfigureAwait(false);
    }

    /// <summary>
    /// Adds a question to an existing configuration
    /// </summary>
    public async Task<InterviewConfigurationQuestion> AddInterviewConfigurationQuestion(InterviewConfigurationQuestion question)
    {
        return await InterviewConfigurationManager.AddQuestion(question).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates a question
    /// </summary>
    public async Task<InterviewConfigurationQuestion> UpdateInterviewConfigurationQuestion(InterviewConfigurationQuestion question)
    {
        return await InterviewConfigurationManager.UpdateQuestion(question).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes a question
    /// </summary>
    public async Task<bool> DeleteInterviewConfigurationQuestion(Guid questionId)
    {
        return await InterviewConfigurationManager.DeleteQuestion(questionId).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets all questions for a configuration
    /// </summary>
    public async Task<List<InterviewConfigurationQuestion>> GetInterviewConfigurationQuestions(Guid configurationId)
    {
        return await InterviewConfigurationManager.GetQuestionsByConfigurationId(configurationId).ConfigureAwait(false);
    }
}
