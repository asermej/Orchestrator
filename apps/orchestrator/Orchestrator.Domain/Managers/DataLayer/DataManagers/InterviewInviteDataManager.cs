using Dapper;
using Npgsql;

namespace Orchestrator.Domain;

/// <summary>
/// Manages data access for InterviewInvite entities
/// </summary>
internal sealed class InterviewInviteDataManager
{
    private readonly string _dbConnectionString;

    public InterviewInviteDataManager(string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
        DapperConfiguration.ConfigureSnakeCaseMapping<InterviewInvite>();
    }

    public async Task<InterviewInvite?> GetById(Guid id)
    {
        const string sql = @"
            SELECT id, interview_id, group_id, short_code, status, expires_at, max_uses, use_count,
                   revoked_at, revoked_by, created_at, updated_at, created_by, updated_by, is_deleted, deleted_at, deleted_by
            FROM interview_invites
            WHERE id = @id AND is_deleted = false";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<InterviewInvite>(sql, new { id });
    }

    public async Task<InterviewInvite?> GetByShortCode(string shortCode)
    {
        const string sql = @"
            SELECT id, interview_id, group_id, short_code, status, expires_at, max_uses, use_count,
                   revoked_at, revoked_by, created_at, updated_at, created_by, updated_by, is_deleted, deleted_at, deleted_by
            FROM interview_invites
            WHERE short_code = @ShortCode AND is_deleted = false";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<InterviewInvite>(sql, new { ShortCode = shortCode });
    }

    public async Task<InterviewInvite?> GetByInterviewId(Guid interviewId)
    {
        const string sql = @"
            SELECT id, interview_id, group_id, short_code, status, expires_at, max_uses, use_count,
                   revoked_at, revoked_by, created_at, updated_at, created_by, updated_by, is_deleted, deleted_at, deleted_by
            FROM interview_invites
            WHERE interview_id = @InterviewId AND is_deleted = false";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<InterviewInvite>(sql, new { InterviewId = interviewId });
    }

    public async Task<InterviewInvite> Add(InterviewInvite invite)
    {
        if (invite.Id == Guid.Empty)
        {
            invite.Id = Guid.NewGuid();
        }

        const string sql = @"
            INSERT INTO interview_invites (id, interview_id, group_id, short_code, status, expires_at, max_uses, use_count, created_by)
            VALUES (@Id, @InterviewId, @GroupId, @ShortCode, @Status, @ExpiresAt, @MaxUses, @UseCount, @CreatedBy)
            RETURNING id, interview_id, group_id, short_code, status, expires_at, max_uses, use_count,
                      revoked_at, revoked_by, created_at, updated_at, created_by, updated_by, is_deleted, deleted_at, deleted_by";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var newItem = await connection.QueryFirstOrDefaultAsync<InterviewInvite>(sql, invite);
        return newItem!;
    }

    public async Task<InterviewInvite> Update(InterviewInvite invite)
    {
        const string sql = @"
            UPDATE interview_invites
            SET
                status = @Status,
                use_count = @UseCount,
                revoked_at = @RevokedAt,
                revoked_by = @RevokedBy,
                updated_at = CURRENT_TIMESTAMP,
                updated_by = @UpdatedBy
            WHERE id = @Id AND is_deleted = false
            RETURNING id, interview_id, group_id, short_code, status, expires_at, max_uses, use_count,
                      revoked_at, revoked_by, created_at, updated_at, created_by, updated_by, is_deleted, deleted_at, deleted_by";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var updatedItem = await connection.QueryFirstOrDefaultAsync<InterviewInvite>(sql, invite);
        if (updatedItem == null)
        {
            throw new InviteNotFoundException("Interview invite not found or already deleted.");
        }
        return updatedItem;
    }

    public async Task<bool> Delete(Guid id)
    {
        const string sql = @"
            UPDATE interview_invites
            SET is_deleted = true, deleted_at = CURRENT_TIMESTAMP
            WHERE id = @id AND is_deleted = false";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var rowsAffected = await connection.ExecuteAsync(sql, new { id });
        return rowsAffected > 0;
    }
}
