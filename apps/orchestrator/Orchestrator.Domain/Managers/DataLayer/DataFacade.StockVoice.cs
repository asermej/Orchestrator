using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

internal sealed partial class DataFacade
{
    private StockVoiceDataManager StockVoiceDataManager => new(_dbConnectionString);

    public async Task<IReadOnlyList<StockVoice>> GetStockVoicesAsync()
    {
        return await StockVoiceDataManager.GetAllOrderedBySortOrderAsync();
    }
}
