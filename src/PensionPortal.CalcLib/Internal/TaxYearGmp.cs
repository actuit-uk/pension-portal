namespace PensionPortal.CalcLib.Internal;

/// <summary>
/// Calculates GMP for a single tax year's earnings.
/// Mirrors spGMPForTaxYear in the legacy GMPEQ database.
/// </summary>
internal static class TaxYearGmp
{
    /// <summary>
    /// NI contribution divisor for pre-1988 tax years (6.85%).
    /// Contributions are divided by this to derive the earnings factor.
    /// </summary>
    internal const decimal NiDivisor = 0.0685m;

    /// <summary>
    /// Accrual rate for GMP earned before 6 April 1988 (25%).
    /// </summary>
    internal const decimal Pre88AccrualRate = 0.25m;

    /// <summary>
    /// Accrual rate for GMP earned from 6 April 1988 onwards (20%).
    /// </summary>
    internal const decimal Post88AccrualRate = 0.20m;

    /// <summary>
    /// Last tax year using contracted-out NI contributions (1987 = 1986/87 tax year).
    /// From 1988 onwards, band earnings are used directly.
    /// </summary>
    internal const int LastNIContributionYear = 1987;

    /// <summary>
    /// Last tax year with pre-88 accrual rate (1988 = 1987/88 tax year).
    /// The 1987/88 tax year uses band earnings but still accrues at 25%.
    /// Post-88 accrual (20%) starts from tax year 1989.
    /// </summary>
    internal const int LastPre88AccrualYear = 1988;

    /// <summary>
    /// Returns true if this tax year contributes to pre-88 GMP totals.
    /// </summary>
    internal static bool IsPre88(int taxYear) => taxYear <= LastPre88AccrualYear;

    /// <summary>
    /// Calculates the raw GMP contribution from a single tax year's earnings
    /// for both male and female working lives, returning a full audit trail.
    /// </summary>
    /// <param name="earningsOrNICs">The earnings (post-87) or contracted-out NICs (pre-88) for this tax year.</param>
    /// <param name="taxYearOfEarnings">The tax year the earnings relate to.</param>
    /// <param name="taxYearOfCalculation">The tax year used for S148 factor lookup (typically tax year of leaving).</param>
    /// <param name="workingLifeMale">Working life in years for male calculation.</param>
    /// <param name="workingLifeFemale">Working life in years for female calculation.</param>
    /// <param name="factors">Factor provider for S148 earnings revaluation lookup.</param>
    /// <returns>TaxYearDetail with all intermediate values and male/female GMP contributions.</returns>
    internal static TaxYearDetail Calculate(
        decimal earningsOrNICs,
        int taxYearOfEarnings,
        int taxYearOfCalculation,
        int workingLifeMale,
        int workingLifeFemale,
        IFactorProvider factors)
    {
        if (earningsOrNICs <= 0)
        {
            return new TaxYearDetail(
                TaxYear: taxYearOfEarnings,
                EarningsOrNICs: earningsOrNICs,
                IsNICs: taxYearOfEarnings <= LastNIContributionYear,
                IsPre88: IsPre88(taxYearOfEarnings),
                Divisor: taxYearOfEarnings <= LastNIContributionYear ? NiDivisor : 1.0m,
                AccrualRate: IsPre88(taxYearOfEarnings) ? Pre88AccrualRate : Post88AccrualRate,
                S148FactorPct: 0m,
                RevaluedEarnings: 0m,
                RawGmpMale: 0m,
                RawGmpFemale: 0m);
        }

        // NI contributions use the NI divisor; band earnings (from 1988) pass through at 1.0
        bool isNICs = taxYearOfEarnings <= LastNIContributionYear;
        decimal divisor = isNICs ? NiDivisor : 1.0m;

        // Accrual rate: 25% for years up to and including 1988, 20% from 1989
        bool isPre88 = IsPre88(taxYearOfEarnings);
        decimal accrualRate = isPre88 ? Pre88AccrualRate : Post88AccrualRate;

        // Look up S148 earnings revaluation factor
        decimal earningsFactor = factors.GetEarningsRevaluationFactor(
            taxYearOfEarnings, taxYearOfCalculation) ?? 0m;

        // Calculate revalued earnings factor:
        // (earnings / NI divisor) * (1 + factor%)
        // For post-88, divisor is 1.0 so earnings pass through directly
        decimal revaluedEarnings = Math.Round(
            earningsOrNICs / divisor * (1m + earningsFactor / 100m), 0);

        // Apply accrual rate and divide by working life
        decimal gmpMale = revaluedEarnings * accrualRate / workingLifeMale;
        decimal gmpFemale = revaluedEarnings * accrualRate / workingLifeFemale;

        return new TaxYearDetail(
            TaxYear: taxYearOfEarnings,
            EarningsOrNICs: earningsOrNICs,
            IsNICs: isNICs,
            IsPre88: isPre88,
            Divisor: divisor,
            AccrualRate: accrualRate,
            S148FactorPct: earningsFactor,
            RevaluedEarnings: revaluedEarnings,
            RawGmpMale: gmpMale,
            RawGmpFemale: gmpFemale);
    }
}
