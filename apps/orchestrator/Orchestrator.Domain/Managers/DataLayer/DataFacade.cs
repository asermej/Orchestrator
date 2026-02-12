namespace Orchestrator.Domain;

internal sealed partial class DataFacade
{
    private readonly string _dbConnectionString;

    public DataFacade(string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
    }
}
