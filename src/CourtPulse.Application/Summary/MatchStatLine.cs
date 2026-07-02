namespace CourtPulse.Application.Summary;

/// <summary>
/// One normalised statistic row for one player, lifted out of the api-tennis
/// <c>statistics</c> array. The feed already aggregates these (per match), so the
/// summary engine's job is interpretation, not calculation.
///
/// Values arrive in three shapes and we keep them all:
///   * A ratio with <see cref="Won"/>/<see cref="Total"/> (e.g. 29/36) — the most
///     trustworthy, because we can judge the sample size too.
///   * A percentage string in <see cref="RawValue"/> (e.g. "81%") with no counts.
///   * A bare count / measurement (e.g. "40", "200 km/h", "2098").
/// </summary>
public sealed record MatchStatLine
{
    public required long PlayerKey { get; init; }

    /// <summary>api-tennis <c>stat_type</c>: "Service" / "Return" / "Points" / "Games".</summary>
    public required string StatType { get; init; }

    /// <summary>api-tennis <c>stat_name</c>, e.g. "1st serve points won".</summary>
    public required string StatName { get; init; }

    /// <summary>Raw <c>stat_value</c> as delivered ("81%", "40", "200 km/h").</summary>
    public required string RawValue { get; init; }

    public int? Won { get; init; }
    public int? Total { get; init; }

    /// <summary>
    /// Best-effort percentage (0..100) for this line, or null when it isn't a
    /// rate. Prefers Won/Total (carries sample size) over parsing the string.
    /// </summary>
    public double? AsPercentage()
    {
        if (Won.HasValue && Total.HasValue && Total.Value > 0)
        {
            return 100.0 * Won.Value / Total.Value;
        }

        string trimmed = RawValue.Trim();
        if (trimmed.EndsWith('%') && double.TryParse(trimmed[..^1], out double parsed))
        {
            return parsed;
        }

        return null;
    }

    /// <summary>Best-effort bare number ("40" → 40, "200 km/h" → 200), or null.</summary>
    public double? AsNumber()
    {
        string trimmed = RawValue.Trim();
        int firstSpace = trimmed.IndexOf(' ');
        string head = firstSpace > 0 ? trimmed[..firstSpace] : trimmed;
        return double.TryParse(head, out double parsed) ? parsed : null;
    }
}
