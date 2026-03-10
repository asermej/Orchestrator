namespace Orchestrator.Domain;

/// <summary>
/// Universal 1-5 behavioral anchor rubric applied to every competency across all roles.
/// Not stored per competency; used as read-only reference for scoring and UI.
/// </summary>
public static class UniversalRubric
{
    public const int MinScore = 1;
    public const int MaxScore = 5;

    private static readonly UniversalRubricLevel[] Levels =
    {
        new(1, "No evidence", "Vague or can't articulate a relevant example."),
        new(2, "Weak", "Generic answer, no specifics, story is incomplete."),
        new(3, "Adequate", "Real example with some specifics, story mostly complete."),
        new(4, "Strong", "Concrete, specific, self-aware, complete story with clear actions and outcome."),
        new(5, "Exceptional", "Specific process, demonstrates mastery, connects actions to measurable impact, fully complete story.")
    };

    public static IReadOnlyList<UniversalRubricLevel> GetAllLevels() => Levels;
}

public class UniversalRubricLevel
{
    public int Level { get; }
    public string Label { get; }
    public string Description { get; }

    public UniversalRubricLevel(int level, string label, string description)
    {
        Level = level;
        Label = label;
        Description = description;
    }
}
