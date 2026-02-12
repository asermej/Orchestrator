namespace Orchestrator.Domain;

/// <summary>
/// Result of warming up interview question audio cache.
/// </summary>
public class InterviewAudioWarmupResult
{
    /// <summary>
    /// The interview ID that was warmed up.
    /// </summary>
    public Guid InterviewId { get; set; }
    
    /// <summary>
    /// Total number of questions to warm up.
    /// </summary>
    public int TotalQuestions { get; set; }
    
    /// <summary>
    /// Number of questions successfully cached.
    /// </summary>
    public int CachedQuestions { get; set; }
    
    /// <summary>
    /// Number of questions that were already in cache.
    /// </summary>
    public int AlreadyCached { get; set; }
    
    /// <summary>
    /// Number of questions that failed to generate.
    /// </summary>
    public int FailedQuestions { get; set; }
    
    /// <summary>
    /// Whether all questions were successfully warmed up.
    /// </summary>
    public bool IsComplete => CachedQuestions + AlreadyCached == TotalQuestions;
}
