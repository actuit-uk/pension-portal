namespace PensionPortal.CalcLib;

/// <summary>
/// Calculates age-related values from dates of birth.
/// </summary>
public static class AgeCalculator
{
    /// <summary>
    /// Returns the number of complete months between a date of birth
    /// and a calculation date.
    /// </summary>
    /// <param name="dateOfBirth">The person's date of birth.</param>
    /// <param name="calculationDate">The date to calculate age at.</param>
    /// <returns>Age in complete months. A month is incomplete if the
    /// calculation date day is earlier than the birth day.</returns>
    public static int CompleteMonths(DateTime dateOfBirth, DateTime calculationDate)
    {
        int months = (calculationDate.Year - dateOfBirth.Year) * 12
                   + (calculationDate.Month - dateOfBirth.Month);

        if (calculationDate.Day < dateOfBirth.Day)
            months--;

        return months;
    }
}
