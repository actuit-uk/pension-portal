namespace PensionPortal.CalcLib;

/// <summary>
/// Assumed rates for projecting pensions beyond the last known factor year.
/// </summary>
/// <param name="FuturePost88GmpIncRate">Annual increase rate for post-1988 GMP in payment (e.g. 0.025 = 2.5%).</param>
/// <param name="FuturePipRate">Annual pension increase rate for excess pension (e.g. 0.025 = 2.5%).</param>
/// <param name="FutureDiscountRate">Annual discount rate for compensation present-value calculation.</param>
/// <param name="ProjectionEndYear">Last tax year to include in the cash flow projection.</param>
public record FutureAssumptions(
    decimal FuturePost88GmpIncRate,
    decimal FuturePipRate,
    decimal FutureDiscountRate,
    int ProjectionEndYear);
