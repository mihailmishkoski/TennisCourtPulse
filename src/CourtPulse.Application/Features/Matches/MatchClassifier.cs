namespace CourtPulse.Application.Features.Matches;

/// <summary>
/// Derives display groupings from the feed's free-text event type
/// (e.g. "Atp Singles", "Itf Women Singles", "Challenger Men Doubles").
/// Gender is the robust split because it spans every tour; the tour tag
/// (ATP/WTA/ITF/Challenger) is a secondary label.
/// </summary>
public static class MatchClassifier
{
    public const string Men = "Men";
    public const string Women = "Women";
    public const string Other = "Other";

    public static string Gender(string? eventType)
    {
        if (string.IsNullOrWhiteSpace(eventType))
        {
            return Other;
        }

        string t = eventType.ToLowerInvariant();

        // Check women first: "women" contains the substring "men".
        if (t.Contains("women") || t.Contains("wta") || t.Contains("ladies") || t.Contains("girls"))
        {
            return Women;
        }

        if (t.Contains("atp") || t.Contains("men") || t.Contains("boys"))
        {
            return Men;
        }

        return Other;
    }

    public static string? Tour(string? eventType)
    {
        if (string.IsNullOrWhiteSpace(eventType))
        {
            return null;
        }

        string t = eventType.ToLowerInvariant();
        if (t.Contains("atp")) return "ATP";
        if (t.Contains("wta")) return "WTA";
        if (t.Contains("challenger")) return "Challenger";
        if (t.Contains("itf")) return "ITF";
        return null;
    }
}
