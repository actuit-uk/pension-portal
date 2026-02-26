namespace PensionPortal.CalcLib;

/// <summary>
/// Scheme-level configuration for GMP equalisation.
/// Maps to tblSchemeInfo in the legacy GMPEQ database.
/// </summary>
/// <param name="PreEqNraMale">Normal retirement age for males before equalisation (typically 65).</param>
/// <param name="PreEqNraFemale">Normal retirement age for females before equalisation (typically 60).</param>
/// <param name="PostEqNra">Normal retirement age after equalisation (typically 65).</param>
/// <param name="DateOfEqualisation">Date the scheme equalised retirement ages.</param>
/// <param name="AccrualRateDenominator">Pension accrual denominator (e.g. 60 for 1/60ths, 80 for 1/80ths).</param>
/// <param name="PipMethod">Which pension increase table to use for excess pension.</param>
/// <param name="GmpRevMethod">GMP revaluation method.</param>
/// <param name="Assumptions">Future projection assumptions.</param>
/// <param name="AntiFrankingApplies">Whether to apply the anti-franking floor to the cash flow. Anti-franking prevents excess pension from being reduced to offset GMP revaluation increases. Only material with the overall increase method; has no effect with the separate increase method.</param>
public record SchemeConfig(
    int PreEqNraMale,
    int PreEqNraFemale,
    int PostEqNra,
    DateTime DateOfEqualisation,
    int AccrualRateDenominator,
    PipIncreaseMethod PipMethod,
    GmpRevaluationMethod GmpRevMethod,
    FutureAssumptions Assumptions,
    bool AntiFrankingApplies = false);
