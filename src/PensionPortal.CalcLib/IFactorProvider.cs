namespace PensionPortal.CalcLib;

/// <summary>
/// Provides actuarial factor lookups for GMP calculations.
/// Abstracts the factor data source so CalcLib has no database dependency.
/// Maps to tblFactorValues in the legacy GMPEQ database where
/// (FactorTableName, FactorIndex1, FactorIndex2) identifies a FactorValue.
/// </summary>
public interface IFactorProvider
{
    /// <summary>
    /// Returns the Section 148 earnings revaluation percentage for the given
    /// tax year of earnings, as at the given calculation tax year.
    /// For example, EarningsFactorOrder[1987, 2002] = 149.5 means 149.5%.
    /// Returns null if no factor is available.
    /// </summary>
    decimal? GetEarningsRevaluationFactor(int taxYearOfEarnings, int taxYearOfCalculation);

    /// <summary>
    /// Returns the fixed revaluation rate for a termination occurring in the
    /// given tax year. For example, termination in 2003 returns 0.045 (4.5%).
    /// </summary>
    decimal GetFixedRevaluationRate(int taxYearOfTermination);

    /// <summary>
    /// Returns the pension increase factor for the given method and tax year.
    /// For example, PIPIncLPI3[2019] = 0.024 (2.4%).
    /// Returns null if no factor is available for that year.
    /// </summary>
    decimal? GetPipIncreaseFactor(PipIncreaseMethod method, int taxYear);

    /// <summary>
    /// Returns the discount rate for the given tax year, used in
    /// compensation present-value calculations.
    /// Returns null if no rate is available for that year.
    /// </summary>
    decimal? GetDiscountRate(int taxYear);

    /// <summary>
    /// Returns the Bank of England base rate for the given tax year,
    /// used for interest on arrears calculations (base rate + 1%).
    /// Returns null if no rate is available for that year.
    /// </summary>
    decimal? GetBaseRate(int taxYear);
}
