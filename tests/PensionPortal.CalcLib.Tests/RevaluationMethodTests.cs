using PensionPortal.CalcLib;
using PensionPortal.CalcLib.Tests.TestData;

namespace PensionPortal.CalcLib.Tests;

/// <summary>
/// Tests for all three GMP revaluation methods: Section148, FixedRate, LimitedRate.
/// Case 4 validates Section148, Case 5 validates FixedRate (same member, different method).
/// LimitedRate (5% compound) is cross-checked against FixedRate (4.5% compound).
/// </summary>
public class RevaluationMethodTests
{
    // --- FixedRate: Case 5 (same member as Case 4, FixedRate 4.5%) ---

    [Fact]
    public void Case5_FixedRate_GmpAtLeaving_MatchesCase4()
    {
        // GMP at leaving is identical regardless of revaluation method
        var result = GmpCalculator.CalculateGmp(
            Case5Data.Member, GmpRevaluationMethod.FixedRate, Case5Data.CreateFactors());

        Assert.Equal(Case5Data.Expected.TotalGmpMalePA, result.MaleAtLeaving.TotalAnnual);
        Assert.Equal(Case5Data.Expected.TotalGmpFemalePA, result.FemaleAtLeaving.TotalAnnual);
        Assert.Equal(Case5Data.Expected.TotalGmpMalePW, result.MaleAtLeaving.TotalWeekly);
        Assert.Equal(Case5Data.Expected.TotalGmpFemalePW, result.FemaleAtLeaving.TotalWeekly);
    }

    [Fact]
    public void Case5_FixedRate_RevaluationFactors_MatchExpected()
    {
        var result = GmpCalculator.CalculateGmp(
            Case5Data.Member, GmpRevaluationMethod.FixedRate, Case5Data.CreateFactors());

        // Fixed rate 4.5%: male (1.045)^13, female (1.045)^8
        Assert.Equal(Case5Data.Expected.RevFactorMale, result.RevaluationFactorMale);
        Assert.Equal(Case5Data.Expected.RevFactorFemale, result.RevaluationFactorFemale);
    }

    [Fact]
    public void Case5_FixedRate_RevaluedGmp_MatchesExpected()
    {
        var result = GmpCalculator.CalculateGmp(
            Case5Data.Member, GmpRevaluationMethod.FixedRate, Case5Data.CreateFactors());

        Assert.Equal(Case5Data.Expected.TotalRevaluedGmpMalePW, result.MaleRevalued.TotalWeekly);
        Assert.Equal(Case5Data.Expected.TotalRevaluedGmpFemalePW, result.FemaleRevalued.TotalWeekly);
        Assert.Equal(Case5Data.Expected.Pre88RevaluedGmpMalePW, result.MaleRevalued.Pre88Weekly);
        Assert.Equal(Case5Data.Expected.Pre88RevaluedGmpFemalePW, result.FemaleRevalued.Pre88Weekly);
    }

    [Fact]
    public void Case5_FixedRate_MaleRevHigherThanSection148()
    {
        // Fixed rate 4.5% for 13 years > Section148 42.2% for this member
        var fixedResult = GmpCalculator.CalculateGmp(
            Case5Data.Member, GmpRevaluationMethod.FixedRate, Case5Data.CreateFactors());
        var s148Result = GmpCalculator.CalculateGmp(
            Case4Data.Member, GmpRevaluationMethod.Section148, Case4Data.CreateFactors());

        Assert.True(fixedResult.RevaluationFactorMale > s148Result.RevaluationFactorMale,
            "FixedRate 4.5%×13yr should exceed Section148 for this member");
        Assert.True(fixedResult.MaleRevalued.TotalWeekly > s148Result.MaleRevalued.TotalWeekly);
    }

    [Fact]
    public void Case5_FixedRate_FullPipeline_ReturnsResult()
    {
        var result = GmpCalculator.Calculate(
            Case5Data.Member, Case5Data.Scheme, Case5Data.CreateFactors());

        Assert.Equal(Case5Data.Expected.TotalGmpMalePA, result.Gmp.MaleAtLeaving.TotalAnnual);
        Assert.Equal(25, result.CashFlow.Count);
        Assert.Equal(25, result.Compensation.Count);
        Assert.NotEqual(0m, result.TotalCompensation);
    }

    // --- LimitedRate: 5% compound (cross-check against FixedRate 4.5%) ---

    [Fact]
    public void LimitedRate_HigherThanFixedRate()
    {
        // Limited rate is 5% compound vs fixed rate 4.5% for this member (2002 band)
        var factors = Case5Data.CreateFactors();

        var fixedResult = GmpCalculator.CalculateGmp(
            Case5Data.Member, GmpRevaluationMethod.FixedRate, factors);
        var limitedResult = GmpCalculator.CalculateGmp(
            Case5Data.Member, GmpRevaluationMethod.LimitedRate, factors);

        // 5% > 4.5% → higher revaluation
        Assert.True(limitedResult.RevaluationFactorMale > fixedResult.RevaluationFactorMale,
            "LimitedRate 5% should exceed FixedRate 4.5%");
        Assert.True(limitedResult.MaleRevalued.TotalWeekly > fixedResult.MaleRevalued.TotalWeekly);
    }

    [Fact]
    public void LimitedRate_RevaluationFactor_MatchesExpected()
    {
        var result = GmpCalculator.CalculateGmp(
            Case5Data.Member, GmpRevaluationMethod.LimitedRate, Case5Data.CreateFactors());

        // (1.05)^13 = 1.88564..., rounded to 3dp = 1.886
        Assert.Equal(1.886m, result.RevaluationFactorMale);
        // (1.05)^8 = 1.47745..., rounded to 3dp = 1.477
        Assert.Equal(1.477m, result.RevaluationFactorFemale);
    }

    [Fact]
    public void LimitedRate_RevaluedGmp_ComputedCorrectly()
    {
        var result = GmpCalculator.CalculateGmp(
            Case5Data.Member, GmpRevaluationMethod.LimitedRate, Case5Data.CreateFactors());

        // Male: 30.33 * (1.05^13) = 57.19, Female: 34.92 * (1.05^8) = 51.59
        Assert.Equal(57.19m, result.MaleRevalued.TotalWeekly);
        Assert.Equal(51.59m, result.FemaleRevalued.TotalWeekly);
    }

    [Fact]
    public void LimitedRate_GmpAtLeaving_SameAsOtherMethods()
    {
        // GMP at leaving doesn't depend on revaluation method
        var result = GmpCalculator.CalculateGmp(
            Case5Data.Member, GmpRevaluationMethod.LimitedRate, Case5Data.CreateFactors());

        Assert.Equal(Case4Data.Expected.TotalGmpMalePA, result.MaleAtLeaving.TotalAnnual);
        Assert.Equal(Case4Data.Expected.TotalGmpFemalePA, result.FemaleAtLeaving.TotalAnnual);
    }

    // --- Cross-method: all three methods agree on at-leaving values ---

    [Fact]
    public void AllThreeMethods_IdenticalGmpAtLeaving()
    {
        var factors = Case5Data.CreateFactors();

        var s148 = GmpCalculator.CalculateGmp(
            Case4Data.Member, GmpRevaluationMethod.Section148, factors);
        var fixd = GmpCalculator.CalculateGmp(
            Case5Data.Member, GmpRevaluationMethod.FixedRate, factors);
        var limited = GmpCalculator.CalculateGmp(
            Case5Data.Member, GmpRevaluationMethod.LimitedRate, factors);

        Assert.Equal(s148.MaleAtLeaving.TotalAnnual, fixd.MaleAtLeaving.TotalAnnual);
        Assert.Equal(s148.MaleAtLeaving.TotalAnnual, limited.MaleAtLeaving.TotalAnnual);
        Assert.Equal(s148.FemaleAtLeaving.TotalAnnual, fixd.FemaleAtLeaving.TotalAnnual);
        Assert.Equal(s148.FemaleAtLeaving.TotalAnnual, limited.FemaleAtLeaving.TotalAnnual);
    }

    [Fact]
    public void AllThreeMethods_RevaluedGmpDiffers()
    {
        var factors = Case5Data.CreateFactors();

        var s148 = GmpCalculator.CalculateGmp(
            Case4Data.Member, GmpRevaluationMethod.Section148, factors);
        var fixd = GmpCalculator.CalculateGmp(
            Case5Data.Member, GmpRevaluationMethod.FixedRate, factors);
        var limited = GmpCalculator.CalculateGmp(
            Case5Data.Member, GmpRevaluationMethod.LimitedRate, factors);

        // All three produce different revalued amounts for this member
        Assert.NotEqual(s148.MaleRevalued.TotalWeekly, fixd.MaleRevalued.TotalWeekly);
        Assert.NotEqual(fixd.MaleRevalued.TotalWeekly, limited.MaleRevalued.TotalWeekly);
        Assert.NotEqual(s148.MaleRevalued.TotalWeekly, limited.MaleRevalued.TotalWeekly);
    }
}
