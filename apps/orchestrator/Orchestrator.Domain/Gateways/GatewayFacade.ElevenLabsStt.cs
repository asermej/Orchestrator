using System;
using System.Threading;
using System.Threading.Tasks;

namespace Orchestrator.Domain;

/// <summary>
/// Gateway facade partial class for ElevenLabs realtime speech-to-text integration
/// </summary>
internal sealed partial class GatewayFacade
{
    /// <summary>
    /// Creates a new ElevenLabs STT manager instance for a phone call session.
    /// Each call session should have its own manager since it holds a dedicated WebSocket.
    /// The caller is responsible for disposing the returned instance.
    /// </summary>
    public ElevenLabsSttManager CreateElevenLabsSttManager()
    {
        return new ElevenLabsSttManager(_serviceLocator);
    }
}
