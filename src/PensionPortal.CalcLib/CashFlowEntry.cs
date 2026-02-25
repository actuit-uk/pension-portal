namespace PensionPortal.CalcLib;

/// <summary>
/// One year of the pension cash flow projection, showing GMP amounts
/// for both male and female calculations and the applicable increase factors.
/// </summary>
/// <param name="TaxYear">The tax year this entry relates to.</param>
/// <param name="Pre88Male">Pre-1988 GMP annual amount (male calculation).</param>
/// <param name="Post88Male">Post-1988 GMP annual amount (male calculation).</param>
/// <param name="TotalMale">Total GMP annual amount (male calculation).</param>
/// <param name="Pre88Female">Pre-1988 GMP annual amount (female calculation).</param>
/// <param name="Post88Female">Post-1988 GMP annual amount (female calculation).</param>
/// <param name="TotalFemale">Total GMP annual amount (female calculation).</param>
/// <param name="Post88GmpIncFactor">Post-1988 GMP increase factor applied this year.</param>
/// <param name="PipIncFactor">Pension increase factor applied to excess pension this year.</param>
public record CashFlowEntry(
    int TaxYear,
    decimal Pre88Male,
    decimal Post88Male,
    decimal TotalMale,
    decimal Pre88Female,
    decimal Post88Female,
    decimal TotalFemale,
    decimal Post88GmpIncFactor,
    decimal PipIncFactor);
