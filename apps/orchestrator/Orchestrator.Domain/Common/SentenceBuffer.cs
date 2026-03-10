using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Orchestrator.Domain;

/// <summary>
/// Shared utility for splitting streamed text into sentences at punctuation boundaries,
/// skipping abbreviations. Used by both the phone call pipeline and web interview pipeline.
/// </summary>
internal static class SentenceBuffer
{
    private static readonly HashSet<string> Abbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        "Dr.", "Mr.", "Mrs.", "Ms.", "Prof.", "Jr.", "Sr.", "vs.", "etc.", "Inc.", "Ltd.", "Corp.",
        "Ave.", "Blvd.", "St.", "Rd.", "Mt.", "ft.", "lb.", "oz.", "pt.", "qt.", "gal."
    };

    /// <summary>
    /// Finds the index of a sentence-ending character (., !, ?) that is not part of an abbreviation.
    /// Returns -1 if no sentence end is found.
    /// </summary>
    public static int FindSentenceEnd(string text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c != '.' && c != '!' && c != '?')
                continue;

            if (c == '.' && IsAbbreviation(text, i))
                continue;

            return i;
        }
        return -1;
    }

    /// <summary>
    /// Checks whether the period at the given index is part of a known abbreviation.
    /// </summary>
    private static bool IsAbbreviation(string text, int dotIndex)
    {
        int wordStart = dotIndex;
        while (wordStart > 0 && text[wordStart - 1] != ' ')
            wordStart--;

        var word = text.Substring(wordStart, dotIndex - wordStart + 1);

        if (Abbreviations.Contains(word))
            return true;

        if (Regex.IsMatch(word, @"^[A-Za-z]\.[A-Za-z]\.$"))
            return true;

        return false;
    }
}
