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
    /// Creates a new InterviewConfiguration with optional questions
    /// </summary>
    public async Task<InterviewConfiguration> CreateConfiguration(InterviewConfiguration config)
    {
        InterviewConfigurationValidator.Validate(config);
        
        // Create the configuration
        var createdConfig = await DataFacade.AddInterviewConfiguration(config).ConfigureAwait(false);

        // Add questions if provided
        if (config.Questions != null && config.Questions.Count > 0)
        {
            for (int i = 0; i < config.Questions.Count; i++)
            {
                var question = config.Questions[i];
                question.InterviewConfigurationId = createdConfig.Id;
                question.DisplayOrder = i;
                await DataFacade.AddInterviewConfigurationQuestion(question).ConfigureAwait(false);
            }
            
            // Reload with questions
            createdConfig = await DataFacade.GetInterviewConfigurationByIdWithQuestions(createdConfig.Id).ConfigureAwait(false);
        }

        return createdConfig!;
    }

    /// <summary>
    /// Gets an InterviewConfiguration by ID
    /// </summary>
    public async Task<InterviewConfiguration?> GetConfigurationById(Guid id)
    {
        return await DataFacade.GetInterviewConfigurationById(id).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets an InterviewConfiguration by ID with all questions
    /// </summary>
    public async Task<InterviewConfiguration?> GetConfigurationByIdWithQuestions(Guid id)
    {
        return await DataFacade.GetInterviewConfigurationByIdWithQuestions(id).ConfigureAwait(false);
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
    /// Updates an InterviewConfiguration and replaces all its questions
    /// </summary>
    public async Task<InterviewConfiguration> UpdateConfigurationWithQuestions(InterviewConfiguration config, List<InterviewConfigurationQuestion> questions)
    {
        InterviewConfigurationValidator.Validate(config);
        
        // Validate all questions
        foreach (var question in questions)
        {
            question.InterviewConfigurationId = config.Id;
            InterviewConfigurationValidator.ValidateQuestion(question);
        }

        // Update the configuration
        var updatedConfig = await DataFacade.UpdateInterviewConfiguration(config).ConfigureAwait(false);

        // Replace all questions
        await DataFacade.ReplaceInterviewConfigurationQuestions(config.Id, questions).ConfigureAwait(false);

        // Reload with questions
        return (await DataFacade.GetInterviewConfigurationByIdWithQuestions(config.Id).ConfigureAwait(false))!;
    }

    /// <summary>
    /// Deletes an InterviewConfiguration (soft delete)
    /// </summary>
    public async Task<bool> DeleteConfiguration(Guid id, string? deletedBy = null)
    {
        return await DataFacade.DeleteInterviewConfiguration(id, deletedBy).ConfigureAwait(false);
    }

    /// <summary>
    /// Adds a question to an existing configuration
    /// </summary>
    public async Task<InterviewConfigurationQuestion> AddQuestion(InterviewConfigurationQuestion question)
    {
        InterviewConfigurationValidator.ValidateQuestion(question);
        return await DataFacade.AddInterviewConfigurationQuestion(question).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates a question
    /// </summary>
    public async Task<InterviewConfigurationQuestion> UpdateQuestion(InterviewConfigurationQuestion question)
    {
        InterviewConfigurationValidator.ValidateQuestion(question);
        return await DataFacade.UpdateInterviewConfigurationQuestion(question).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes a question
    /// </summary>
    public async Task<bool> DeleteQuestion(Guid questionId)
    {
        return await DataFacade.DeleteInterviewConfigurationQuestion(questionId).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets all questions for a configuration
    /// </summary>
    public async Task<List<InterviewConfigurationQuestion>> GetQuestionsByConfigurationId(Guid configurationId)
    {
        return await DataFacade.GetInterviewConfigurationQuestions(configurationId).ConfigureAwait(false);
    }

    public void Dispose()
    {
        // DataFacade doesn't implement IDisposable, so no disposal needed
    }
}
