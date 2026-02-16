namespace Orchestrator.Domain;

internal sealed partial class DataFacade
{
    private FollowUpTemplateDataManager? _followUpTemplateDataManager;
    private FollowUpTemplateDataManager FollowUpTemplateDataManager => _followUpTemplateDataManager ??= new FollowUpTemplateDataManager(_dbConnectionString);

    private FollowUpSelectionLogDataManager? _followUpSelectionLogDataManager;
    private FollowUpSelectionLogDataManager FollowUpSelectionLogDataManager => _followUpSelectionLogDataManager ??= new FollowUpSelectionLogDataManager(_dbConnectionString);

    public async Task<FollowUpTemplate?> GetFollowUpTemplateById(Guid id)
    {
        return await FollowUpTemplateDataManager.GetById(id).ConfigureAwait(false);
    }

    public async Task<IEnumerable<FollowUpTemplate>> GetFollowUpTemplatesByInterviewQuestionId(Guid interviewQuestionId)
    {
        return await FollowUpTemplateDataManager.GetByInterviewQuestionId(interviewQuestionId).ConfigureAwait(false);
    }

    public async Task<IEnumerable<FollowUpTemplate>> GetApprovedFollowUpTemplatesByInterviewQuestionId(Guid interviewQuestionId)
    {
        return await FollowUpTemplateDataManager.GetApprovedByInterviewQuestionId(interviewQuestionId).ConfigureAwait(false);
    }

    public async Task<FollowUpTemplate> AddFollowUpTemplate(FollowUpTemplate template)
    {
        return await FollowUpTemplateDataManager.Add(template).ConfigureAwait(false);
    }

    public async Task<FollowUpTemplate> UpdateFollowUpTemplate(FollowUpTemplate template)
    {
        return await FollowUpTemplateDataManager.Update(template).ConfigureAwait(false);
    }

    public async Task BulkApproveFollowUpTemplates(List<Guid> templateIds)
    {
        await FollowUpTemplateDataManager.BulkApprove(templateIds).ConfigureAwait(false);
    }

    public async Task<bool> DeleteFollowUpTemplate(Guid id)
    {
        return await FollowUpTemplateDataManager.Delete(id).ConfigureAwait(false);
    }

    public async Task<FollowUpSelectionLog> AddFollowUpSelectionLog(FollowUpSelectionLog log)
    {
        return await FollowUpSelectionLogDataManager.Add(log).ConfigureAwait(false);
    }

    public async Task<IEnumerable<FollowUpSelectionLog>> GetFollowUpSelectionLogsByInterviewId(Guid interviewId)
    {
        return await FollowUpSelectionLogDataManager.GetByInterviewId(interviewId).ConfigureAwait(false);
    }
}
