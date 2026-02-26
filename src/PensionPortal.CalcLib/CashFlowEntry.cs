namespace PensionPortal.CalcLib;

/// <summary>
/// One tax year of the pension cash flow projection.
/// Tracks GMP components and excess pension for both male and female
/// calculations side by side, enabling equalisation comparison.
/// </summary>
/// <param name="TaxYear">The tax year this entry relates to.</param>
/// <param name="StatusMale">GMP phase for the male calculation this year.</param>
/// <param name="Pre88GmpMale">Pre-1988 GMP annual amount (male). Flat until PIP, then stays flat.</param>
/// <param name="Post88GmpMale">Post-1988 GMP annual amount (male). Flat in DEF, increases at LPI in PIP.</param>
/// <param name="TotalGmpMale">Total GMP annual amount (male). Pre88 + Post88.</param>
/// <param name="ExcessMale">Excess pension above GMP (male). Scheme-dependent, increases at PIP rate.</param>
/// <param name="TotalPensionMale">Total pension payable (male). GMP + Excess.</param>
/// <param name="StatusFemale">GMP phase for the female calculation this year.</param>
/// <param name="Pre88GmpFemale">Pre-1988 GMP annual amount (female).</param>
/// <param name="Post88GmpFemale">Post-1988 GMP annual amount (female).</param>
/// <param name="TotalGmpFemale">Total GMP annual amount (female).</param>
/// <param name="ExcessFemale">Excess pension above GMP (female).</param>
/// <param name="TotalPensionFemale">Total pension payable (female).</param>
/// <param name="Post88GmpIncFactor">Post-1988 GMP increase factor applied this year (LPI rate).</param>
/// <param name="ExcessIncFactor">Pension increase factor applied to excess pension this year.</param>
public record CashFlowEntry(
    int TaxYear,
    GmpStatus StatusMale,
    decimal Pre88GmpMale,
    decimal Post88GmpMale,
    decimal TotalGmpMale,
    decimal ExcessMale,
    decimal TotalPensionMale,
    GmpStatus StatusFemale,
    decimal Pre88GmpFemale,
    decimal Post88GmpFemale,
    decimal TotalGmpFemale,
    decimal ExcessFemale,
    decimal TotalPensionFemale,
    decimal Post88GmpIncFactor,
    decimal ExcessIncFactor);
