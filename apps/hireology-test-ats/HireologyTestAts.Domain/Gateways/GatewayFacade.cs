namespace HireologyTestAts.Domain;

/// <summary>
/// Gateway facade providing business-focused methods for external API integrations.
/// Shields the domain from external API details.
/// </summary>
internal sealed partial class GatewayFacade : IDisposable
{
    private bool _disposed;
    private readonly ServiceLocatorBase _serviceLocator;

    public GatewayFacade(ServiceLocatorBase serviceLocator)
    {
        _serviceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
    }

    private void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _orchestratorManager?.Dispose();
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
