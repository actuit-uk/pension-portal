using PensionPortal.CalcLib.Internal;

namespace PensionPortal.CalcLib;

/// <summary>
/// Estimates a complete MemberData record from minimal inputs.
/// Synthesises NI contributions (pre-88) and band earnings (post-88)
/// from a single salary anchor inflated/deflated by published earnings indices.
/// Designed for schemes with patchy records that lack per-year earnings data.
/// </summary>
public static class EarningsEstimator
{
    private static readonly DateTime CoStartFloor = new(1978, 4, 6);
    private static readonly DateTime CoEndCeiling = new(1997, 4, 5);

    /// <summary>
    /// Produces a MemberData record with a synthetic earnings history.
    /// </summary>
    /// <param name="sex">Member's sex.</param>
    /// <param name="dateOfBirth">Date of birth.</param>
    /// <param name="dateJoined">Date of joining pensionable service.</param>
    /// <param name="dateLeft">Date of leaving pensionable service.</param>
    /// <param name="salary1990">Approximate annual salary circa 1990 (the anchor).</param>
    /// <param name="factors">Factor provider (reserved for future use).</param>
    /// <param name="salaryMargin">
    /// Additive margin applied to the 1990 salary before index scaling.
    /// 0.0 = no adjustment, 0.10 = 10% uplift, -0.05 = 5% reduction.
    /// </param>
    public static MemberData Estimate(
        Sex sex,
        DateTime dateOfBirth,
        DateTime dateJoined,
        DateTime dateLeft,
        decimal salary1990,
        IFactorProvider factors,
        decimal salaryMargin = 0m)
    {
        // Step 1: Clamp contracted-out period
        var coStart = dateJoined < CoStartFloor ? CoStartFloor : dateJoined;
        var coEnd = dateLeft > CoEndCeiling ? CoEndCeiling : dateLeft;

        // Step 2: Determine active tax years
        int firstTy = TaxYearHelper.TaxYearFromDate(coStart);
        int lastTy = TaxYearHelper.TaxYearFromDate(coEnd);

        // If coEnd falls exactly on 5 April, it belongs to the previous tax year
        // (handled by TaxYearFromDate). If coEnd is before coStart, no earnings.
        if (lastTy < firstTy)
            lastTy = firstTy;

        // Anchor index (1990 tax year)
        decimal anchorIndex = NiThresholds.AverageWeeklyEarnings[1990];
        decimal adjustedSalary = salary1990 * (1m + salaryMargin);

        var earnings = new Dictionary<int, decimal>();

        for (int ty = firstTy; ty <= lastTy && ty <= 1996; ty++)
        {
            if (!NiThresholds.AverageWeeklyEarnings.TryGetValue(ty, out decimal yearIndex))
                continue;
            if (!NiThresholds.LEL.TryGetValue(ty, out decimal lel))
                continue;
            if (!NiThresholds.UEL.TryGetValue(ty, out decimal uel))
                continue;

            // Step 3: Estimate salary for this tax year
            decimal salary = Math.Round(adjustedSalary * (yearIndex / anchorIndex), 0);

            // Step 4: Convert to band earnings, then to the appropriate format
            decimal bandEarnings = Math.Max(0m, Math.Min(salary, uel) - lel);

            if (ty <= TaxYearGmp.LastNIContributionYear)
            {
                // Pre-1988: convert to NI contributions using the GMP NI divisor.
                // TaxYearGmp divides by this same rate to recover band earnings.
                decimal nics = Math.Round(bandEarnings * TaxYearGmp.NiDivisor, 2);
                if (nics > 0m)
                    earnings[ty] = nics;
            }
            else
            {
                // Post-1988: band earnings used directly.
                decimal rounded = Math.Round(bandEarnings, 0);
                if (rounded > 0m)
                    earnings[ty] = rounded;
            }
        }

        // Step 5: Estimate FinalPensionableSalary at date of leaving
        int leavingTy = TaxYearHelper.TaxYearFromDate(dateLeft);
        decimal fps;
        if (NiThresholds.AverageWeeklyEarnings.TryGetValue(leavingTy, out decimal leavingIndex))
        {
            fps = Math.Round(adjustedSalary * (leavingIndex / anchorIndex), 0);
        }
        else
        {
            // Leaving year outside index range — extrapolate from last known year
            decimal lastKnownIndex = NiThresholds.AverageWeeklyEarnings[1997];
            int yearsAfter = leavingTy - 1997;
            // Assume ~3% annual growth beyond 1997 (a conservative approximation)
            decimal extrapolated = lastKnownIndex * DecimalPow(1.03m, yearsAfter);
            fps = Math.Round(adjustedSalary * (extrapolated / anchorIndex), 0);
        }

        // Step 6: Return MemberData
        return new MemberData(
            Sex: sex,
            DateOfBirth: dateOfBirth,
            DateCOStart: coStart,
            DateCOEnd: coEnd,
            DateOfLeaving: dateLeft,
            DateOfRetirement: null,
            DateOfDeath: null,
            Earnings: earnings,
            PensionAtLeaving: null,
            FinalPensionableSalary: fps);
    }

    private static decimal DecimalPow(decimal baseVal, int exponent)
    {
        if (exponent <= 0) return 1m;
        decimal result = 1m;
        for (int i = 0; i < exponent; i++)
            result *= baseVal;
        return result;
    }
}
