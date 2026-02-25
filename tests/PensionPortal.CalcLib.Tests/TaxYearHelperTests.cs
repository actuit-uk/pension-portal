using PensionPortal.CalcLib;

namespace PensionPortal.CalcLib.Tests;

public class TaxYearHelperTests
{
    [Theory]
    [InlineData(2002, 11, 10, 2002)] // Nov in same tax year
    [InlineData(2003, 3, 3, 2002)]   // Mar before 6 Apr = previous tax year
    [InlineData(2003, 4, 5, 2002)]   // 5 Apr = still previous tax year
    [InlineData(2003, 4, 6, 2003)]   // 6 Apr = new tax year
    [InlineData(2003, 4, 7, 2003)]   // 7 Apr = new tax year
    [InlineData(1986, 4, 6, 1986)]   // CO start date
    [InlineData(1951, 12, 29, 1951)] // DOB in Dec
    public void TaxYearFromDate_ReturnsCorrectTaxYear(int year, int month, int day, int expected)
    {
        var date = new DateTime(year, month, day);
        Assert.Equal(expected, TaxYearHelper.TaxYearFromDate(date));
    }

    [Fact]
    public void StartOfTaxYear_Returns6April()
    {
        var start = TaxYearHelper.StartOfTaxYear(2002);
        Assert.Equal(new DateTime(2002, 4, 6), start);
    }

    [Fact]
    public void EndOfTaxYear_Returns5AprilNextYear()
    {
        var end = TaxYearHelper.EndOfTaxYear(2002);
        Assert.Equal(new DateTime(2003, 4, 5), end);
    }
}
