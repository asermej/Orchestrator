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

    [Column("overall_score_display")]
    public int? OverallScoreDisplay { get; set; }

    [Column("recommendation_tier")]
    public string? RecommendationTier { get; set; }
}

/// <summary>
/// Recommendation tier constants derived from overall_score_display and thresholds.
/// </summary>
public static class RecommendationTiers
{
    public const string StronglyRecommend = "Strongly Recommend";
    public const string Recommend = "Recommend";
    public const string Consider = "Consider";
    public const string DoNotRecommend = "Do Not Recommend";
}

/// <summary>
/// Represents the single-row global recommendation threshold settings.
/// </summary>
[Table("recommendation_threshold_defaults")]
public class RecommendationThresholdDefaults
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("strongly_recommend_min")]
    public int StronglyRecommendMin { get; set; } = 80;

    [Column("recommend_min")]
    public int RecommendMin { get; set; } = 65;

    [Column("consider_min")]
    public int ConsiderMin { get; set; } = 50;

    [Column("do_not_recommend_min")]
    public int DoNotRecommendMin { get; set; } = 0;
}
