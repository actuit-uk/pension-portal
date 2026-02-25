namespace PensionPortal.CalcLib.Internal;

/// <summary>
/// Calculates working life years for GMP purposes.
/// Working life starts at the later of 6 April 1978 or the 6 April on or
/// immediately before the 16th birthday, and ends at the 5 April before
/// GMP payable age (65 for men, 60 for women).
/// </summary>
internal static class WorkingLife
{
    /// <summary>
    /// Returns the number of years in the working life.
    /// </summary>
    /// <param name="dateOfBirth">The member's date of birth.</param>
    /// <param name="gmpPayableAge">GMP payable age (65 for male, 60 for female).</param>
    internal static int Calculate(DateTime dateOfBirth, int gmpPayableAge)
    {
        int startYear = TaxYearStartYear(dateOfBirth);
        int endYear = TaxYearOfGmpPayableAge(dateOfBirth, gmpPayableAge);

        return endYear - startYear;
    }

    /// <summary>
    /// Returns the tax year in which the working life starts.
    /// This is the later of 1978 or the tax year containing the 16th birthday
    /// (i.e. the tax year of the 6 April on or immediately before the 16th birthday).
    /// </summary>
    private static int TaxYearStartYear(DateTime dateOfBirth)
    {
        // The tax year containing the 16th birthday
        int sixteenthBirthdayYear = dateOfBirth.Year + 16;
        var sixteenthBirthday = new DateTime(sixteenthBirthdayYear, dateOfBirth.Month, dateOfBirth.Day);
        int taxYear = TaxYearHelper.TaxYearFromDate(sixteenthBirthday);

        // Cannot start before 1978
        return Math.Max(taxYear, 1978);
    }

    /// <summary>
    /// Returns the tax year before GMP payable age is reached.
    /// GMP payable age is 65 for men, 60 for women.
    /// </summary>
    private static int TaxYearOfGmpPayableAge(DateTime dateOfBirth, int gmpPayableAge)
    {
        int gmpAgeYear = dateOfBirth.Year + gmpPayableAge;
        var gmpAgeDate = new DateTime(gmpAgeYear, dateOfBirth.Month, dateOfBirth.Day);
        int taxYear = TaxYearHelper.TaxYearFromDate(gmpAgeDate);

        // We want the tax year BEFORE GMP payable age
        return taxYear;
    }
}
