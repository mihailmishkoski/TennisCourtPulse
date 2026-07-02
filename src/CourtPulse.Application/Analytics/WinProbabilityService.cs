using CourtPulse.Domain.Enums;

namespace CourtPulse.Application.Analytics;

/// <summary>
/// Estimates live match win probability from the classic hierarchical tennis
/// model: a per-point serve-win probability drives a game model, games drive a
/// set model, sets drive a match model. Everything is analytic + memoised, so a
/// call is cheap enough to run on every score change.
///
/// Documented approximations (deliberately simple, easy to refine later):
///   * The tiebreak uses an averaged per-point win rate rather than modelling the
///     exact 1-2-2 serve rotation.
///   * Future (not-yet-started) sets use a serve-neutral set probability; only
///     the set in progress uses the live game/point state.
/// These wash out quickly and keep the model transparent.
/// </summary>
public sealed class WinProbabilityService
{
    public WinProbability Estimate(WinProbabilityInput input)
    {
        double pServeA = Clamp(input.FirstServePointWin);
        double pServeB = Clamp(input.SecondServePointWin);

        Dictionary<(int, int), double> gameMemoA = new Dictionary<(int, int), double>();
        Dictionary<(int, int), double> gameMemoB = new Dictionary<(int, int), double>();

        double holdA = Game(pServeA, 0, 0, gameMemoA);
        double holdB = Game(pServeB, 0, 0, gameMemoB);

        // Tiebreak: A's effective per-point win rate is the mean of holding and breaking.
        double tiebreakPoint = ((pServeA) + (1.0 - pServeB)) / 2.0;
        double tiebreakA = Race(tiebreakPoint, 0, 0, new Dictionary<(int, int), double>());

        bool aServesCurrent = input.Serving == PlayerSide.First;
        double currentGameWinA = aServesCurrent
            ? Game(pServeA, input.ServerPointsInGame, input.ReturnerPointsInGame, gameMemoA)
            : 1.0 - Game(pServeB, input.ServerPointsInGame, input.ReturnerPointsInGame, gameMemoB);

        // Set in progress: first game transition uses the live game state.
        Dictionary<(int, int, bool, bool), double> setMemo = new Dictionary<(int, int, bool, bool), double>();
        double currentSetA = Set(input.FirstGamesInSet, input.SecondGamesInSet, aServesCurrent, true,
            holdA, holdB, currentGameWinA, tiebreakA, setMemo);

        // Future sets: average over who serves first (serve-neutral).
        double futureSetA = 0.5 * (
            Set(0, 0, true, false, holdA, holdB, 0.0, tiebreakA, setMemo) +
            Set(0, 0, false, false, holdA, holdB, 0.0, tiebreakA, setMemo));

        int setsToWin = input.BestOfFive ? 3 : 2;
        Dictionary<(int, int), double> matchMemo = new Dictionary<(int, int), double>();

        double matchA =
            currentSetA * Match(input.FirstSetsWon + 1, input.SecondSetsWon, setsToWin, futureSetA, matchMemo) +
            (1.0 - currentSetA) * Match(input.FirstSetsWon, input.SecondSetsWon + 1, setsToWin, futureSetA, matchMemo);

        matchA = Math.Clamp(matchA, 0.0, 1.0);
        return new WinProbability { First = matchA, Second = 1.0 - matchA };
    }

    /// <summary>Server's win probability for a game, from (server, returner) points.</summary>
    private static double Game(double p, int a, int b, Dictionary<(int, int), double> memo)
    {
        double q = 1.0 - p;

        if (a >= 4 && a - b >= 2) { return 1.0; }
        if (b >= 4 && b - a >= 2) { return 0.0; }

        if (a >= 3 && b >= 3)
        {
            double deuce = (p * p) / ((p * p) + (q * q));
            if (a == b) { return deuce; }
            if (a > b) { return p + (q * deuce); }   // advantage server
            return p * deuce;                          // advantage returner
        }

        if (memo.TryGetValue((a, b), out double cached))
        {
            return cached;
        }

        double result = (p * Game(p, a + 1, b, memo)) + (q * Game(p, a, b + 1, memo));
        memo[(a, b)] = result;
        return result;
    }

    /// <summary>First-to-7, win-by-2 race with a constant per-point win rate (tiebreak model).</summary>
    private static double Race(double p, int a, int b, Dictionary<(int, int), double> memo)
    {
        double q = 1.0 - p;

        if (a >= 7 && a - b >= 2) { return 1.0; }
        if (b >= 7 && b - a >= 2) { return 0.0; }

        if (a >= 6 && b >= 6)
        {
            double deuce = (p * p) / ((p * p) + (q * q));
            if (a == b) { return deuce; }
            if (a > b) { return p + (q * deuce); }
            return p * deuce;
        }

        if (memo.TryGetValue((a, b), out double cached))
        {
            return cached;
        }

        double result = (p * Race(p, a + 1, b, memo)) + (q * Race(p, a, b + 1, memo));
        memo[(a, b)] = result;
        return result;
    }

    /// <summary>First player's probability of winning the set from a games score.</summary>
    private static double Set(int ga, int gb, bool aServes, bool firstGame,
        double holdA, double holdB, double currentGameWinA, double tiebreakA,
        Dictionary<(int, int, bool, bool), double> memo)
    {
        if (ga >= 6 && ga - gb >= 2) { return 1.0; }
        if (gb >= 6 && gb - ga >= 2) { return 0.0; }
        if (ga == 6 && gb == 6) { return tiebreakA; }

        if (memo.TryGetValue((ga, gb, aServes, firstGame), out double cached))
        {
            return cached;
        }

        double winGameA = firstGame ? currentGameWinA : (aServes ? holdA : 1.0 - holdB);
        double result =
            (winGameA * Set(ga + 1, gb, !aServes, false, holdA, holdB, currentGameWinA, tiebreakA, memo)) +
            ((1.0 - winGameA) * Set(ga, gb + 1, !aServes, false, holdA, holdB, currentGameWinA, tiebreakA, memo));

        memo[(ga, gb, aServes, firstGame)] = result;
        return result;
    }

    /// <summary>First player's probability of winning the match from a sets score.</summary>
    private static double Match(int sa, int sb, int setsToWin, double pSet,
        Dictionary<(int, int), double> memo)
    {
        if (sa >= setsToWin) { return 1.0; }
        if (sb >= setsToWin) { return 0.0; }

        if (memo.TryGetValue((sa, sb), out double cached))
        {
            return cached;
        }

        double result = (pSet * Match(sa + 1, sb, setsToWin, pSet, memo)) +
            ((1.0 - pSet) * Match(sa, sb + 1, setsToWin, pSet, memo));
        memo[(sa, sb)] = result;
        return result;
    }

    private static double Clamp(double p)
    {
        // Keep away from the exact edges so deuce formulas never divide by zero.
        return Math.Clamp(p, 0.01, 0.99);
    }
}
