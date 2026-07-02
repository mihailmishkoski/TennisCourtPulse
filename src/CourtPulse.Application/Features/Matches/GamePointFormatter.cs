using CourtPulse.Domain.Entities;

namespace CourtPulse.Application.Features.Matches;

/// <summary>
/// Formats the stored current-game point ranks (0/1/2/3/4) back into the
/// tennis tokens ("0"/"15"/"30"/"40"/"A") for display. Only live, unfinished
/// matches have a meaningful in-progress game score.
/// </summary>
public static class GamePointFormatter
{
    private static readonly string[] Tokens = { "0", "15", "30", "40", "A" };

    public static string? Format(Match match)
    {
        if (!match.IsLive || match.IsFinished)
        {
            return null;
        }

        return $"{Token(match.CurrentFirstPoints)} - {Token(match.CurrentSecondPoints)}";
    }

    private static string Token(int rank)
    {
        return rank >= 0 && rank < Tokens.Length ? Tokens[rank] : "0";
    }
}
