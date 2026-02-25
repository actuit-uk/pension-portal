namespace PensionPortal.CalcLib;

/// <summary>
/// Complete GMP equalisation calculation output, comprising the GMP result,
/// year-by-year cash flow projection, compensation entries, and total compensation.
/// </summary>
/// <param name="Gmp">The underlying GMP calculation result.</param>
/// <param name="CashFlow">Year-by-year pension cash flow projection.</param>
/// <param name="Compensation">Year-by-year compensation entries.</param>
/// <param name="TotalCompensation">Sum of all compensation cash flows.</param>
public record EqualisationResult(
    GmpResult Gmp,
    IReadOnlyList<CashFlowEntry> CashFlow,
    IReadOnlyList<CompensationEntry> Compensation,
    decimal TotalCompensation);
