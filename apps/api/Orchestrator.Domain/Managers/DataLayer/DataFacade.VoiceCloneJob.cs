using System;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

internal sealed partial class DataFacade
{
    private VoiceCloneJobDataManager VoiceCloneJobDataManager => new(_dbConnectionString);

    public Task<VoiceCloneJob> AddVoiceCloneJob(VoiceCloneJob job)
    {
        return VoiceCloneJobDataManager.Add(job);
    }

    public Task UpdateVoiceCloneJob(VoiceCloneJob job)
    {
        return VoiceCloneJobDataManager.Update(job);
    }

    public async Task<VoiceCloneJob?> GetVoiceCloneJobById(Guid id)
    {
        return await VoiceCloneJobDataManager.GetById(id);
    }

    public async Task<VoiceCloneJob?> GetRecentCloneJobSuccessByUserIdWithinHours(string userId, int hours = 24)
    {
        return await VoiceCloneJobDataManager.GetMostRecentSuccessByUserIdWithinHours(userId, hours);
    }

    public async Task<int> GetSuccessfulCloneCountByUserIdWithinHoursAsync(string userId, int hours = 24)
    {
        return await VoiceCloneJobDataManager.GetSuccessfulCloneCountByUserIdWithinHoursAsync(userId, hours);
    }
}
