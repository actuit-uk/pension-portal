namespace PensionPortal.CalcLib.Internal;

/// <summary>
/// Calculates equalisation compensation by comparing the member's actual pension
/// against the opposite-sex scenario. Uses the Barber window to isolate the
/// compensable portion of post-88 GMP and excess pension separately.
/// Compensation accrues from the second PIP year onwards for each sex.
/// Uses signed differences (C2-style): years where the actual sex is better produce
/// negative compensation that offsets years where the opposite sex is better.
/// </summary>
internal static class CompensationCalculator
{
    /// <summary>
    /// Calculates year-by-year compensation entries and total compensation.
    /// </summary>
    /// <param name="cashFlow">Cash flow projection (from CashFlowBuilder).</param>
    /// <param name="memberSex">The member's actual sex.</param>
    /// <param name="scheme">Scheme configuration (assumptions for fallback discount rate).</param>
    /// <param name="factors">Factor provider for discount rate lookups.</param>
    /// <param name="barberGmpProportion">Fraction of post-88 GMP in the Barber window (0-1).</param>
    /// <param name="barberServiceProportion">Fraction of service in the Barber window (0-1). Scales excess comparison.</param>
    internal static (IReadOnlyList<CompensationEntry> Entries, decimal Total) Calculate(
        IReadOnlyList<CashFlowEntry> cashFlow,
        Sex memberSex,
        SchemeConfig scheme,
        IFactorProvider factors,
        decimal barberGmpProportion = 1m,
        decimal barberServiceProportion = 1m)
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

            // Only count from the second PIP year (first PIP year establishes the base)
            bool actualActive = actualInPip && actualWasInPip;
            bool oppSexActive = oppSexInPip && oppSexWasInPip;

            // Post-88 GMP: scaled by Barber GMP proportion (excludes pre-88 and non-Barber post-88)
            decimal actualPost88 = actualActive
                ? Math.Round((memberSex == Sex.Male ? cf.Post88GmpMale : cf.Post88GmpFemale) * barberGmpProportion, 2)
                : 0m;
            decimal oppSexPost88 = oppSexActive
                ? Math.Round((memberSex == Sex.Male ? cf.Post88GmpFemale : cf.Post88GmpMale) * barberGmpProportion, 2)
                : 0m;

            // Excess pension: scaled by Barber service proportion
            decimal actualExcess = actualActive
                ? Math.Round((memberSex == Sex.Male ? cf.ExcessMale : cf.ExcessFemale) * barberServiceProportion, 2)
                : 0m;
            decimal oppSexExcess = oppSexActive
                ? Math.Round((memberSex == Sex.Male ? cf.ExcessFemale : cf.ExcessMale) * barberServiceProportion, 2)
                : 0m;

            // Total compensable pension = Barber-scaled post-88 GMP + Barber-scaled excess
            decimal actualTotal = actualPost88 + actualExcess;
            decimal oppSexTotal = oppSexPost88 + oppSexExcess;

            // Compensation: positive = opposite-sex better, negative = actual sex better.
            decimal comp = oppSexTotal - actualTotal;

            // Discount rate (tracked for future PV calculations)
            decimal rate = factors.GetDiscountRate(cf.TaxYear) ?? scheme.Assumptions.FutureDiscountRate;

            if (i > 0)
                cumulativeDiscountFactor = Math.Round(
                    cumulativeDiscountFactor / (1m + rate), 5);

            totalCompensation += comp;

            entries.Add(new CompensationEntry(
                TaxYear: cf.TaxYear,
                ActualCashFlow: actualTotal,
                OppSexCashFlow: oppSexTotal,
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
