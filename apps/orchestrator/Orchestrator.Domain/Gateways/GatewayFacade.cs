using System;

namespace Orchestrator.Domain;

/// <summary>
/// Gateway facade for external system integrations
/// Provides a unified interface for accessing external services while shielding the domain from integration details
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
            _openAIManager?.Dispose();
            _elevenLabsManager?.Dispose();
            _atsGatewayManager?.Dispose();
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

