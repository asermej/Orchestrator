using System;
using System.ComponentModel.DataAnnotations;

namespace Orchestrator.Api.ResourcesModels;

/// <summary>
/// Request model for the streaming conversation turn endpoint.
/// LATENCY-CRITICAL: This request initiates a single-call streaming pipeline
/// that replaces the sequential classify → evaluate → TTS round trips.
/// </summary>
public class RespondToTurnResource
{
    [Required]
    public string CandidateTranscript { get; set; } = "";

    [Required]
    public Guid CompetencyId { get; set; }

    [Required]
    public string CompetencyName { get; set; } = "";

    [Required]
    public string CurrentQuestion { get; set; } = "";

    /// <summary>"primary" or "followup"</summary>
    public string Phase { get; set; } = "primary";

    public int FollowUpCount { get; set; }

    /// <summary>All candidate responses for this competency concatenated.</summary>
    public string AccumulatedTranscript { get; set; } = "";

    /// <summary>What the previous follow-up probed ("action" or "result"), if any.</summary>
    public string? PreviousFollowUpTarget { get; set; }

    /// <summary>How many question repeats the candidate has left for this competency.</summary>
    public int RepeatsRemaining { get; set; } = 2;

    /// <summary>ISO 639-1 code when the candidate has requested a language switch, null for default (English).</summary>
    public string? Language { get; set; }

    /// <summary>The AI's previous spoken response for this competency, used to avoid identical rephrasing on repeats.</summary>
    public string? PreviousAiResponse { get; set; }

    /// <summary>True when this is the final competency in the interview; prevents "let's move on" phrasing.</summary>
    public bool IsLastCompetency { get; set; }
}
