using Dapper;
using Npgsql;

namespace Orchestrator.Domain;

internal sealed class RecommendationThresholdDataManager
{
    private readonly string _dbConnectionString;

    public RecommendationThresholdDataManager(string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
        DapperConfiguration.ConfigureSnakeCaseMapping<RecommendationThresholdDefaults>();
    }

    public async Task<RecommendationThresholdDefaults> Get()
    {
        const string sql = @"
            SELECT id, strongly_recommend_min, recommend_min, consider_min, do_not_recommend_min, created_at, updated_at
            FROM recommendation_threshold_defaults
            LIMIT 1";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        var result = await connection.QueryFirstOrDefaultAsync<RecommendationThresholdDefaults>(sql);
        return result ?? new RecommendationThresholdDefaults
        {
            StronglyRecommendMin = 80,
            RecommendMin = 65,
            ConsiderMin = 50,
            DoNotRecommendMin = 0
        };
    }

    public async Task<RecommendationThresholdDefaults> Update(RecommendationThresholdDefaults thresholds)
    {
        const string sql = @"
            UPDATE recommendation_threshold_defaults
            SET strongly_recommend_min = @StronglyRecommendMin,
                recommend_min = @RecommendMin,
                consider_min = @ConsiderMin,
                do_not_recommend_min = @DoNotRecommendMin,
                updated_at = CURRENT_TIMESTAMP
            WHERE id = @Id
            RETURNING id, strongly_recommend_min, recommend_min, consider_min, do_not_recommend_min, created_at, updated_at";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        var result = await connection.QueryFirstOrDefaultAsync<RecommendationThresholdDefaults>(sql, thresholds);
        return result ?? thresholds;
    }
}
