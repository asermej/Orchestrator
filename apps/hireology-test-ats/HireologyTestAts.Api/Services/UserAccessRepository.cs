using Dapper;
using Npgsql;

namespace HireologyTestAts.Api.Services;

public class UserAccessRepository
{
    private readonly string _connectionString;

    public UserAccessRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("HireologyTestAts")
            ?? throw new InvalidOperationException("ConnectionStrings:HireologyTestAts is required");
    }

    public async Task SetGroupAccessAsync(Guid userId, IReadOnlyList<Guid> groupIds, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.ExecuteAsync(new CommandDefinition("DELETE FROM user_group_access WHERE user_id = @UserId", new { UserId = userId }, cancellationToken: ct));
        foreach (var gid in groupIds.Distinct())
        {
            await conn.ExecuteAsync(new CommandDefinition(
                "INSERT INTO user_group_access (user_id, group_id) VALUES (@UserId, @GroupId)",
                new { UserId = userId, GroupId = gid }, cancellationToken: ct));
        }
    }

    public async Task SetOrganizationAccessAsync(Guid userId, IReadOnlyList<Guid> organizationIds, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.ExecuteAsync(new CommandDefinition("DELETE FROM user_organization_access WHERE user_id = @UserId", new { UserId = userId }, cancellationToken: ct));
        foreach (var oid in organizationIds.Distinct())
        {
            await conn.ExecuteAsync(new CommandDefinition(
                "INSERT INTO user_organization_access (user_id, organization_id) VALUES (@UserId, @OrganizationId)",
                new { UserId = userId, OrganizationId = oid }, cancellationToken: ct));
        }
    }

    public async Task<IReadOnlyList<Guid>> GetGroupIdsAsync(Guid userId, CancellationToken ct = default)
    {
        const string sql = "SELECT group_id FROM user_group_access WHERE user_id = @UserId";
        await using var conn = new NpgsqlConnection(_connectionString);
        var ids = await conn.QueryAsync<Guid>(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
        return ids.ToList();
    }

    public async Task<IReadOnlyList<Guid>> GetOrganizationIdsAsync(Guid userId, CancellationToken ct = default)
    {
        const string sql = "SELECT organization_id FROM user_organization_access WHERE user_id = @UserId";
        await using var conn = new NpgsqlConnection(_connectionString);
        var ids = await conn.QueryAsync<Guid>(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
        return ids.ToList();
    }
}
