namespace PensionPortal.CalcLib.Internal;

/// <summary>
/// Builds the year-by-year pension cash flow projection from GMP results.
/// Uses the separate increase method: pre-88 GMP stays flat, post-88 GMP
/// increases at LPI3 (CPI capped at 3%) once in payment.
/// Excess pension is computed via ExcessPensionCalculator (three-tier fallback)
/// and increases at the scheme PIP rate once in payment.
/// </summary>
internal static class CashFlowBuilder
{
    /// <summary>
    /// Builds cash flow entries from the tax year of leaving through to the projection end year.
    /// </summary>
    /// <param name="gmp">GMP calculation result (at-leaving and revalued values).</param>
    /// <param name="member">Member data (DOB for determining GMP payable ages).</param>
    /// <param name="scheme">Scheme configuration (NRA ages, PIP method, assumptions).</param>
    /// <param name="factors">Factor provider for LPI3 increase lookups.</param>
    internal static IReadOnlyList<CashFlowEntry> Build(
        GmpResult gmp,
        MemberData member,
        SchemeConfig scheme,
        IFactorProvider factors)
    {
        var assumptions = scheme.Assumptions;
        int startYear = gmp.TaxYearOfLeaving;
        int endYear = assumptions.ProjectionEndYear;

        // Determine PIP start years (tax year in which GMP payable age is reached)
        int malePipYear = PipStartYear(member.DateOfBirth, 65);
        int femalePipYear = PipStartYear(member.DateOfBirth, 60);

        // Excess pension above GMP (three-tier: direct, salary-based, or zero)
        var (excessAtLeavingM, excessAtLeavingF) = ExcessPensionCalculator.Calculate(
            member, scheme, gmp.MaleAtLeaving.TotalAnnual, gmp.FemaleAtLeaving.TotalAnnual);

        var entries = new List<CashFlowEntry>();

        // Running post-88 amounts (updated each PIP year)
        decimal runningPost88M = 0m;
        decimal runningPost88F = 0m;
        decimal runningExcessM = excessAtLeavingM;
        decimal runningExcessF = excessAtLeavingF;
        bool maleEnteredPip = false;
        bool femaleEnteredPip = false;
        decimal prevFactor = 0m;
        decimal prevExcessFactor = 0m;

        for (int year = startYear; year <= endYear; year++)
        {
            var statusM = GetStatus(year, startYear, malePipYear);
            var statusF = GetStatus(year, startYear, femalePipYear);

            // LPI3 factor for this year (used to compute NEXT year's PIP amount)
            decimal factor = factors.GetPipIncreaseFactor(PipIncreaseMethod.LPI3, year)
                ?? assumptions.FuturePost88GmpIncRate;

            // Excess increase factor (scheme PIP rate — typically LPI5 for excess, LPI3 for GMP)
            decimal excessFactor = factors.GetPipIncreaseFactor(scheme.PipMethod, year)
                ?? assumptions.FuturePipRate;

            // --- Male ---
            decimal pre88M, post88M;
            if (statusM != GmpStatus.InPayment)
            {
                // EXIT or DEF: at-leaving values, flat
                pre88M = gmp.MaleAtLeaving.Pre88Annual;
                post88M = gmp.MaleAtLeaving.Post88Annual;
            }
            else if (!maleEnteredPip)
            {
                // First PIP year: jump to revalued values
                pre88M = gmp.MaleRevalued.Pre88Annual;
                post88M = gmp.MaleRevalued.Post88Annual;
                runningPost88M = post88M;
                maleEnteredPip = true;
            }
            else
            {
                // Subsequent PIP years: pre-88 flat, post-88 increases
                pre88M = gmp.MaleRevalued.Pre88Annual;
                post88M = Math.Round(runningPost88M * (1m + prevFactor), 2);
                runningPost88M = post88M;
            }

            // --- Female ---
            decimal pre88F, post88F;
            if (statusF != GmpStatus.InPayment)
            {
                pre88F = gmp.FemaleAtLeaving.Pre88Annual;
                post88F = gmp.FemaleAtLeaving.Post88Annual;
            }
            else if (!femaleEnteredPip)
            {
                pre88F = gmp.FemaleRevalued.Pre88Annual;
                post88F = gmp.FemaleRevalued.Post88Annual;
                runningPost88F = post88F;
                femaleEnteredPip = true;
            }
            else
            {
                pre88F = gmp.FemaleRevalued.Pre88Annual;
                post88F = Math.Round(runningPost88F * (1m + prevFactor), 2);
                runningPost88F = post88F;
            }

            decimal totalM = Math.Round(pre88M + post88M, 2);
            decimal totalF = Math.Round(pre88F + post88F, 2);

            // Excess pension: flat until PIP, then increases at scheme PIP rate
            decimal excessM, excessF;
            if (statusM != GmpStatus.InPayment || !maleEnteredPip)
            {
                excessM = excessAtLeavingM;
            }
            else
            {
                excessM = Math.Round(runningExcessM * (1m + prevExcessFactor), 2);
                runningExcessM = excessM;
            }
            if (statusF != GmpStatus.InPayment || !femaleEnteredPip)
            {
                excessF = excessAtLeavingF;
            }
            else
            {
                excessF = Math.Round(runningExcessF * (1m + prevExcessFactor), 2);
                runningExcessF = excessF;
            }

            entries.Add(new CashFlowEntry(
                TaxYear: year,
                StatusMale: statusM,
                Pre88GmpMale: pre88M,
                Post88GmpMale: post88M,
                TotalGmpMale: totalM,
                ExcessMale: excessM,
                TotalPensionMale: totalM + excessM,
                StatusFemale: statusF,
                Pre88GmpFemale: pre88F,
                Post88GmpFemale: post88F,
                TotalGmpFemale: totalF,
                ExcessFemale: excessF,
                TotalPensionFemale: totalF + excessF,
                Post88GmpIncFactor: factor,
                ExcessIncFactor: excessFactor));

            prevFactor = factor;
            prevExcessFactor = excessFactor;
        }

        return entries.AsReadOnly();
    }

    /// <summary>
    /// Determines the GMP status for a given tax year.
    /// </summary>
    private static GmpStatus GetStatus(int taxYear, int taxYearOfLeaving, int pipStartYear)
    {
        if (taxYear == taxYearOfLeaving)
            return GmpStatus.Exit;
        if (taxYear < pipStartYear)
            return GmpStatus.Deferred;
        return GmpStatus.InPayment;
    }

    /// <summary>
    /// Returns the tax year in which GMP payable age is reached.
    /// </summary>
    private static int PipStartYear(DateTime dateOfBirth, int gmpAge)
    {
        int gmpAgeYear = dateOfBirth.Year + gmpAge;
        var gmpAgeDate = new DateTime(gmpAgeYear, dateOfBirth.Month, dateOfBirth.Day);
        return TaxYearHelper.TaxYearFromDate(gmpAgeDate);
    }
}
