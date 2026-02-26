namespace PensionPortal.CalcLib.Internal;

/// <summary>
/// Calculates excess pension above GMP using a three-tier fallback:
/// 1. Direct PensionAtLeaving — if the total scheme pension is known
/// 2. Salary-based estimate — FinalPensionableSalary × service ÷ accrual denominator
/// 3. GMP-only — excess is zero (valid for GMP-only schemes)
/// The excess is the same for both male and female calculations (it's the scheme pension
/// independent of GMP sex differences).
/// </summary>
internal static class ExcessPensionCalculator
{
    /// <summary>
    /// Calculates the excess pension at date of leaving for each sex.
    /// Excess = total scheme pension - total GMP (sex-specific since GMP differs by sex).
    /// Returns (excessMale, excessFemale) as annual amounts. Never negative (floored at 0).
    /// </summary>
    /// <param name="member">Member data (may include PensionAtLeaving or FinalPensionableSalary).</param>
    /// <param name="scheme">Scheme configuration (accrual denominator for salary fallback).</param>
    /// <param name="totalGmpMaleAnnual">Total GMP at leaving for male calculation.</param>
    /// <param name="totalGmpFemaleAnnual">Total GMP at leaving for female calculation.</param>
    internal static (decimal ExcessMale, decimal ExcessFemale) Calculate(
        MemberData member,
        SchemeConfig scheme,
        decimal totalGmpMaleAnnual,
        decimal totalGmpFemaleAnnual)
    {
        decimal? totalPension = ResolveTotalPension(member, scheme);

        if (totalPension is null)
            return (0m, 0m); // Tier 3: GMP-only

        decimal excessM = Math.Max(0m, Math.Round(totalPension.Value - totalGmpMaleAnnual, 2));
        decimal excessF = Math.Max(0m, Math.Round(totalPension.Value - totalGmpFemaleAnnual, 2));

        return (excessM, excessF);
    }

    /// <summary>
    /// Resolves the total scheme pension using the three-tier hierarchy.
    /// Returns null if neither PensionAtLeaving nor FinalPensionableSalary is available.
    /// </summary>
    private static decimal? ResolveTotalPension(MemberData member, SchemeConfig scheme)
    {
        // Tier 1: direct pension at leaving
        if (member.PensionAtLeaving.HasValue)
            return member.PensionAtLeaving.Value;

        // Tier 2: salary-based estimate
        if (member.FinalPensionableSalary.HasValue)
        {
            decimal service = PensionableServiceYears(member);
            decimal estimated = Math.Round(
                member.FinalPensionableSalary.Value * service / scheme.AccrualRateDenominator, 2);
            return estimated;
        }

        // Tier 3: no excess data available
        return null;
    }

    /// <summary>
    /// Calculates pensionable service in complete years from CO start to date of leaving.
    /// </summary>
    private static decimal PensionableServiceYears(MemberData member)
    {
        var days = (member.DateOfLeaving - member.DateCOStart).TotalDays;
        return Math.Round((decimal)(days / 365.25), 2);
    }
}
