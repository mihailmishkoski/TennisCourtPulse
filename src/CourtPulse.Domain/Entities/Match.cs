using CourtPulse.Domain.Enums;

namespace CourtPulse.Domain.Entities;

public sealed class Match
{
    public Guid Id { get; set; }
    public long ExternalEventKey { get; set; }

    public Guid TournamentId { get; set; }
    public Tournament? Tournament { get; set; }

    public Guid FirstPlayerId { get; set; }
    public Player? FirstPlayer { get; set; }
    public Guid SecondPlayerId { get; set; }
    public Player? SecondPlayer { get; set; }

    public DateOnly EventDate { get; set; }
    public TimeOnly EventTime { get; set; }
    public string? Round { get; set; }
    public string? EventType { get; set; }
    public string? Status { get; set; }
    public bool IsLive { get; set; }
    public bool IsFinished { get; set; }
    public string? FinalResult { get; set; }

    // Live game state for win-probability (from event_serve / event_game_result).
    public PlayerSide? Serving { get; set; }
    public int CurrentFirstPoints { get; set; }
    public int CurrentSecondPoints { get; set; }

    public Guid? WinnerId { get; set; }
    public Player? Winner { get; set; }

    // Carried momentum state so the sync advances incrementally instead of recomputing.
    public double MomentumFirstCumulative { get; set; }
    public double MomentumSecondCumulative { get; set; }
    public double MomentumFirstEwma { get; set; }
    public double MomentumSecondEwma { get; set; }

    public DateTimeOffset LastSyncedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<MatchSet> Sets { get; set; } = new List<MatchSet>();
    public ICollection<MatchGame> Games { get; set; } = new List<MatchGame>();
    public ICollection<PlayerMatchStatistic> Statistics { get; set; } = new List<PlayerMatchStatistic>();
    public ICollection<MomentumSnapshot> MomentumSnapshots { get; set; } = new List<MomentumSnapshot>();
}
