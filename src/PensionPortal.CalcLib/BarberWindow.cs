namespace PensionPortal.CalcLib;

/// <summary>
/// Barber window helper. The Barber v Guardian Royal Exchange (1990) judgment
/// requires equalisation of pension benefits for service from 17 May 1990 to
/// the date the scheme equalised NRAs (or 5 Apr 1997, when GMP contracting-out
/// ceased, whichever is earlier). Only post-88 GMP accrued during this window
/// is eligible for equalisation compensation.
/// </summary>
public static class BarberWindow
{
    /// <summary>
    /// The Barber judgment date: 17 May 1990.
    /// </summary>
    public static readonly DateTime JudgmentDate = new(1990, 5, 17);

    /// <summary>
    /// The date GMP contracting-out ceased: 6 April 1997.
    /// The last day of contracted-out service is 5 April 1997 (tax year 1996).
    /// </summary>
    public static readonly DateTime ContractingOutEnd = new(1997, 4, 6);

    /// <summary>
    /// First tax year that falls (partially) within the Barber window.
    /// Tax year 1990 = 6 Apr 1990 to 5 Apr 1991, Barber date is 17 May 1990.
    /// </summary>
    public const int FirstTaxYear = 1990;

    /// <summary>
    /// Last tax year that falls within the Barber window.
    /// Tax year 1996 = 6 Apr 1996 to 5 Apr 1997 (last CO tax year).
    /// </summary>
    public const int LastTaxYear = 1996;

    /// <summary>
    /// Returns true if the given tax year falls within the Barber window
    /// (tax years 1990 to 1996 inclusive). Tax year 1990 is partial but included.
    /// </summary>
    public static bool IsInWindow(int taxYear) => taxYear >= FirstTaxYear && taxYear <= LastTaxYear;

    /// <summary>
    /// Calculates the proportion of pensionable service that falls within the Barber window.
    /// Used for apportioning excess pension to the Barber window period.
    /// </summary>
    /// <param name="dateCOStart">Start of contracted-out employment.</param>
    /// <param name="dateOfLeaving">Date of leaving pensionable service.</param>
    public static decimal CalculateServiceProportion(DateTime dateCOStart, DateTime dateOfLeaving)
    {
        double totalDays = (dateOfLeaving - dateCOStart).TotalDays;
        if (totalDays <= 0) return 0m;

        var barberEnd = new DateTime(1997, 4, 5); // 5 Apr 1997

        var overlapStart = dateCOStart > JudgmentDate ? dateCOStart : JudgmentDate;
        var overlapEnd = dateOfLeaving < barberEnd ? dateOfLeaving : barberEnd;

        double barberDays = Math.Max(0, (overlapEnd - overlapStart).TotalDays);
        return Math.Round((decimal)(barberDays / totalDays), 6);
    }

    /// <summary>
    /// Calculates the proportion of post-88 GMP that falls within the Barber window.
    /// Uses the per-tax-year GMP audit trail to sum Barber window years vs all post-88 years.
    /// Returns 1.0 if all post-88 GMP is within the window (no adjustment needed).
    /// Returns 0.0 if there is no post-88 GMP.
    /// </summary>
    /// <param name="taxYearDetails">Per-tax-year audit trail from GMP calculation.</param>
    public static decimal CalculateProportion(IReadOnlyList<TaxYearDetail> taxYearDetails)
    {
        decimal totalPost88 = 0m;
        decimal barberPost88 = 0m;

        foreach (var detail in taxYearDetails)
        {
            if (detail.IsPre88)
                continue;

            // Use revalued earnings as the proxy (proportional to GMP contribution,
            // and the same for both sexes since working life divides out)
            totalPost88 += detail.RevaluedEarnings;

            if (IsInWindow(detail.TaxYear))
                barberPost88 += detail.RevaluedEarnings;
        }

        if (totalPost88 == 0m)
            return 0m;

        return Math.Round(barberPost88 / totalPost88, 6);
    }
}
