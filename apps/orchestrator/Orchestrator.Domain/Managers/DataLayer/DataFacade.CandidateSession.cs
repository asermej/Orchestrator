namespace Orchestrator.Domain;

internal sealed partial class DataFacade
{
    private CandidateSessionDataManager CandidateSessionDataManager => new(_dbConnectionString);

    public Task<CandidateSession> AddCandidateSession(CandidateSession session)
    {
        return CandidateSessionDataManager.Add(session);
    }

    public async Task<CandidateSession?> GetCandidateSessionById(Guid id)
    {
        return await CandidateSessionDataManager.GetById(id);
    }

    public async Task<CandidateSession?> GetCandidateSessionByJti(string jti)
    {
        return await CandidateSessionDataManager.GetByJti(jti);
    }

    public Task<CandidateSession> UpdateCandidateSession(CandidateSession session)
    {
        return CandidateSessionDataManager.Update(session);
    }

    public Task DeactivatePreviousCandidateSessions(Guid inviteId)
    {
        return CandidateSessionDataManager.DeactivatePreviousSessions(inviteId);
    }
}
