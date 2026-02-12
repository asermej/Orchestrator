namespace Orchestrator.Domain;

internal sealed partial class DataFacade
{
    private InterviewConfigurationDataManager InterviewConfigurationDataManager => new(_dbConnectionString);

    public Task<InterviewConfiguration> AddInterviewConfiguration(InterviewConfiguration config)
    {
        return InterviewConfigurationDataManager.Add(config);
    }

    public async Task<InterviewConfiguration?> GetInterviewConfigurationById(Guid id)
    {
        return await InterviewConfigurationDataManager.GetById(id);
    }

    public async Task<InterviewConfiguration?> GetInterviewConfigurationByIdWithQuestions(Guid id)
    {
        return await InterviewConfigurationDataManager.GetByIdWithQuestions(id);
    }
    
    public Task<InterviewConfiguration> UpdateInterviewConfiguration(InterviewConfiguration config)
    {
        return InterviewConfigurationDataManager.Update(config);
    }

    public Task<bool> DeleteInterviewConfiguration(Guid id, string? deletedBy = null)
    {
        return InterviewConfigurationDataManager.Delete(id, deletedBy);
    }

    public Task<PaginatedResult<InterviewConfiguration>> SearchInterviewConfigurations(
        Guid? organizationId, 
        Guid? agentId, 
        string? name, 
        bool? isActive,
        string? sortBy, 
        int pageNumber, 
        int pageSize)
    {
        return InterviewConfigurationDataManager.Search(organizationId, agentId, name, isActive, sortBy, pageNumber, pageSize);
    }

    // Question management
    public Task<InterviewConfigurationQuestion> AddInterviewConfigurationQuestion(InterviewConfigurationQuestion question)
    {
        return InterviewConfigurationDataManager.AddQuestion(question);
    }

    public Task<InterviewConfigurationQuestion> UpdateInterviewConfigurationQuestion(InterviewConfigurationQuestion question)
    {
        return InterviewConfigurationDataManager.UpdateQuestion(question);
    }

    public Task<bool> DeleteInterviewConfigurationQuestion(Guid questionId)
    {
        return InterviewConfigurationDataManager.DeleteQuestion(questionId);
    }

    public Task<List<InterviewConfigurationQuestion>> GetInterviewConfigurationQuestions(Guid configurationId)
    {
        return InterviewConfigurationDataManager.GetQuestionsByConfigurationId(configurationId);
    }

    public Task<InterviewConfigurationQuestion?> GetInterviewConfigurationQuestionById(Guid questionId)
    {
        return InterviewConfigurationDataManager.GetQuestionById(questionId);
    }

    public Task ReplaceInterviewConfigurationQuestions(Guid configurationId, List<InterviewConfigurationQuestion> questions)
    {
        return InterviewConfigurationDataManager.ReplaceQuestions(configurationId, questions);
    }
}
