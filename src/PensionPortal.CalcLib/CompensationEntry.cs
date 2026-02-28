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
/// <param name="RawDifference">OppSex minus Actual before Barber scaling. For cross-engine verification.</param>
/// <param name="BarberGmpProportion">Barber window proportion applied to post-88 GMP (0-1).</param>
/// <param name="BarberServiceProportion">Barber window proportion applied to excess pension (0-1).</param>
/// <param name="InterestRate">BoE base rate + 1% for this year (populated when settlement date provided).</param>
/// <param name="InterestAmount">Simple interest accrued for this year (populated when settlement date provided).</param>
public record CompensationEntry(
    int TaxYear,
    decimal ActualCashFlow,
    decimal OppSexCashFlow,
    decimal CompensationCashFlow,
    decimal DiscountRate,
    decimal DiscountFactor,
    decimal RawDifference = 0m,
    decimal BarberGmpProportion = 1m,
    decimal BarberServiceProportion = 1m,
    decimal InterestRate = 0m,
    decimal InterestAmount = 0m);
