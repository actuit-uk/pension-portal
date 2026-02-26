using PensionPortal.CalcLib;
using PensionPortal.CalcLib.Tests.TestData;

namespace PensionPortal.CalcLib.Tests;

public class BarberWindowTests
{
    [Theory]
    [InlineData(1987, false)]
    [InlineData(1988, false)]
    [InlineData(1989, false)]
    [InlineData(1990, true)]
    [InlineData(1991, true)]
    [InlineData(1995, true)]
    [InlineData(1996, true)]
    [InlineData(1997, false)]
    [InlineData(1998, false)]
    public void IsInWindow_ReturnsCorrectResult(int taxYear, bool expected)
    {
        Assert.Equal(expected, BarberWindow.IsInWindow(taxYear));
    }

    [Fact]
    public void Case4_Proportion_LessThanOne()
    {
        // Case 4 has post-88 earnings in 1989 and 1997 outside the Barber window
        var result = GmpCalculator.CalculateGmp(
            Case4Data.Member, GmpRevaluationMethod.Section148, Case4Data.CreateFactors());

        Assert.True(result.BarberWindowProportion < 1m,
            "Case 4 has post-88 GMP outside the Barber window (1989 and 1997)");
        Assert.True(result.BarberWindowProportion > 0.5m,
            "Most of Case 4's post-88 GMP should be in the Barber window");
    }

    [Fact]
    public void AllPre88_ProportionIsZero()
    {
        // If all earnings are pre-88, Barber proportion = 0
        var details = new List<TaxYearDetail>
        {
            new(1987, 1000m, true, true, 0.0685m, 0.25m, 100m, 2000m, 50m, 60m),
        };
        Assert.Equal(0m, BarberWindow.CalculateProportion(details));
    }

    [Fact]
    public void AllPost88InBarber_ProportionIsOne()
    {
        // If all post-88 earnings are in the Barber window (1990-1996), proportion = 1
        var details = new List<TaxYearDetail>
        {
            new(1990, 10000m, false, false, 1m, 0.2m, 50m, 15000m, 100m, 120m),
            new(1991, 12000m, false, false, 1m, 0.2m, 40m, 16800m, 110m, 130m),
        };
        Assert.Equal(1m, BarberWindow.CalculateProportion(details));
    }
}
