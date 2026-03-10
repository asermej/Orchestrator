namespace Orchestrator.Domain;

/// <summary>
/// Transient result from the off-script response classifier.
/// Never persisted as-is — only classification-derived fields (CompetencySkipped, SkipReason) are stored.
/// </summary>
public class ResponseClassificationResult
{
    public string Classification { get; set; } = "on_topic";
    public bool RequiresResponse { get; set; }
    public string? ResponseText { get; set; }
    public bool ConsumesRedirect { get; set; }
    public bool AbandonCompetency { get; set; }
    public string? StoreNote { get; set; }
}

/// <summary>
/// Combined result from classifying and (if on_topic) evaluating a candidate response
/// in a single round-trip. Evaluation is null when classification is not on_topic.
/// </summary>
public class ClassifyAndEvaluateResult
{
    public ResponseClassificationResult Classification { get; set; } = new();
    public HolisticEvaluationResult? Evaluation { get; set; }
}
