namespace PensionPortal.CalcLib.Internal;

/// <summary>
/// Calculates equalisation compensation by comparing the member's actual post-88 GMP
/// cash flow against the opposite-sex scenario. Compensation accrues from the second
/// PIP year onwards (first PIP year is the base before any increases apply).
/// Discount factors are tracked for present-value reporting but the primary total
/// is undiscounted (sum of annual compensation amounts).
/// </summary>
internal static class CompensationCalculator
{
    /// <summary>
    /// Calculates year-by-year compensation entries and total compensation.
    /// </summary>
    /// <param name="cashFlow">Cash flow projection (from CashFlowBuilder).</param>
    /// <param name="memberSex">The member's actual sex.</param>
    /// <param name="factors">Factor provider for discount rate lookups.</param>
    /// <param name="assumptions">Future assumptions (fallback discount rate).</param>
    internal static (IReadOnlyList<CompensationEntry> Entries, decimal Total) Calculate(
        IReadOnlyList<CashFlowEntry> cashFlow,
        Sex memberSex,
        IFactorProvider factors,
        FutureAssumptions assumptions)
    {
        var entries = new List<CompensationEntry>();
        decimal cumulativeDiscountFactor = 1m;
        decimal totalCompensation = 0m;

        // Track whether each sex was in PIP in the previous year
        // (compensation only counts from the second PIP year onwards)
        bool actualWasInPip = false;
        bool oppSexWasInPip = false;

        for (int i = 0; i < cashFlow.Count; i++)
        {
            var cf = cashFlow[i];

            // Determine current PIP status for each sex
            var actualStatus = memberSex == Sex.Male ? cf.StatusMale : cf.StatusFemale;
            var oppSexStatus = memberSex == Sex.Male ? cf.StatusFemale : cf.StatusMale;

            bool actualInPip = actualStatus == GmpStatus.InPayment;
            bool oppSexInPip = oppSexStatus == GmpStatus.InPayment;

            // Post-88 GMP amounts — only count from the second PIP year
            // (first PIP year establishes the base, no increase has been applied yet)
            decimal actualPost88 = (actualInPip && actualWasInPip)
                ? (memberSex == Sex.Male ? cf.Post88GmpMale : cf.Post88GmpFemale)
                : 0m;

            decimal oppSexPost88 = (oppSexInPip && oppSexWasInPip)
                ? (memberSex == Sex.Male ? cf.Post88GmpFemale : cf.Post88GmpMale)
                : 0m;

            // Compensation: positive means opposite-sex scenario is better
            decimal comp = Math.Max(0m, oppSexPost88 - actualPost88);

            // Discount rate (tracked for future PV calculations)
            decimal rate = factors.GetDiscountRate(cf.TaxYear) ?? assumptions.FutureDiscountRate;

            if (i > 0)
                cumulativeDiscountFactor = Math.Round(
                    cumulativeDiscountFactor / (1m + rate), 5);

            totalCompensation += comp;

            entries.Add(new CompensationEntry(
                TaxYear: cf.TaxYear,
                ActualCashFlow: actualPost88,
                OppSexCashFlow: oppSexPost88,
                CompensationCashFlow: comp,
                DiscountRate: rate,
                DiscountFactor: Math.Round(cumulativeDiscountFactor, 5)));

            // Update PIP tracking for next iteration
            actualWasInPip = actualInPip;
            oppSexWasInPip = oppSexInPip;
        }

        return (entries.AsReadOnly(), Math.Round(totalCompensation, 2));
    }
}
