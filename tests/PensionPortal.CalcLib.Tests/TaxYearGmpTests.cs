using PensionPortal.CalcLib;
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
        var detail = TaxYearGmp.Calculate(
            earningsOrNICs: 544.66m,
            taxYearOfEarnings: 1987,
            taxYearOfCalculation: 2002,
            workingLifeMale: 38,
            workingLifeFemale: 33,
            factors: factors);

        Assert.True(detail.IsNICs);
        Assert.True(detail.IsPre88);
        Assert.Equal(TaxYearGmp.NiDivisor, detail.Divisor);
        Assert.Equal(TaxYearGmp.Pre88AccrualRate, detail.AccrualRate);
        Assert.InRange(detail.RawGmpMale, 130m, 131m);
        Assert.InRange(detail.RawGmpFemale, 150m, 151m);
    }

    [Fact]
    public void Case4_TaxYear1988_TransitionYear_MatchesExpected()
    {
        // Tax year 1988 is the last pre-88 accrual year (25% rate, but band earnings at divisor 1.0)
        // Expected: RawGMPMForTY1988 = 168.48, RawGMPFForTY1988 = 194.008
        var factors = Case4Data.CreateFactors();
        var detail = TaxYearGmp.Calculate(
            earningsOrNICs: 11024m,
            taxYearOfEarnings: 1988,
            taxYearOfCalculation: 2002,
            workingLifeMale: 38,
            workingLifeFemale: 33,
            factors: factors);

        Assert.False(detail.IsNICs);   // Band earnings (not NICs)
        Assert.True(detail.IsPre88);    // But still pre-88 accrual
        Assert.Equal(1.0m, detail.Divisor);
        Assert.Equal(TaxYearGmp.Pre88AccrualRate, detail.AccrualRate);
        Assert.InRange(detail.RawGmpMale, 168m, 169m);
        Assert.InRange(detail.RawGmpFemale, 193m, 195m);
    }

    [Fact]
    public void Case4_TaxYear1992_Post88_MatchesExpected()
    {
        // Tax year 1992 is post-88 (band earnings at divisor 1.0, 20% accrual)
        // Expected: RawGMPMForTY1992 = 149.611, RawGMPFForTY1992 = 172.279
        var factors = Case4Data.CreateFactors();
        var detail = TaxYearGmp.Calculate(
            earningsOrNICs: 17407m,
            taxYearOfEarnings: 1992,
            taxYearOfCalculation: 2002,
            workingLifeMale: 38,
            workingLifeFemale: 33,
            factors: factors);

        Assert.False(detail.IsNICs);
        Assert.False(detail.IsPre88);
        Assert.Equal(1.0m, detail.Divisor);
        Assert.Equal(TaxYearGmp.Post88AccrualRate, detail.AccrualRate);
        Assert.InRange(detail.RawGmpMale, 149m, 150m);
        Assert.InRange(detail.RawGmpFemale, 172m, 173m);
    }

    [Fact]
    public void ZeroEarnings_ReturnsZero()
    {
        var factors = Case4Data.CreateFactors();
        var detail = TaxYearGmp.Calculate(
            earningsOrNICs: 0m,
            taxYearOfEarnings: 1990,
            taxYearOfCalculation: 2002,
            workingLifeMale: 38,
            workingLifeFemale: 33,
            factors: factors);

        Assert.Equal(0m, detail.RawGmpMale);
        Assert.Equal(0m, detail.RawGmpFemale);
        Assert.Equal(0m, detail.RevaluedEarnings);
    }

    [Fact]
    public void Case4_TaxYearDetails_AllPopulated()
    {
        // Verify the full GmpResult includes all 11 tax year details
        var result = GmpCalculator.CalculateGmp(
            Case4Data.Member, GmpRevaluationMethod.Section148, Case4Data.CreateFactors());

        Assert.Equal(11, result.TaxYearDetails.Count);
        Assert.Equal(1987, result.TaxYearDetails[0].TaxYear);
        Assert.Equal(1997, result.TaxYearDetails[^1].TaxYear);

        // Verify all details have positive earnings
        Assert.All(result.TaxYearDetails, d => Assert.True(d.EarningsOrNICs > 0));
    }
}
