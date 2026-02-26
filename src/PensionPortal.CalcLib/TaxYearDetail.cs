namespace PensionPortal.CalcLib;

/// <summary>
/// Audit record capturing all intermediate values for a single tax year's GMP calculation.
/// One record per tax year of earnings, providing full traceability.
/// </summary>
/// <param name="TaxYear">The tax year these earnings relate to.</param>
/// <param name="EarningsOrNICs">The raw earnings or NI contributions input.</param>
/// <param name="IsNICs">True if this year uses NI contributions (up to 1987), false for band earnings.</param>
/// <param name="IsPre88">True if this year accrues at the pre-88 rate (up to 1988).</param>
/// <param name="Divisor">NI divisor (0.0685 for NICs years) or 1.0 for band earnings years.</param>
/// <param name="AccrualRate">Accrual rate applied: 0.25 for pre-88, 0.20 for post-88.</param>
/// <param name="S148FactorPct">S148 earnings revaluation factor as a percentage (e.g. 149.5).</param>
/// <param name="RevaluedEarnings">Earnings after applying divisor and S148 factor, rounded to nearest pound.</param>
/// <param name="RawGmpMale">Unrounded annual GMP contribution for male working life.</param>
/// <param name="RawGmpFemale">Unrounded annual GMP contribution for female working life.</param>
public record TaxYearDetail(
    int TaxYear,
    decimal EarningsOrNICs,
    bool IsNICs,
    bool IsPre88,
    decimal Divisor,
    decimal AccrualRate,
    decimal S148FactorPct,
    decimal RevaluedEarnings,
    decimal RawGmpMale,
    decimal RawGmpFemale);
