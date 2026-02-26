namespace PensionPortal.CalcLib;

/// <summary>
/// Complete GMP equalisation calculation output, comprising the GMP result,
/// year-by-year cash flow projection, compensation entries, total compensation,
/// and interest on arrears.
/// </summary>
/// <param name="Gmp">The underlying GMP calculation result.</param>
/// <param name="CashFlow">Year-by-year pension cash flow projection.</param>
/// <param name="Compensation">Year-by-year compensation entries.</param>
/// <param name="TotalCompensation">Sum of all compensation cash flows.</param>
/// <param name="InterestOnArrears">Simple interest on past compensation arrears (base rate + 1%). Zero if no settlement date provided.</param>
/// <param name="TotalWithInterest">TotalCompensation + InterestOnArrears.</param>
public record EqualisationResult(
    GmpResult Gmp,
    IReadOnlyList<CashFlowEntry> CashFlow,
    IReadOnlyList<CompensationEntry> Compensation,
    decimal TotalCompensation,
    decimal InterestOnArrears = 0m,
    decimal TotalWithInterest = 0m);
