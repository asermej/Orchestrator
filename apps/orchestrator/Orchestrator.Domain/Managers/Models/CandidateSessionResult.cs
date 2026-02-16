namespace Orchestrator.Domain;

/// <summary>
/// Result returned when a candidate redeems an invite and gets a session token.
/// Contains the JWT token and the interview detail needed to render the UI.
/// </summary>
public class CandidateSessionResult
{
    /// <summary>
    /// The signed JWT token for the candidate session
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// The interview associated with this session
    /// </summary>
    public Interview Interview { get; set; } = null!;

    /// <summary>
    /// The agent conducting the interview
    /// </summary>
    public Agent? Agent { get; set; }

    /// <summary>
    /// The job associated with the interview
    /// </summary>
    public Job? Job { get; set; }

    /// <summary>
    /// The applicant taking the interview
    /// </summary>
    public Applicant? Applicant { get; set; }

    /// <summary>
    /// The interview questions from the interview guide
    /// </summary>
    public List<InterviewGuideQuestion> Questions { get; set; } = new();

    /// <summary>
    /// The candidate session record
    /// </summary>
    public CandidateSession Session { get; set; } = null!;
}
