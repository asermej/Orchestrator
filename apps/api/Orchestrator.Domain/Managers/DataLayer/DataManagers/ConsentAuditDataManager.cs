using System;
using System.Threading.Tasks;
using Dapper;
using Npgsql;

namespace Orchestrator.Domain;

/// <summary>
/// Manages data access for ConsentAudit entities (voice cloning consent).
/// </summary>
internal sealed class ConsentAuditDataManager
{
    private readonly string _dbConnectionString;

    public ConsentAuditDataManager(string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
        DapperConfiguration.ConfigureSnakeCaseMapping<ConsentAudit>();
    }

    public async Task<ConsentAudit?> GetById(Guid id)
    {
        const string sql = @"
            SELECT id, user_id, persona_id, consent_text_version, attested, created_at
            FROM consent_audit
            WHERE id = @id";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<ConsentAudit>(sql, new { id });
    }

    public async Task<ConsentAudit> Add(ConsentAudit consentAudit)
    {
        if (consentAudit.Id == Guid.Empty)
        {
            consentAudit.Id = Guid.NewGuid();
        }

        const string sql = @"
            INSERT INTO consent_audit (id, user_id, persona_id, consent_text_version, attested)
            VALUES (@Id, @UserId, @PersonaId, @ConsentTextVersion, @Attested)
            RETURNING id, user_id, persona_id, consent_text_version, attested, created_at";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var newItem = await connection.QueryFirstOrDefaultAsync<ConsentAudit>(sql, consentAudit);
        return newItem!;
    }
}
