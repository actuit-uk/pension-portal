namespace PensionPortal.CalcLib;

/// <summary>
/// Method used to revalue GMP from date of leaving to GMP payable age.
/// </summary>
public enum GmpRevaluationMethod
{
    /// <summary>
    /// Full revaluation using Section 148 orders.
    /// Single factor lookup from the relevant S148 order.
    /// </summary>
    Section148,

    /// <summary>
    /// Fixed rate revaluation. Compound percentage applied
    /// for each tax year between leaving and GMP payable age.
    /// Rate depends on the termination date band.
    /// </summary>
    FixedRate,

    /// <summary>
    /// Limited rate revaluation (5% compound).
    /// Only available for members who left contracted-out
    /// employment before 6 April 1997.
    /// </summary>
    LimitedRate
}
