namespace PensionPortal.CalcLib;

/// <summary>
/// Complete GMP calculation result including at-leaving and revalued amounts
/// for both male and female, plus per-tax-year audit trail.
/// </summary>
/// <param name="WorkingLifeMale">Working life in years for male GMP calculation.</param>
/// <param name="WorkingLifeFemale">Working life in years for female GMP calculation.</param>
/// <param name="TaxYearOfLeaving">Tax year in which the member left contracted-out employment.</param>
/// <param name="MaleAtLeaving">GMP breakdown calculated as male at date of leaving.</param>
/// <param name="FemaleAtLeaving">GMP breakdown calculated as female at date of leaving.</param>
/// <param name="MaleRevalued">GMP breakdown revalued to male GMP payable age (65).</param>
/// <param name="FemaleRevalued">GMP breakdown revalued to female GMP payable age (60).</param>
/// <param name="RevaluationMethod">The revaluation method used.</param>
/// <param name="RevaluationFactorMale">Revaluation factor applied to male GMP.</param>
/// <param name="RevaluationFactorFemale">Revaluation factor applied to female GMP.</param>
/// <param name="TaxYearDetails">Per-tax-year audit trail of intermediate calculation values.</param>
public record GmpResult(
    int WorkingLifeMale,
    int WorkingLifeFemale,
    int TaxYearOfLeaving,
    GmpBreakdown MaleAtLeaving,
    GmpBreakdown FemaleAtLeaving,
    GmpBreakdown MaleRevalued,
    GmpBreakdown FemaleRevalued,
    GmpRevaluationMethod RevaluationMethod,
    decimal RevaluationFactorMale,
    decimal RevaluationFactorFemale,
    IReadOnlyList<TaxYearDetail> TaxYearDetails);
