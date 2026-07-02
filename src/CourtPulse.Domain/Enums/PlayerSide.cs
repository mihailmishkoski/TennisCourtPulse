namespace CourtPulse.Domain.Enums;

/// <summary>
/// Which of the two players in a match a given event belongs to.
/// The external api-tennis payload identifies participants as
/// "First Player" / "Second Player" (relative to the event record),
/// so we mirror that here rather than inventing home/away semantics.
/// </summary>
public enum PlayerSide
{
    First = 1,
    Second = 2
}
