namespace Orchestrator.Domain;

/// <summary>
/// A single turn in a conversation (role + content) for AI context only.
/// Not persisted; used by the gateway and managers for chat completion history.
/// </summary>
public class ConversationTurn
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
