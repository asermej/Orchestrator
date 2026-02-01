using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Npgsql;

namespace Orchestrator.Domain;

/// <summary>
/// Manages data access for StockVoice entities (curated stock voices).
/// </summary>
internal sealed class StockVoiceDataManager
{
    private readonly string _dbConnectionString;

    public StockVoiceDataManager(string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
        DapperConfiguration.ConfigureSnakeCaseMapping<StockVoice>();
    }

    public async Task<IReadOnlyList<StockVoice>> GetAllOrderedBySortOrderAsync()
    {
        const string sql = @"
            SELECT id, voice_id, name, description, tags, preview_text, sort_order
            FROM stock_voices
            ORDER BY sort_order ASC, name ASC";
        using var connection = new NpgsqlConnection(_dbConnectionString);
        var list = await connection.QueryAsync<StockVoice>(sql);
        return list.AsList();
    }
}
