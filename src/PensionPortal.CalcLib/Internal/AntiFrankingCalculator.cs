namespace PensionPortal.CalcLib.Internal;

/// <summary>
/// Applies the anti-franking floor to a cash flow projection using the Ring-Fence (90-97)
/// technique from PASA guidance. Anti-franking prevents schemes from reducing excess pension
/// to offset GMP revaluation increases.
///
/// The anti-franking minimum pension (AFM) at GMP payable age is:
///   AFM = Revalued GMP + Excess at leaving
///
/// In subsequent PIP years, the AFM evolves:
///   - Pre-88 GMP: stays flat (no increases)
///   - Post-88 GMP: increases at statutory rate (LPI3)
///   - Excess component of AFM: stays at leaving value (ring-fenced)
///
/// If the actual total pension in any PIP year is less than the AFM, the excess
/// pension is increased to make up the difference.
///
/// Note: Under the separate increase method (where excess is tracked independently
/// from GMP), the AFM equals the actual total pension by construction, so anti-franking
/// never bites. It becomes material under the overall increase method where a single
/// increase rate applies to the total pension and GMP increases may eat into excess.
/// </summary>
internal static class AntiFrankingCalculator
{
    /// <summary>
    /// Returns a new cash flow with the anti-franking floor applied.
    /// Entries where the total pension is below the AFM have their excess
    /// adjusted upward. Entries above the AFM are returned unchanged.
    /// </summary>
    /// <param name="cashFlow">Raw cash flow projection.</param>
    /// <param name="gmp">GMP result (provides revalued amounts and at-leaving excess).</param>
    /// <param name="excessAtLeavingMale">Excess pension at leaving for male calculation.</param>
    /// <param name="excessAtLeavingFemale">Excess pension at leaving for female calculation.</param>
    /// <param name="factors">Factor provider for LPI3 lookups.</param>
    /// <param name="assumptions">Fallback increase rate.</param>
    internal static IReadOnlyList<CashFlowEntry> ApplyFloor(
        IReadOnlyList<CashFlowEntry> cashFlow,
        GmpResult gmp,
        decimal excessAtLeavingMale,
        decimal excessAtLeavingFemale,
        IFactorProvider factors,
        FutureAssumptions assumptions)
    {
        var result = new List<CashFlowEntry>(cashFlow.Count);

        // Track the running AFM post-88 GMP component (increases at LPI3)
        decimal afmPost88M = gmp.MaleRevalued.Post88Annual;
        decimal afmPost88F = gmp.FemaleRevalued.Post88Annual;
        bool maleEnteredPip = false;
        bool femaleEnteredPip = false;
        decimal prevFactor = 0m;

        for (int i = 0; i < cashFlow.Count; i++)
        {
            var cf = cashFlow[i];

            decimal factor = factors.GetPipIncreaseFactor(PipIncreaseMethod.LPI3, cf.TaxYear)
                ?? assumptions.FuturePost88GmpIncRate;

            // --- Male AFM ---
            decimal adjustedExcessM = cf.ExcessMale;
            if (cf.StatusMale == GmpStatus.InPayment)
            {
                if (!maleEnteredPip)
                {
                    // First PIP year: AFM = revalued GMP + excess at leaving
                    afmPost88M = gmp.MaleRevalued.Post88Annual;
                    maleEnteredPip = true;
                }
                else
                {
                    // Subsequent: AFM post-88 increases at LPI3
                    afmPost88M = Math.Round(afmPost88M * (1m + prevFactor), 2);
                }

                decimal afmM = gmp.MaleRevalued.Pre88Annual + afmPost88M + excessAtLeavingMale;
                decimal actualTotalM = cf.TotalGmpMale + cf.ExcessMale;

                if (actualTotalM < afmM)
                {
                    // Top up excess to meet the floor
                    adjustedExcessM = Math.Round(afmM - cf.TotalGmpMale, 2);
                }
            }

            // --- Female AFM ---
            decimal adjustedExcessF = cf.ExcessFemale;
            if (cf.StatusFemale == GmpStatus.InPayment)
            {
                if (!femaleEnteredPip)
                {
                    afmPost88F = gmp.FemaleRevalued.Post88Annual;
                    femaleEnteredPip = true;
                }
                else
                {
                    afmPost88F = Math.Round(afmPost88F * (1m + prevFactor), 2);
                }

                decimal afmF = gmp.FemaleRevalued.Pre88Annual + afmPost88F + excessAtLeavingFemale;
                decimal actualTotalF = cf.TotalGmpFemale + cf.ExcessFemale;

                if (actualTotalF < afmF)
                {
                    adjustedExcessF = Math.Round(afmF - cf.TotalGmpFemale, 2);
                }
            }

            prevFactor = factor;

            // Only create a new entry if excess was adjusted
            if (adjustedExcessM != cf.ExcessMale || adjustedExcessF != cf.ExcessFemale)
            {
                result.Add(cf with
                {
                    ExcessMale = adjustedExcessM,
                    TotalPensionMale = cf.TotalGmpMale + adjustedExcessM,
                    ExcessFemale = adjustedExcessF,
                    TotalPensionFemale = cf.TotalGmpFemale + adjustedExcessF,
                });
            }
            else
            {
                result.Add(cf);
            }
        }

        return result.AsReadOnly();
    }
}
