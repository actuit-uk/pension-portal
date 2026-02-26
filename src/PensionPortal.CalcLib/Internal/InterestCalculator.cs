namespace PensionPortal.CalcLib.Internal;

/// <summary>
/// Calculates simple interest on compensation arrears.
/// HMRC confirmed: interest rate is Bank of England base rate + 1%,
/// calculated as simple interest (not compound) from each year's
/// compensation payment date to the settlement date.
/// </summary>
internal static class InterestCalculator
{
    /// <summary>
    /// Calculates total simple interest on past compensation arrears.
    /// Only positive compensation amounts (years where the member was disadvantaged)
    /// accrue interest. Future years (at or after settlement) are excluded.
    /// </summary>
    /// <param name="compensation">Year-by-year compensation entries.</param>
    /// <param name="settlementTaxYear">The tax year of settlement.</param>
    /// <param name="factors">Factor provider for base rate lookups.</param>
    /// <param name="fallbackBaseRate">Base rate to use when no factor data is available.</param>
    internal static decimal Calculate(
        IReadOnlyList<CompensationEntry> compensation,
        int settlementTaxYear,
        IFactorProvider factors,
        decimal fallbackBaseRate)
    {
        decimal totalInterest = 0m;

        foreach (var entry in compensation)
        {
            // No interest on future or settlement-year compensation
            if (entry.TaxYear >= settlementTaxYear)
                continue;

            // Only accrue interest on positive compensation (arrears owed to member)
            if (entry.CompensationCashFlow <= 0m)
                continue;

            decimal baseRate = factors.GetBaseRate(entry.TaxYear) ?? fallbackBaseRate;
            decimal interestRate = baseRate + 0.01m; // Base rate + 1%
            int years = settlementTaxYear - entry.TaxYear;

            // Simple interest: principal × rate × time
            totalInterest += Math.Round(entry.CompensationCashFlow * interestRate * years, 2);
        }

        return Math.Round(totalInterest, 2);
    }
}
