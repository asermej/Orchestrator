namespace Orchestrator.Domain;

/// <summary>
/// Represents follow-up question budgets for an interview
/// </summary>
public class FollowUpBudgets
{
    public int MaxFollowUpsPerQuestion { get; set; } = 2;
    public int MaxFollowUpsPerInterview { get; set; } = 4;
    public int FollowUpsAskedForQuestion { get; set; } = 0;
    public int TotalFollowUpsAsked { get; set; } = 0;
}
