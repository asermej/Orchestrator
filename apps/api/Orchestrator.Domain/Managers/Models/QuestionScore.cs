namespace Orchestrator.Domain;

/// <summary>
/// Represents the score for a single interview question
/// </summary>
public class QuestionScore
{
    /// <summary>
    /// The index of the question in the interview
    /// </summary>
    public int QuestionIndex { get; set; }

    /// <summary>
    /// The question text
    /// </summary>
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// The score achieved (0-10)
    /// </summary>
    public decimal Score { get; set; }

    /// <summary>
    /// The maximum possible score
    /// </summary>
    public decimal MaxScore { get; set; } = 10;

    /// <summary>
    /// The weight of this question in overall scoring
    /// </summary>
    public decimal Weight { get; set; } = 1;

    /// <summary>
    /// Feedback or explanation for the score
    /// </summary>
    public string Feedback { get; set; } = string.Empty;
}
