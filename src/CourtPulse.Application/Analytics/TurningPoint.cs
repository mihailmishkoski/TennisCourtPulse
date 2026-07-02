using CourtPulse.Domain.Enums;

namespace CourtPulse.Application.Analytics;

/// <summary>
/// A moment the match swung — either a big single momentum event (a break, a set
/// point) or the point where the overall lead changed hands. These are what the
/// UI pins on the momentum graph and lists as "key moments".
/// </summary>
public sealed record TurningPoint
{
    public required int SetNumber { get; init; }
    public required int GameNumber { get; init; }
    public required int PointNumber { get; init; }

    public required PlayerSide Beneficiary { get; init; }
    public required string Reason { get; init; }
    public required double Delta { get; init; }

    /// <summary>Cumulative differential (First − Second) immediately after this moment.</summary>
    public required double DifferentialAfter { get; init; }

    /// <summary>True when the cumulative lead flipped from one player to the other here.</summary>
    public required bool LeadChanged { get; init; }

    /// <summary>Ranking score — bigger means more pivotal.</summary>
    public required double Impact { get; init; }
}
