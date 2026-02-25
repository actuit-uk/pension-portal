using PensionPortal.CalcLib.Internal;
using PensionPortal.CalcLib.Tests.TestData;

namespace PensionPortal.CalcLib.Tests;

public class TaxYearGmpTests
{
    [Fact]
    public void Case4_TaxYear1987_Pre88_MatchesExpected()
    {
        // Tax year 1987 is pre-88 (NICs at 6.85%, 25% accrual)
        // Expected from DB: RawGMPMForTY1987 = 130.513, RawGMPFForTY1987 = 150.288
        var factors = Case4Data.CreateFactors();
        var (male, female) = TaxYearGmp.Calculate(
            earningsOrNICs: 544.66m,
            taxYearOfEarnings: 1987,
            taxYearOfCalculation: 2002,
            workingLifeMale: 38,
            workingLifeFemale: 33,
            factors: factors);

        // The sproc stores unrounded per-year amounts
        Assert.InRange(male, 130m, 131m);
        Assert.InRange(female, 150m, 151m);
    }

    [Fact]
    public void Case4_TaxYear1988_Post88_MatchesExpected()
    {
        // Tax year 1988 is the last pre-88 accrual year (25% rate, but band earnings at divisor 1.0)
        // Wait - looking at the sproc, 1988 uses accrual 0.25 and NIRate 1.0
        // Expected: RawGMPMForTY1988 = 168.48, RawGMPFForTY1988 = 194.008
        var factors = Case4Data.CreateFactors();
        var (male, female) = TaxYearGmp.Calculate(
            earningsOrNICs: 11024m,
            taxYearOfEarnings: 1988,
            taxYearOfCalculation: 2002,
            workingLifeMale: 38,
            workingLifeFemale: 33,
            factors: factors);

        Assert.InRange(male, 168m, 169m);
        Assert.InRange(female, 193m, 195m);
    }

    [Fact]
    public void Case4_TaxYear1992_Post88_MatchesExpected()
    {
        // Tax year 1992 is post-88 (band earnings at divisor 1.0, 20% accrual)
        // Expected: RawGMPMForTY1992 = 149.611, RawGMPFForTY1992 = 172.279
        var factors = Case4Data.CreateFactors();
        var (male, female) = TaxYearGmp.Calculate(
            earningsOrNICs: 17407m,
            taxYearOfEarnings: 1992,
            taxYearOfCalculation: 2002,
            workingLifeMale: 38,
            workingLifeFemale: 33,
            factors: factors);

        Assert.InRange(male, 149m, 150m);
        Assert.InRange(female, 172m, 173m);
    }

    [Fact]
    public void ZeroEarnings_ReturnsZero()
    {
        var factors = Case4Data.CreateFactors();
        var (male, female) = TaxYearGmp.Calculate(
            earningsOrNICs: 0m,
            taxYearOfEarnings: 1990,
            taxYearOfCalculation: 2002,
            workingLifeMale: 38,
            workingLifeFemale: 33,
            factors: factors);

        Assert.Equal(0m, male);
        Assert.Equal(0m, female);
    }
}
