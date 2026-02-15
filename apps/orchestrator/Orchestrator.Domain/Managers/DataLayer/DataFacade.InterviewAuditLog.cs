namespace Orchestrator.Domain;

internal sealed partial class DataFacade
{
    private InterviewAuditLogDataManager InterviewAuditLogDataManager => new(_dbConnectionString);

    public Task<InterviewAuditLog> AddInterviewAuditLog(InterviewAuditLog auditLog)
    {
        return InterviewAuditLogDataManager.Add(auditLog);
    }

    public async Task<IEnumerable<InterviewAuditLog>> GetInterviewAuditLogsByInterviewId(Guid interviewId)
    {
        return await InterviewAuditLogDataManager.GetByInterviewId(interviewId);
    }
}
