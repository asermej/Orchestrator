namespace Orchestrator.Domain;

internal sealed partial class DataFacade
{
    private InterviewGuideDataManager InterviewGuideDataManager => new(_dbConnectionString);

    public Task<InterviewGuide> AddInterviewGuide(InterviewGuide guide)
    {
        return InterviewGuideDataManager.Add(guide);
    }

    public async Task<InterviewGuide?> GetInterviewGuideById(Guid id)
    {
        return await InterviewGuideDataManager.GetById(id);
    }

    public async Task<InterviewGuide?> GetInterviewGuideByIdWithQuestions(Guid id)
    {
        return await InterviewGuideDataManager.GetByIdWithQuestions(id);
    }

    public Task<InterviewGuide> UpdateInterviewGuide(InterviewGuide guide)
    {
        return InterviewGuideDataManager.Update(guide);
    }

    public Task<bool> DeleteInterviewGuide(Guid id, string? deletedBy = null)
    {
        return InterviewGuideDataManager.Delete(id, deletedBy);
    }

    public Task<PaginatedResult<InterviewGuide>> SearchInterviewGuides(
        Guid? groupId,
        string? name,
        bool? isActive,
        string? sortBy,
        int pageNumber,
        int pageSize,
        IReadOnlyList<Guid>? organizationIds = null)
    {
        return InterviewGuideDataManager.Search(groupId, name, isActive, sortBy, pageNumber, pageSize, organizationIds);
    }

    // Question management
    public Task<InterviewGuideQuestion> AddInterviewGuideQuestion(InterviewGuideQuestion question)
    {
        return InterviewGuideDataManager.AddQuestion(question);
    }

    public Task<InterviewGuideQuestion> UpdateInterviewGuideQuestion(InterviewGuideQuestion question)
    {
        return InterviewGuideDataManager.UpdateQuestion(question);
    }

    public Task<bool> DeleteInterviewGuideQuestion(Guid questionId)
    {
        return InterviewGuideDataManager.DeleteQuestion(questionId);
    }

    public Task<List<InterviewGuideQuestion>> GetInterviewGuideQuestions(Guid guideId)
    {
        return InterviewGuideDataManager.GetQuestionsByGuideId(guideId);
    }

    public Task<InterviewGuideQuestion?> GetInterviewGuideQuestionById(Guid questionId)
    {
        return InterviewGuideDataManager.GetQuestionById(questionId);
    }

    public Task ReplaceInterviewGuideQuestions(Guid guideId, List<InterviewGuideQuestion> questions)
    {
        return InterviewGuideDataManager.ReplaceQuestions(guideId, questions);
    }
}
