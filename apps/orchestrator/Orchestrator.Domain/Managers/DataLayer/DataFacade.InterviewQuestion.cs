namespace Orchestrator.Domain;

internal sealed partial class DataFacade
{
    private InterviewQuestionDataManager InterviewQuestionDataManager => new(_dbConnectionString);

    public async Task<InterviewQuestion?> GetInterviewQuestionById(Guid id)
    {
        return await InterviewQuestionDataManager.GetById(id);
    }
}
