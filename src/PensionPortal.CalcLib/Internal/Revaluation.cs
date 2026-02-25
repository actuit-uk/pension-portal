namespace PensionPortal.CalcLib.Internal;

/// <summary>
/// Calculates the GMP revaluation factor from date of leaving to GMP payable age.
/// Supports Section 148, Fixed Rate, and Limited Rate methods.
/// </summary>
internal static class Revaluation
{
    /// <summary>
    /// Limited rate revaluation compound percentage (5%).
    /// Only available for members who left before 6 April 1997.
    /// </summary>
    private const decimal LimitedRate = 0.05m;

    /// <summary>
    /// Calculates the revaluation factor to apply to GMP at date of leaving
    /// to obtain GMP at GMP payable age.
    /// </summary>
    /// <param name="method">The revaluation method to use.</param>
    /// <param name="taxYearOfLeaving">Tax year in which the member left.</param>
    /// <param name="taxYearBeforeGmpAge">The last complete tax year before GMP payable age.</param>
    /// <param name="factors">Factor provider for lookups.</param>
    /// <returns>
    /// The revaluation factor as a multiplier (e.g. 1.422 means 42.2% increase).
    /// </returns>
    internal static decimal CalculateFactor(
        GmpRevaluationMethod method,
        int taxYearOfLeaving,
        int taxYearBeforeGmpAge,
        IFactorProvider factors)
    {
        return method switch
        {
            GmpRevaluationMethod.Section148 =>
                CalculateSection148(taxYearOfLeaving, taxYearBeforeGmpAge, factors),
            GmpRevaluationMethod.FixedRate =>
                CalculateFixed(taxYearOfLeaving, taxYearBeforeGmpAge, factors),
            GmpRevaluationMethod.LimitedRate =>
                CalculateLimited(taxYearOfLeaving, taxYearBeforeGmpAge),
            _ => throw new ArgumentOutOfRangeException(nameof(method))
        };
    }

    /// <summary>
    /// Section 148: single factor lookup from the S148 order.
    /// Look up from the tax year after leaving to the tax year before GMP age.
    /// </summary>
    private static decimal CalculateSection148(
        int taxYearOfLeaving, int taxYearBeforeGmpAge, IFactorProvider factors)
    {
        int fromYear = taxYearOfLeaving + 1;
        decimal percentage = factors.GetEarningsRevaluationFactor(fromYear, taxYearBeforeGmpAge) ?? 0m;
        return 1m + percentage / 100m;
    }

    /// <summary>
    /// Fixed rate: compound (1 + rate) ^ years.
    /// Rate depends on the termination date band.
    /// </summary>
    private static decimal CalculateFixed(
        int taxYearOfLeaving, int taxYearBeforeGmpAge, IFactorProvider factors)
    {
        decimal rate = factors.GetFixedRevaluationRate(taxYearOfLeaving);
        int years = taxYearBeforeGmpAge - taxYearOfLeaving;
        return Power(1m + rate, years);
    }

    /// <summary>
    /// Limited rate: 5% compound for each year.
    /// </summary>
    private static decimal CalculateLimited(int taxYearOfLeaving, int taxYearBeforeGmpAge)
    {
        int years = taxYearBeforeGmpAge - taxYearOfLeaving;
        return Power(1m + LimitedRate, years);
    }

    /// <summary>
    /// Raises a decimal base to an integer power.
    /// </summary>
    private static decimal Power(decimal baseValue, int exponent)
    {
        if (exponent <= 0) return 1m;
        decimal result = 1m;
        for (int i = 0; i < exponent; i++)
            result *= baseValue;
        return result;
    }
}
