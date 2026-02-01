using System;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

internal sealed partial class DataFacade
{
    private ConsentAuditDataManager ConsentAuditDataManager => new(_dbConnectionString);

    public Task<ConsentAudit> AddConsentAudit(ConsentAudit consentAudit)
    {
        return ConsentAuditDataManager.Add(consentAudit);
    }

    public async Task<ConsentAudit?> GetConsentAuditById(Guid id)
    {
        return await ConsentAuditDataManager.GetById(id);
    }
}
