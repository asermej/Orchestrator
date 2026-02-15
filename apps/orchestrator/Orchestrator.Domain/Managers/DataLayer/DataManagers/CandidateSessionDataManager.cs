using Dapper;
using Npgsql;

namespace Orchestrator.Domain;

/// <summary>
/// Manages data access for CandidateSession entities
/// </summary>
internal sealed class CandidateSessionDataManager
{
    private readonly string _dbConnectionString;

    public CandidateSessionDataManager(string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
        DapperConfiguration.ConfigureSnakeCaseMapping<CandidateSession>();
    }

    public async Task<CandidateSession?> GetById(Guid id)
    {
        const string sql = @"
            SELECT id, invite_id, interview_id, jti, is_active, ip_address, user_agent,
                   started_at, last_activity_at, expires_at, created_at, updated_at, is_deleted, deleted_at, deleted_by
            FROM candidate_sessions
            WHERE id = @id AND is_deleted = false";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<CandidateSession>(sql, new { id });
    }

    public async Task<CandidateSession?> GetByJti(string jti)
    {
        const string sql = @"
            SELECT id, invite_id, interview_id, jti, is_active, ip_address, user_agent,
                   started_at, last_activity_at, expires_at, created_at, updated_at, is_deleted, deleted_at, deleted_by
            FROM candidate_sessions
            WHERE jti = @Jti AND is_deleted = false";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<CandidateSession>(sql, new { Jti = jti });
    }

    public async Task<CandidateSession> Add(CandidateSession session)
    {
        if (session.Id == Guid.Empty)
        {
            session.Id = Guid.NewGuid();
        }

        const string sql = @"
            INSERT INTO candidate_sessions (id, invite_id, interview_id, jti, is_active, ip_address, user_agent, started_at, expires_at)
            VALUES (@Id, @InviteId, @InterviewId, @Jti, @IsActive, @IpAddress, @UserAgent, @StartedAt, @ExpiresAt)
            RETURNING id, invite_id, interview_id, jti, is_active, ip_address, user_agent,
                      started_at, last_activity_at, expires_at, created_at, updated_at, is_deleted, deleted_at, deleted_by";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var newItem = await connection.QueryFirstOrDefaultAsync<CandidateSession>(sql, session);
        return newItem!;
    }

    public async Task<CandidateSession> Update(CandidateSession session)
    {
        const string sql = @"
            UPDATE candidate_sessions
            SET
                is_active = @IsActive,
                last_activity_at = @LastActivityAt,
                updated_at = CURRENT_TIMESTAMP
            WHERE id = @Id AND is_deleted = false
            RETURNING id, invite_id, interview_id, jti, is_active, ip_address, user_agent,
                      started_at, last_activity_at, expires_at, created_at, updated_at, is_deleted, deleted_at, deleted_by";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var updatedItem = await connection.QueryFirstOrDefaultAsync<CandidateSession>(sql, session);
        if (updatedItem == null)
        {
            throw new CandidateSessionNotFoundException("Candidate session not found or already deleted.");
        }
        return updatedItem;
    }

    /// <summary>
    /// Deactivates all active sessions for a given invite (used when a new session is created)
    /// </summary>
    public async Task DeactivatePreviousSessions(Guid inviteId)
    {
        const string sql = @"
            UPDATE candidate_sessions
            SET is_active = false, updated_at = CURRENT_TIMESTAMP
            WHERE invite_id = @InviteId AND is_active = true AND is_deleted = false";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        await connection.ExecuteAsync(sql, new { InviteId = inviteId });
    }
}
