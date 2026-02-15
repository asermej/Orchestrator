namespace Orchestrator.Domain;

internal sealed partial class DataFacade
{
    private InterviewInviteDataManager InterviewInviteDataManager => new(_dbConnectionString);

    public Task<InterviewInvite> AddInterviewInvite(InterviewInvite invite)
    {
        return InterviewInviteDataManager.Add(invite);
    }

    public async Task<InterviewInvite?> GetInterviewInviteById(Guid id)
    {
        return await InterviewInviteDataManager.GetById(id);
    }

    public async Task<InterviewInvite?> GetInterviewInviteByShortCode(string shortCode)
    {
        return await InterviewInviteDataManager.GetByShortCode(shortCode);
    }

    public async Task<InterviewInvite?> GetInterviewInviteByInterviewId(Guid interviewId)
    {
        return await InterviewInviteDataManager.GetByInterviewId(interviewId);
    }

    public Task<InterviewInvite> UpdateInterviewInvite(InterviewInvite invite)
    {
        return InterviewInviteDataManager.Update(invite);
    }

    public Task<bool> DeleteInterviewInvite(Guid id)
    {
        return InterviewInviteDataManager.Delete(id);
    }
}
