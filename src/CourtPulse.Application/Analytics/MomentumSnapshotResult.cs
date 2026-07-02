using CourtPulse.Domain.Enums;

namespace CourtPulse.Application.Analytics;

/// <summary>
/// The engine's output for a single processed point: the state after applying
/// it, plus a human-readable reason for the change. One of these maps to one
/// (or two, if you split per player) <c>MomentumSnapshot</c> rows.
/// </summary>
public sealed record MomentumSnapshotResult
{
    public required int SetNumber { get; init; }
    public required int GameNumber { get; init; }
    public required int PointNumber { get; init; }

    /// <summary>Which player this change primarily benefited.</summary>
    public required PlayerSide Beneficiary { get; init; }

    /// <summary>Signed weight applied on this point (already includes any game-close bonus).</summary>
    public required double Delta { get; init; }

    /// <summary>Why the delta was what it was — handy for debugging and for UI tooltips.</summary>
    public required string Reason { get; init; }

    /// <summary>The beneficiary won this point on the opponent's serve.</summary>
    public required bool WasReturnPoint { get; init; }

    /// <summary>This point was a break, set or match point (a pressure point).</summary>
    public required bool WasPressurePoint { get; init; }

    /// <summary>Full momentum state after this point was folded in.</summary>
    public required MomentumState State { get; init; }
}
