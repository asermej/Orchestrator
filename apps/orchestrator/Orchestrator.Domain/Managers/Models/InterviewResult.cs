using System.ComponentModel.DataAnnotations.Schema;

namespace Orchestrator.Domain;

/// <summary>
/// Represents an InterviewResult in the domain
/// </summary>
[Table("interview_results")]
public class InterviewResult : Entity
{
    [Column("interview_id")]
    public Guid InterviewId { get; set; }

    [Column("summary")]
    public string? Summary { get; set; }

    [Column("score")]
    public int? Score { get; set; }

    [Column("recommendation")]
    public string? Recommendation { get; set; }

    [Column("strengths")]
    public string? Strengths { get; set; }

    [Column("areas_for_improvement")]
    public string? AreasForImprovement { get; set; }

    [Column("full_transcript_url")]
    public string? FullTranscriptUrl { get; set; }

    [Column("webhook_sent_at")]
    public DateTime? WebhookSentAt { get; set; }

    [Column("webhook_response")]
    public string? WebhookResponse { get; set; }

    [Column("question_scores")]
    public string? QuestionScores { get; set; }
}

/// <summary>
/// Interview recommendation constants
/// </summary>
public static class InterviewRecommendation
{
    public const string StronglyRecommend = "strongly_recommend";
    public const string Recommend = "recommend";
    public const string Neutral = "neutral";
    public const string NotRecommended = "not_recommended";
}
