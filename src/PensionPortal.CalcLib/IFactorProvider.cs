namespace PensionPortal.CalcLib;

/// <summary>
/// Provides actuarial factor lookups for GMP calculations.
/// Abstracts the factor data source so CalcLib has no database dependency.
/// Maps to tblFactorValues in the legacy GMPEQ database where
/// (FactorTableName, FactorIndex1, FactorIndex2) identifies a FactorValue.
///
/// Stochastic engine notes: In CalcLib all factors are deterministic lookups.
/// In the stochastic engine (pension-engine), some factors become simulation-dependent
/// for future projection years:
/// - S148 earnings revaluation: always deterministic (historical only)
/// - Fixed/limited revaluation rate: always deterministic (set at termination)
/// - Post-88 GMP increase (LPI3): deterministic for historical years, stochastic for
///   future years (simulated as min(CPI_sim, 3%))
/// - Excess PIP increase (LPI5): deterministic for historical years, stochastic for
///   future years (simulated as min(RPI_sim, 5%) or scheme-specific rate)
/// - Discount rate: deterministic here, possibly correlated with inflation in stochastic model
/// - Base rate: deterministic (historical BoE rates)
/// </summary>
public interface IFactorProvider
{
    /// <summary>
    /// Returns the Section 148 earnings revaluation percentage for the given
    /// tax year of earnings, as at the given calculation tax year.
    /// For example, EarningsFactorOrder[1987, 2002] = 149.5 means 149.5%.
    /// Returns null if no factor is available.
    /// Deterministic only — S148 orders are published historical data.
    /// </summary>
    decimal? GetEarningsRevaluationFactor(int taxYearOfEarnings, int taxYearOfCalculation);

    /// <summary>
    /// Returns the fixed revaluation rate for a termination occurring in the
    /// given tax year. For example, termination in 2003 returns 0.045 (4.5%).
    /// Deterministic only — fixed rates are set at termination date.
    /// </summary>
    decimal GetFixedRevaluationRate(int taxYearOfTermination);

    /// <summary>
    /// Returns the pension increase factor for the given method and tax year.
    /// For example, PIPIncLPI3[2019] = 0.024 (2.4%).
    /// Returns null if no factor is available for that year.
    /// Stochastic candidate: for future years, LPI3 = min(CPI, 3%) and
    /// LPI5 = min(RPI, 5%) become simulation-dependent in the stochastic engine.
    /// </summary>
    decimal? GetPipIncreaseFactor(PipIncreaseMethod method, int taxYear);

    /// <summary>
    /// Returns the discount rate for the given tax year, used in
    /// compensation present-value calculations.
    /// Returns null if no rate is available for that year.
    /// Stochastic candidate: may be correlated with inflation in stochastic model.
    /// </summary>
    decimal? GetDiscountRate(int taxYear);

    /// <summary>
    /// Returns the Bank of England base rate for the given tax year,
    /// used for interest on arrears calculations (base rate + 1%).
    /// Returns null if no rate is available for that year.
    /// Deterministic only — historical BoE published rates.
    /// </summary>
    decimal? GetBaseRate(int taxYear);
}
