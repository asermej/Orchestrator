using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Npgsql;

namespace Orchestrator.Domain;

/// <summary>
/// Manages data access for VoiceCloneJob entities (IVC jobs).
/// </summary>
internal sealed class VoiceCloneJobDataManager
{
    private readonly string _dbConnectionString;

    public VoiceCloneJobDataManager(string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
        DapperConfiguration.ConfigureSnakeCaseMapping<VoiceCloneJob>();
    }

    public async Task<VoiceCloneJob?> GetById(Guid id)
    {
        const string sql = @"
            SELECT id, user_id, persona_id, sample_blob_url, sample_duration_seconds, status,
                   eleven_labs_voice_id, error_message, created_at, updated_at, style_lane
            FROM voice_clone_jobs
            WHERE id = @id";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<VoiceCloneJob>(sql, new { id });
    }

    public async Task<VoiceCloneJob> Add(VoiceCloneJob job)
    {
        if (job.Id == Guid.Empty)
        {
            job.Id = Guid.NewGuid();
        }

        const string sql = @"
            INSERT INTO voice_clone_jobs (id, user_id, persona_id, sample_blob_url, sample_duration_seconds, status, style_lane)
            VALUES (@Id, @UserId, @PersonaId, @SampleBlobUrl, @SampleDurationSeconds, @Status, @StyleLane)
            RETURNING id, user_id, persona_id, sample_blob_url, sample_duration_seconds, status,
                      eleven_labs_voice_id, error_message, created_at, updated_at, style_lane";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        var newItem = await connection.QueryFirstOrDefaultAsync<VoiceCloneJob>(sql, job);
        return newItem!;
    }

    public async Task Update(VoiceCloneJob job)
    {
        const string sql = @"
            UPDATE voice_clone_jobs
            SET status = @Status, eleven_labs_voice_id = @ElevenLabsVoiceId, error_message = @ErrorMessage,
                updated_at = CURRENT_TIMESTAMP
            WHERE id = @Id";

        using var connection = new NpgsqlConnection(_dbConnectionString);
        await connection.ExecuteAsync(sql, job);
    }

    /// <summary>
    /// Gets the most recent successful clone job by user within the last N hours (for rate limit).
    /// </summary>
    public async Task<VoiceCloneJob?> GetMostRecentSuccessByUserIdWithinHours(string userId, int hours = 24)
    {
        const string sql = @"
            SELECT id, user_id, persona_id, sample_blob_url, sample_duration_seconds, status,
                   eleven_labs_voice_id, error_message, created_at, updated_at, style_lane
            FROM voice_clone_jobs
            WHERE user_id = @userId AND status = 'Success'
              AND created_at >= (CURRENT_TIMESTAMP AT TIME ZONE 'UTC') - (INTERVAL '1 hour' * @hours)
            ORDER BY created_at DESC
            LIMIT 1";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.QueryFirstOrDefaultAsync<VoiceCloneJob>(sql, new { userId, hours });
    }

    /// <summary>
    /// Returns the count of successful clone jobs by user within the last N hours (for rate limit: 5/day).
    /// </summary>
    public async Task<int> GetSuccessfulCloneCountByUserIdWithinHoursAsync(string userId, int hours = 24)
    {
        const string sql = @"
            SELECT COUNT(*) FROM voice_clone_jobs
            WHERE user_id = @userId AND status = 'Success'
              AND created_at >= (CURRENT_TIMESTAMP AT TIME ZONE 'UTC') - (INTERVAL '1 hour' * @hours)";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        return await connection.ExecuteScalarAsync<int>(sql, new { userId, hours });
    }
}
