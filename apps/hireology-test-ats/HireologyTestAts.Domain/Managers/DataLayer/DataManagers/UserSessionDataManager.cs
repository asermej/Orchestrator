using Dapper;
using Npgsql;

namespace HireologyTestAts.Domain;

internal sealed class UserSessionDataManager
{
    private readonly string _connectionString;

    public UserSessionDataManager(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<Guid?> GetSelectedOrganizationIdAsync(Guid userId)
    {
        const string sql = "SELECT selected_organization_id FROM user_sessions WHERE user_id = @UserId";
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QuerySingleOrDefaultAsync<Guid?>(new CommandDefinition(sql, new { UserId = userId }));
    }

    public async Task SetSelectedOrganizationIdAsync(Guid userId, Guid? organizationId)
    {
        const string sql = @"
            INSERT INTO user_sessions (user_id, selected_organization_id, updated_at)
            VALUES (@UserId, @OrganizationId, CURRENT_TIMESTAMP)
            ON CONFLICT (user_id) DO UPDATE SET selected_organization_id = @OrganizationId, updated_at = CURRENT_TIMESTAMP";
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.ExecuteAsync(new CommandDefinition(sql, new { UserId = userId, OrganizationId = organizationId }));
    }
}
