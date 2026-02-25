namespace PensionPortal.CalcLib;

/// <summary>
/// Utility methods for UK tax year calculations.
/// A UK tax year runs from 6 April to 5 April and is identified
/// by the calendar year in which it starts (e.g. 2002 = 6 Apr 2002 to 5 Apr 2003).
/// </summary>
public static class TaxYearHelper
{
    /// <summary>
    /// Returns the tax year that contains the given date.
    /// Dates on or after 6 April belong to that calendar year's tax year.
    /// Dates before 6 April belong to the previous calendar year's tax year.
    /// </summary>
    /// <example>
    /// 10 Nov 2002 → 2002, 3 Mar 2003 → 2002, 6 Apr 2003 → 2003, 5 Apr 2003 → 2002
    /// </example>
    public static int TaxYearFromDate(DateTime date)
    {
        if (date.Month > 4 || (date.Month == 4 && date.Day >= 6))
            return date.Year;

        return date.Year - 1;
    }

    /// <summary>
    /// Returns 6 April of the given tax year.
    /// </summary>
    public static DateTime StartOfTaxYear(int taxYear)
    {
        return new DateTime(taxYear, 4, 6);
    }

    /// <summary>
    /// Returns 5 April of the year following the given tax year.
    /// </summary>
    public static DateTime EndOfTaxYear(int taxYear)
    {
        return new DateTime(taxYear + 1, 4, 5);
    }
}
