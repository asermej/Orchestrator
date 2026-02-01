namespace Orchestrator.Domain;

/// <summary>
/// Exception thrown when ElevenLabs TTS is disabled in configuration
/// </summary>
public class ElevenLabsDisabledException : Exception
{
    public ElevenLabsDisabledException() : base("ElevenLabs TTS is disabled in configuration")
    {
    }

    public ElevenLabsDisabledException(string message) : base(message)
    {
    }
}
