namespace Orchestrator.Domain;

/// <summary>
/// Webhook event types sent to ATS integrations
/// </summary>
public static class WebhookEventTypes
{
    public const string InterviewCompleted = "interview.completed";
    public const string InterviewStarted = "interview.started";
    public const string InterviewCancelled = "interview.cancelled";
}
