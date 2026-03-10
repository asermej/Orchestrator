namespace Orchestrator.Domain;

/// <summary>
/// Gateway facade partial class for ElevenLabs WebSocket-based TTS (ulaw phone audio)
/// </summary>
internal sealed partial class GatewayFacade
{
    /// <summary>
    /// Creates a new ElevenLabs TTS WebSocket manager instance for a phone call session.
    /// Each synthesis request should use its own manager since it holds a dedicated WebSocket.
    /// The caller is responsible for disposing the returned instance.
    /// </summary>
    public ElevenLabsTtsWebSocketManager CreateElevenLabsTtsWebSocketManager()
    {
        return new ElevenLabsTtsWebSocketManager(_serviceLocator);
    }
}
