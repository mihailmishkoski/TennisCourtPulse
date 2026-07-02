namespace CourtPulse.Application.Analytics;

/// <summary>
/// Finds the pivotal moments in a momentum timeline. Pure and deterministic —
/// it reads the snapshot stream the <see cref="MomentumCalculationService"/>
/// produced and never re-derives anything.
/// </summary>
public sealed class TurningPointDetector
{
    /// <summary>Extra weight added to a moment that flips the overall lead.</summary>
    private readonly double _leadChangeBonus;

    public TurningPointDetector(double leadChangeBonus = 4.0)
    {
        _leadChangeBonus = leadChangeBonus;
    }

    /// <summary>
    /// Return up to <paramref name="maxCount"/> most pivotal moments, in
    /// chronological order. Impact combines the raw momentum delta with a bonus
    /// when the cumulative lead changed hands at that moment.
    /// </summary>
    public IReadOnlyList<TurningPoint> Detect(IReadOnlyList<MomentumSnapshotResult> snapshots, int maxCount)
    {
        List<TurningPoint> candidates = new List<TurningPoint>();
        double previousDifferential = 0.0;

        foreach (MomentumSnapshotResult snapshot in snapshots)
        {
            double currentDifferential = snapshot.State.CumulativeDifferential;

            // A lead change is a sign flip while both sides of the flip are meaningful
            // (ignore the trivial first move away from an exact 0-0 tie).
            bool leadChanged = previousDifferential != 0.0
                && Math.Sign(currentDifferential) != Math.Sign(previousDifferential)
                && currentDifferential != 0.0;

            double impact = Math.Abs(snapshot.Delta) + (leadChanged ? _leadChangeBonus : 0.0);

            candidates.Add(new TurningPoint
            {
                SetNumber = snapshot.SetNumber,
                GameNumber = snapshot.GameNumber,
                PointNumber = snapshot.PointNumber,
                Beneficiary = snapshot.Beneficiary,
                Reason = leadChanged ? snapshot.Reason + " (took the lead)" : snapshot.Reason,
                Delta = snapshot.Delta,
                DifferentialAfter = currentDifferential,
                LeadChanged = leadChanged,
                Impact = impact
            });

            previousDifferential = currentDifferential;
        }

        // Keep the top-N by impact, then restore chronological order for display.
        List<TurningPoint> topByImpact = candidates
            .OrderByDescending((TurningPoint t) => t.Impact)
            .Take(maxCount)
            .ToList();

        topByImpact.Sort((TurningPoint a, TurningPoint b) =>
        {
            int bySet = a.SetNumber.CompareTo(b.SetNumber);
            if (bySet != 0) { return bySet; }
            int byGame = a.GameNumber.CompareTo(b.GameNumber);
            if (byGame != 0) { return byGame; }
            return a.PointNumber.CompareTo(b.PointNumber);
        });

        return topByImpact;
    }
}
