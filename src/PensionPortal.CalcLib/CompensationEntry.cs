namespace PensionPortal.CalcLib;

/// <summary>
/// One year of the equalisation compensation calculation.
/// Compares actual GMP cash flow against the opposite-sex calculation
/// to determine the compensation due.
/// </summary>
/// <param name="TaxYear">The tax year this entry relates to.</param>
/// <param name="ActualCashFlow">GMP cash flow based on the member's actual sex.</param>
/// <param name="OppSexCashFlow">GMP cash flow based on the opposite sex.</param>
/// <param name="CompensationCashFlow">Difference: opposite sex minus actual (positive = compensation due).</param>
/// <param name="DiscountRate">Discount rate applied for this year.</param>
/// <param name="DiscountFactor">Cumulative discount factor for present-value calculation.</param>
public record CompensationEntry(
    int TaxYear,
    decimal ActualCashFlow,
    decimal OppSexCashFlow,
    decimal CompensationCashFlow,
    decimal DiscountRate,
    decimal DiscountFactor);
