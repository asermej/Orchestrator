using Dapper;
using Npgsql;

namespace Orchestrator.Domain;

/// <summary>
/// Manages data access for InterviewAuditLog entities (append-only)
/// </summary>
internal sealed class InterviewAuditLogDataManager
{
    private readonly string _dbConnectionString;

    public InterviewAuditLogDataManager(string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
        DapperConfiguration.ConfigureSnakeCaseMapping<InterviewAuditLog>();
    }

    public async Task<InterviewAuditLog> Add(InterviewAuditLog auditLog)
    {
        if (auditLog.Id == Guid.Empty)
        {
            auditLog.Id = Guid.NewGuid();
        }

        const string sql = @"
            INSERT INTO interview_audit_logs (id, interview_id, invite_id, session_id, event_type, event_data, ip_address, user_agent)
            VALUES (@Id, @InterviewId, @InviteId, @SessionId, @EventType, @EventData::jsonb, @IpAddress, @UserAgent)
            RETURNING id, interview_id, invite_id, session_id, event_type, event_data, ip_address, user_agent, created_at";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var newItem = await connection.QueryFirstOrDefaultAsync<InterviewAuditLog>(sql, auditLog);
        return newItem!;
    }

    public async Task<IEnumerable<InterviewAuditLog>> GetByInterviewId(Guid interviewId)
    {
        const string sql = @"
            SELECT id, interview_id, invite_id, session_id, event_type, event_data, ip_address, user_agent, created_at
            FROM interview_audit_logs
            WHERE interview_id = @InterviewId
            ORDER BY created_at ASC";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryAsync<InterviewAuditLog>(sql, new { InterviewId = interviewId });
    }
}
