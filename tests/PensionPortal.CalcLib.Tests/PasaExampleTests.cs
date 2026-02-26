using PensionPortal.CalcLib;
using PensionPortal.CalcLib.Internal;
using PensionPortal.CalcLib.Tests.TestData;

namespace PensionPortal.CalcLib.Tests;

/// <summary>
/// Tests inspired by PASA Conversion Example 5 (male deferred member).
/// These tests validate the full excess pension comparison pipeline
/// using a manually constructed GmpResult with PASA-sourced GMP/pension figures.
///
/// Exact number matching against the PASA Annex isn't possible because:
/// - Our model uses tax years; PASA uses calendar years with Jan 1 increase dates
/// - Our model lumps 1988-90 and post-1990 GMP into a single "post-88" bucket
/// - GMP age drives PIP timing in our model; PASA uses NRD for excess pension
///
/// Instead, these tests verify structural correctness: excess flows through,
/// increase rates are differentiated (LPI3 for GMP, LPI5 for excess),
/// and the male member is correctly identified as disadvantaged.
/// </summary>
public class PasaExampleTests
{
    // ===== Excess Pension Calculation =====

    [Fact]
    public void Example5_ExcessPension_DerivedFromPensionAtLeaving()
    {
        var (excessM, excessF) = ExcessPensionCalculator.Calculate(
            PasaExample5Data.Member, PasaExample5Data.Scheme,
            PasaExample5Data.Expected.TotalGmpMalePA,
            PasaExample5Data.Expected.TotalGmpFemalePA);

        Assert.Equal(PasaExample5Data.Expected.ExcessMalePA, excessM);
        Assert.Equal(PasaExample5Data.Expected.ExcessFemalePA, excessF);
    }

    [Fact]
    public void Example5_ExcessMale_GreaterThanFemale()
    {
        // Male GMP is lower, so male excess (pension above GMP) is higher
        var (excessM, excessF) = ExcessPensionCalculator.Calculate(
            PasaExample5Data.Member, PasaExample5Data.Scheme,
            PasaExample5Data.Expected.TotalGmpMalePA,
            PasaExample5Data.Expected.TotalGmpFemalePA);

        Assert.True(excessM > excessF,
            $"Male excess ({excessM}) should be greater than female excess ({excessF}) " +
            "because male GMP is lower");
    }

    // ===== Cash Flow Projection =====

    [Fact]
    public void Example5_CashFlow_ExcessFlowsThrough()
    {
        var gmp = PasaExample5Data.CreateGmpResult();
        var factors = PasaExample5Data.CreateFactors();
        var cashFlow = CashFlowBuilder.Build(gmp, PasaExample5Data.Member, PasaExample5Data.Scheme, factors);

        // First year (EXIT) should have excess from PensionAtLeaving
        var exitYear = cashFlow[0];
        Assert.Equal(2006, exitYear.TaxYear);
        Assert.Equal(PasaExample5Data.Expected.ExcessMalePA, exitYear.ExcessMale);
        Assert.Equal(PasaExample5Data.Expected.ExcessFemalePA, exitYear.ExcessFemale);

        // TotalPension = GMP + Excess
        Assert.Equal(exitYear.TotalGmpMale + exitYear.ExcessMale, exitYear.TotalPensionMale);
        Assert.Equal(exitYear.TotalGmpFemale + exitYear.ExcessFemale, exitYear.TotalPensionFemale);
    }

    [Fact]
    public void Example5_CashFlow_ExcessFlatInDeferredPeriod()
    {
        var gmp = PasaExample5Data.CreateGmpResult();
        var factors = PasaExample5Data.CreateFactors();
        var cashFlow = CashFlowBuilder.Build(gmp, PasaExample5Data.Member, PasaExample5Data.Scheme, factors);

        // Male deferred until tax year 2026 (GMP age 65 = Feb 2027, TY 2026)
        // Excess should stay at at-leaving values in deferred years
        var def2010 = cashFlow.First(e => e.TaxYear == 2010);
        var def2020 = cashFlow.First(e => e.TaxYear == 2020);

        Assert.Equal(GmpStatus.Deferred, def2010.StatusMale);
        Assert.Equal(GmpStatus.Deferred, def2020.StatusMale);
        Assert.Equal(PasaExample5Data.Expected.ExcessMalePA, def2010.ExcessMale);
        Assert.Equal(PasaExample5Data.Expected.ExcessMalePA, def2020.ExcessMale);
    }

    [Fact]
    public void Example5_CashFlow_GmpPost88IncreasesAtLPI3()
    {
        var gmp = PasaExample5Data.CreateGmpResult();
        var factors = PasaExample5Data.CreateFactors();
        var cashFlow = CashFlowBuilder.Build(gmp, PasaExample5Data.Member, PasaExample5Data.Scheme, factors);

        // Female PIP starts in tax year 2021 (GMP age 60 = Feb 2022, TY 2021)
        // First PIP year: revalued values. Second PIP year (2022): increase at LPI3 (2.5%)
        var pip2021 = cashFlow.First(e => e.TaxYear == 2021);
        var pip2022 = cashFlow.First(e => e.TaxYear == 2022);

        Assert.Equal(GmpStatus.InPayment, pip2021.StatusFemale);
        Assert.Equal(GmpStatus.InPayment, pip2022.StatusFemale);

        // Post-88 GMP should increase by LPI3 factor (2.5%)
        decimal expectedPost88 = Math.Round(pip2021.Post88GmpFemale * 1.025m, 2);
        Assert.Equal(expectedPost88, pip2022.Post88GmpFemale);

        // Pre-88 GMP should stay flat (no increases)
        Assert.Equal(pip2021.Pre88GmpFemale, pip2022.Pre88GmpFemale);
    }

    [Fact]
    public void Example5_CashFlow_ExcessIncreasesAtLPI5()
    {
        var gmp = PasaExample5Data.CreateGmpResult();
        var factors = PasaExample5Data.CreateFactors();
        var cashFlow = CashFlowBuilder.Build(gmp, PasaExample5Data.Member, PasaExample5Data.Scheme, factors);

        // Female enters PIP in 2021. Second PIP year (2022): excess increases at LPI5 (3.5%)
        var pip2021 = cashFlow.First(e => e.TaxYear == 2021);
        var pip2022 = cashFlow.First(e => e.TaxYear == 2022);

        decimal expectedExcess = Math.Round(pip2021.ExcessFemale * 1.035m, 2);
        Assert.Equal(expectedExcess, pip2022.ExcessFemale);

        // Verify the increase factor is tracked correctly
        Assert.Equal(0.035m, pip2021.ExcessIncFactor);
    }

    [Fact]
    public void Example5_CashFlow_DifferentIncreaseRates()
    {
        var gmp = PasaExample5Data.CreateGmpResult();
        var factors = PasaExample5Data.CreateFactors();
        var cashFlow = CashFlowBuilder.Build(gmp, PasaExample5Data.Member, PasaExample5Data.Scheme, factors);

        // After several years in PIP, excess should have grown faster than GMP
        // because LPI5 (3.5%) > LPI3 (2.5%)
        var latePip = cashFlow.First(e => e.TaxYear == 2035);

        Assert.Equal(GmpStatus.InPayment, latePip.StatusFemale);

        // Ratio of excess to at-leaving excess should be higher than
        // ratio of post-88 GMP to revalued post-88 GMP after same years
        decimal excessGrowthRatio = latePip.ExcessFemale / PasaExample5Data.Expected.ExcessFemalePA;
        decimal gmpGrowthRatio = latePip.Post88GmpFemale / PasaExample5Data.Expected.RevaluedPost88FemalePA;
        Assert.True(excessGrowthRatio > gmpGrowthRatio,
            $"Excess growth ({excessGrowthRatio:F4}) should exceed GMP growth ({gmpGrowthRatio:F4}) " +
            "because LPI5 (3.5%) > LPI3 (2.5%)");
    }

    [Fact]
    public void Example5_CashFlow_SpansFullProjection()
    {
        var gmp = PasaExample5Data.CreateGmpResult();
        var factors = PasaExample5Data.CreateFactors();
        var cashFlow = CashFlowBuilder.Build(gmp, PasaExample5Data.Member, PasaExample5Data.Scheme, factors);

        Assert.Equal(2006, cashFlow.First().TaxYear);
        Assert.Equal(2058, cashFlow.Last().TaxYear);
        Assert.Equal(53, cashFlow.Count); // 2006 to 2058 inclusive
    }

    // ===== Compensation =====

    [Fact]
    public void Example5_Compensation_MaleIsDisadvantaged()
    {
        var gmp = PasaExample5Data.CreateGmpResult();
        var factors = PasaExample5Data.CreateFactors();
        var cashFlow = CashFlowBuilder.Build(gmp, PasaExample5Data.Member, PasaExample5Data.Scheme, factors);

        var (entries, total) = CompensationCalculator.Calculate(
            cashFlow, Sex.Male, PasaExample5Data.Scheme, factors,
            gmp.BarberWindowProportion, gmp.BarberServiceProportion);

        Assert.True(total > 0,
            $"Male member should be disadvantaged (positive compensation), got {total}");
    }

    [Fact]
    public void Example5_Compensation_FemaleComparatorIsAdvantaged()
    {
        // Same data but viewed from female perspective — should have negative compensation
        var gmp = PasaExample5Data.CreateGmpResult();
        var factors = PasaExample5Data.CreateFactors();
        var cashFlow = CashFlowBuilder.Build(gmp, PasaExample5Data.Member, PasaExample5Data.Scheme, factors);

        var (entries, total) = CompensationCalculator.Calculate(
            cashFlow, Sex.Female, PasaExample5Data.Scheme, factors,
            gmp.BarberWindowProportion, gmp.BarberServiceProportion);

        Assert.True(total < 0,
            $"Female should be advantaged (negative compensation), got {total}");
    }

    [Fact]
    public void Example5_Compensation_HasEntriesForAllYears()
    {
        var gmp = PasaExample5Data.CreateGmpResult();
        var factors = PasaExample5Data.CreateFactors();
        var cashFlow = CashFlowBuilder.Build(gmp, PasaExample5Data.Member, PasaExample5Data.Scheme, factors);

        var (entries, _) = CompensationCalculator.Calculate(
            cashFlow, Sex.Male, PasaExample5Data.Scheme, factors,
            gmp.BarberWindowProportion, gmp.BarberServiceProportion);

        Assert.Equal(cashFlow.Count, entries.Count);
        Assert.Equal(2006, entries.First().TaxYear);
        Assert.Equal(2058, entries.Last().TaxYear);
    }

    [Fact]
    public void Example5_Compensation_MaleAndFemaleSymmetric()
    {
        // Male total + Female total should approximately sum to zero (signed differences)
        var gmp = PasaExample5Data.CreateGmpResult();
        var factors = PasaExample5Data.CreateFactors();
        var cashFlow = CashFlowBuilder.Build(gmp, PasaExample5Data.Member, PasaExample5Data.Scheme, factors);

        var (_, maleTotal) = CompensationCalculator.Calculate(
            cashFlow, Sex.Male, PasaExample5Data.Scheme, factors,
            gmp.BarberWindowProportion, gmp.BarberServiceProportion);

        var (_, femaleTotal) = CompensationCalculator.Calculate(
            cashFlow, Sex.Female, PasaExample5Data.Scheme, factors,
            gmp.BarberWindowProportion, gmp.BarberServiceProportion);

        Assert.Equal(maleTotal, -femaleTotal);
    }

    [Fact]
    public void Example5_Compensation_EarlyYearsHaveZeroCompensation()
    {
        // Before either sex enters PIP, compensation should be zero
        var gmp = PasaExample5Data.CreateGmpResult();
        var factors = PasaExample5Data.CreateFactors();
        var cashFlow = CashFlowBuilder.Build(gmp, PasaExample5Data.Member, PasaExample5Data.Scheme, factors);

        var (entries, _) = CompensationCalculator.Calculate(
            cashFlow, Sex.Male, PasaExample5Data.Scheme, factors,
            gmp.BarberWindowProportion, gmp.BarberServiceProportion);

        // Tax years 2006-2020: both sexes deferred or just entering PIP
        // Female PIP starts 2021, but compensation only from second PIP year (2022)
        // Male PIP starts 2026, compensation from 2027
        var preCompYears = entries.Where(e => e.TaxYear <= 2021);
        Assert.All(preCompYears, e =>
            Assert.Equal(0m, e.CompensationCashFlow));
    }

    // ===== Barber Window =====

    [Fact]
    public void Example5_BarberServiceProportion_IsPartial()
    {
        // CO service: Jun 1985 to Jul 2006 (~21 years)
        // Barber window: May 1990 to Apr 1997 (~7 years)
        // Proportion should be roughly 7/21 ≈ 0.33
        decimal prop = BarberWindow.CalculateServiceProportion(
            PasaExample5Data.Member.DateCOStart,
            PasaExample5Data.Member.DateOfLeaving);

        Assert.True(prop > 0.30m && prop < 0.35m,
            $"Barber service proportion {prop} should be approximately 0.33");
    }
}
