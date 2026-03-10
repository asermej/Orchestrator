namespace Orchestrator.Domain;

public static class AgentTone
{
    public const string Warm = "warm";
    public const string Neutral = "neutral";
    public const string Professional = "professional";

    public static readonly string[] AllValues = { Warm, Neutral, Professional };

    public static bool IsValid(string? value) =>
        string.IsNullOrEmpty(value) || AllValues.Contains(value);
}

public static class AgentPace
{
    public const string Conversational = "conversational";
    public const string Efficient = "efficient";

    public static readonly string[] AllValues = { Conversational, Efficient };

    public static bool IsValid(string? value) =>
        string.IsNullOrEmpty(value) || AllValues.Contains(value);
}

public static class AgentAcknowledgmentStyle
{
    public const string Brief = "brief";
    public const string Reflective = "reflective";

    public static readonly string[] AllValues = { Brief, Reflective };

    public static bool IsValid(string? value) =>
        string.IsNullOrEmpty(value) || AllValues.Contains(value);
}

/// <summary>
/// Assembles an agent's effective system prompt from structured behavioral fields.
/// The user never writes or sees the raw system prompt — it is derived at runtime.
/// </summary>
public static class AgentSystemPromptBuilder
{
    public static string Build(Agent agent)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(agent.Tone))
        {
            var toneInstruction = agent.Tone switch
            {
                AgentTone.Warm => "Use a warm, friendly, and approachable tone throughout the conversation.",
                AgentTone.Neutral => "Use a balanced, neutral tone throughout the conversation.",
                AgentTone.Professional => "Use a formal, professional tone throughout the conversation.",
                _ => ""
            };
            if (!string.IsNullOrEmpty(toneInstruction))
                parts.Add(toneInstruction);
        }

        if (!string.IsNullOrWhiteSpace(agent.Pace))
        {
            var paceInstruction = agent.Pace switch
            {
                AgentPace.Conversational => "Take time to acknowledge answers with natural transitions. Let the conversation breathe.",
                AgentPace.Efficient => "Keep acknowledgments brief and move through questions promptly.",
                _ => ""
            };
            if (!string.IsNullOrEmpty(paceInstruction))
                parts.Add(paceInstruction);
        }

        if (!string.IsNullOrWhiteSpace(agent.AcknowledgmentStyle))
        {
            var ackInstruction = agent.AcknowledgmentStyle switch
            {
                AgentAcknowledgmentStyle.Brief => "Use short acknowledgments before moving on (e.g. \"Got it, thank you.\").",
                AgentAcknowledgmentStyle.Reflective => "Mirror something the candidate said back before moving on (e.g. \"That's helpful context, thank you for sharing that.\").",
                _ => ""
            };
            if (!string.IsNullOrEmpty(ackInstruction))
                parts.Add(ackInstruction);
        }

        if (!string.IsNullOrWhiteSpace(agent.AdditionalInstructions))
        {
            parts.Add(agent.AdditionalInstructions.Trim());
        }

        return string.Join("\n\n", parts);
    }
}
