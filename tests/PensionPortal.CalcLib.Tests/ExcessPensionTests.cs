using PensionPortal.CalcLib;
using PensionPortal.CalcLib.Internal;
using PensionPortal.CalcLib.Tests.TestData;

namespace PensionPortal.CalcLib.Tests;

public class ExcessPensionTests
{
    [Fact]
    public void Tier3_NoSalaryOrPension_ExcessIsZero()
    {
        // Case 4 member has no PensionAtLeaving or FinalPensionableSalary
        var (excessM, excessF) = ExcessPensionCalculator.Calculate(
            Case4Data.Member, Case4Data.Scheme,
            Case4Data.Expected.TotalGmpMalePA, Case4Data.Expected.TotalGmpFemalePA);

        Assert.Equal(0m, excessM);
        Assert.Equal(0m, excessF);
    }

    [Fact]
    public void Tier1_DirectPension_ExcessComputedCorrectly()
    {
        // Provide a direct total pension at leaving
        var memberWithPension = Case4Data.Member with { PensionAtLeaving = 5000m };

        var (excessM, excessF) = ExcessPensionCalculator.Calculate(
            memberWithPension, Case4Data.Scheme,
            Case4Data.Expected.TotalGmpMalePA, Case4Data.Expected.TotalGmpFemalePA);

        // Excess = 5000 - GMP (different for M/F)
        Assert.Equal(Math.Round(5000m - Case4Data.Expected.TotalGmpMalePA, 2), excessM);
        Assert.Equal(Math.Round(5000m - Case4Data.Expected.TotalGmpFemalePA, 2), excessF);
        Assert.True(excessM > excessF, "Male GMP is lower so male excess should be higher");
    }

    [Fact]
    public void Tier2_SalaryFallback_EstimatesExcess()
    {
        // Provide salary but no direct pension. 1/80ths scheme, ~16.6 years service.
        // Case 4: CO 6 Apr 1986 to 10 Nov 2002 = ~16.6 years
        // Estimated pension = 30000 * 16.6 / 80 = ~6225
        var memberWithSalary = Case4Data.Member with { FinalPensionableSalary = 30000m };

        var (excessM, excessF) = ExcessPensionCalculator.Calculate(
            memberWithSalary, Case4Data.Scheme,
            Case4Data.Expected.TotalGmpMalePA, Case4Data.Expected.TotalGmpFemalePA);

        Assert.True(excessM > 0, "Salary-based excess should be positive");
        Assert.True(excessF > 0, "Salary-based excess should be positive");
        Assert.True(excessM > excessF, "Male GMP is lower so male excess should be higher");
    }

    [Fact]
    public void Tier1_TakesPrecedence_OverTier2()
    {
        // Both PensionAtLeaving and FinalPensionableSalary provided — tier 1 wins
        var member = Case4Data.Member with
        {
            PensionAtLeaving = 4000m,
            FinalPensionableSalary = 50000m  // would give much higher excess
        };

        var (excessM, _) = ExcessPensionCalculator.Calculate(
            member, Case4Data.Scheme,
            Case4Data.Expected.TotalGmpMalePA, Case4Data.Expected.TotalGmpFemalePA);

        // Tier 1: excess = 4000 - 1576.97 = 2423.03
        Assert.Equal(Math.Round(4000m - Case4Data.Expected.TotalGmpMalePA, 2), excessM);
    }

    [Fact]
    public void PensionBelowGmp_ExcessFlooredAtZero()
    {
        // Edge case: total pension less than GMP
        var member = Case4Data.Member with { PensionAtLeaving = 500m };

        var (excessM, excessF) = ExcessPensionCalculator.Calculate(
            member, Case4Data.Scheme,
            Case4Data.Expected.TotalGmpMalePA, Case4Data.Expected.TotalGmpFemalePA);

        Assert.Equal(0m, excessM);
        Assert.Equal(0m, excessF);
    }

    [Fact]
    public void CashFlow_WithExcess_TotalPensionIncludesExcess()
    {
        // Run full cash flow with direct pension to verify excess flows through
        var memberWithPension = Case4Data.Member with { PensionAtLeaving = 5000m };
        var gmp = GmpCalculator.CalculateGmp(
            memberWithPension, GmpRevaluationMethod.Section148, Case4Data.CreateFactors());
        var cashFlow = CashFlowBuilder.Build(gmp, memberWithPension, Case4Data.Scheme, Case4Data.CreateFactors());

        // All years should have positive excess
        Assert.All(cashFlow, e =>
        {
            Assert.True(e.ExcessMale > 0, $"Year {e.TaxYear}: male excess should be positive");
            Assert.True(e.ExcessFemale > 0, $"Year {e.TaxYear}: female excess should be positive");
            Assert.Equal(e.TotalGmpMale + e.ExcessMale, e.TotalPensionMale);
            Assert.Equal(e.TotalGmpFemale + e.ExcessFemale, e.TotalPensionFemale);
        });
    }

    [Fact]
    public void CashFlow_ExcessIncreases_InPipYears()
    {
        var memberWithPension = Case4Data.Member with { PensionAtLeaving = 5000m };
        var gmp = GmpCalculator.CalculateGmp(
            memberWithPension, GmpRevaluationMethod.Section148, Case4Data.CreateFactors());
        var cashFlow = CashFlowBuilder.Build(gmp, memberWithPension, Case4Data.Scheme, Case4Data.CreateFactors());

        // Female excess should increase after first PIP year (2011)
        var pip2011 = cashFlow.First(e => e.TaxYear == 2011);
        var pip2012 = cashFlow.First(e => e.TaxYear == 2012);
        Assert.True(pip2012.ExcessFemale > pip2011.ExcessFemale,
            "Female excess should increase after first PIP year");

        // Deferred years should have the same (at-leaving) excess
        var def2005 = cashFlow.First(e => e.TaxYear == 2005);
        var def2009 = cashFlow.First(e => e.TaxYear == 2009);
        Assert.Equal(def2005.ExcessFemale, def2009.ExcessFemale);
    }
}
