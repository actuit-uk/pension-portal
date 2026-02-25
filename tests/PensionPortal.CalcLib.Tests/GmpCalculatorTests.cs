using PensionPortal.CalcLib;
using PensionPortal.CalcLib.Tests.TestData;

namespace PensionPortal.CalcLib.Tests;

public class GmpCalculatorTests
{
    [Fact]
    public void Case4_WorkingLife_MatchesExpected()
    {
        var result = GmpCalculator.CalculateGmp(
            Case4Data.Member, GmpRevaluationMethod.Section148, Case4Data.CreateFactors());

        Assert.Equal(Case4Data.Expected.WorkingLifeMale, result.WorkingLifeMale);
        Assert.Equal(Case4Data.Expected.WorkingLifeFemale, result.WorkingLifeFemale);
    }

    [Fact]
    public void Case4_TaxYearOfLeaving_MatchesExpected()
    {
        var result = GmpCalculator.CalculateGmp(
            Case4Data.Member, GmpRevaluationMethod.Section148, Case4Data.CreateFactors());

        Assert.Equal(Case4Data.Expected.TaxYearForEarnings, result.TaxYearOfLeaving);
    }

    [Fact]
    public void Case4_Pre88GmpAtLeaving_MatchesExpected()
    {
        var result = GmpCalculator.CalculateGmp(
            Case4Data.Member, GmpRevaluationMethod.Section148, Case4Data.CreateFactors());

        Assert.Equal(Case4Data.Expected.Pre88GmpMalePA, result.MaleAtLeaving.Pre88Annual);
        Assert.Equal(Case4Data.Expected.Pre88GmpFemalePA, result.FemaleAtLeaving.Pre88Annual);
        Assert.Equal(Case4Data.Expected.Pre88GmpMalePW, result.MaleAtLeaving.Pre88Weekly);
        Assert.Equal(Case4Data.Expected.Pre88GmpFemalePW, result.FemaleAtLeaving.Pre88Weekly);
    }

    [Fact]
    public void Case4_TotalGmpAtLeaving_MatchesExpected()
    {
        var result = GmpCalculator.CalculateGmp(
            Case4Data.Member, GmpRevaluationMethod.Section148, Case4Data.CreateFactors());

        Assert.Equal(Case4Data.Expected.TotalGmpMalePA, result.MaleAtLeaving.TotalAnnual);
        Assert.Equal(Case4Data.Expected.TotalGmpFemalePA, result.FemaleAtLeaving.TotalAnnual);
        Assert.Equal(Case4Data.Expected.TotalGmpMalePW, result.MaleAtLeaving.TotalWeekly);
        Assert.Equal(Case4Data.Expected.TotalGmpFemalePW, result.FemaleAtLeaving.TotalWeekly);
    }

    [Fact]
    public void Case4_Post88GmpAtLeaving_MatchesExpected()
    {
        var result = GmpCalculator.CalculateGmp(
            Case4Data.Member, GmpRevaluationMethod.Section148, Case4Data.CreateFactors());

        Assert.Equal(Case4Data.Expected.Post88GmpMalePW, result.MaleAtLeaving.Post88Weekly);
        Assert.Equal(Case4Data.Expected.Post88GmpFemalePW, result.FemaleAtLeaving.Post88Weekly);
    }

    [Fact]
    public void Case4_RevaluationFactors_MatchExpected()
    {
        var result = GmpCalculator.CalculateGmp(
            Case4Data.Member, GmpRevaluationMethod.Section148, Case4Data.CreateFactors());

        Assert.Equal(Case4Data.Expected.RevFactorMale, result.RevaluationFactorMale);
        Assert.Equal(Case4Data.Expected.RevFactorFemale, result.RevaluationFactorFemale);
    }

    [Fact]
    public void Case4_RevaluedGmp_MatchesExpected()
    {
        var result = GmpCalculator.CalculateGmp(
            Case4Data.Member, GmpRevaluationMethod.Section148, Case4Data.CreateFactors());

        Assert.Equal(Case4Data.Expected.TotalRevaluedGmpMalePW, result.MaleRevalued.TotalWeekly);
        Assert.Equal(Case4Data.Expected.TotalRevaluedGmpFemalePW, result.FemaleRevalued.TotalWeekly);
        Assert.Equal(Case4Data.Expected.Pre88RevaluedGmpMalePW, result.MaleRevalued.Pre88Weekly);
        Assert.Equal(Case4Data.Expected.Pre88RevaluedGmpFemalePW, result.FemaleRevalued.Pre88Weekly);
    }
}
