using PensionPortal.CalcLib;
using PensionPortal.CalcLib.Internal;
using PensionPortal.CalcLib.Tests.TestData;

namespace PensionPortal.CalcLib.Tests;

/// <summary>
/// Tests for the anti-franking floor calculator.
/// Anti-franking prevents schemes from reducing excess pension to offset GMP revaluation.
/// Under the separate increase method, anti-franking never bites (excess is tracked
/// independently). These tests verify that behavior, and also test the floor directly
/// using manually constructed cash flows that simulate the overall increase method.
/// </summary>
public class AntiFrankingTests
{
    [Fact]
    public void SeparateMethod_AntiFranking_IsNoOp()
    {
        // Under separate method, anti-franking floor = actual total, so no adjustment
        var gmp = PasaExample5Data.CreateGmpResult();
        var factors = PasaExample5Data.CreateFactors();
        var rawCashFlow = CashFlowBuilder.Build(
            gmp, PasaExample5Data.Member, PasaExample5Data.Scheme, factors);

        var (excessM, excessF) = ExcessPensionCalculator.Calculate(
            PasaExample5Data.Member, PasaExample5Data.Scheme,
            gmp.MaleAtLeaving.TotalAnnual, gmp.FemaleAtLeaving.TotalAnnual);

        var adjusted = AntiFrankingCalculator.ApplyFloor(
            rawCashFlow, gmp, excessM, excessF, factors, PasaExample5Data.Assumptions);

        // Every entry should be identical (no adjustments)
        for (int i = 0; i < rawCashFlow.Count; i++)
        {
            Assert.Equal(rawCashFlow[i].ExcessMale, adjusted[i].ExcessMale);
            Assert.Equal(rawCashFlow[i].ExcessFemale, adjusted[i].ExcessFemale);
            Assert.Equal(rawCashFlow[i].TotalPensionMale, adjusted[i].TotalPensionMale);
            Assert.Equal(rawCashFlow[i].TotalPensionFemale, adjusted[i].TotalPensionFemale);
        }
    }

    [Fact]
    public void ReducedExcess_FloorApplied()
    {
        // Simulate a scenario where excess has been eroded (as would happen with
        // overall increase method where GMP grows faster than total pension).
        // Create a cash flow where the excess in PIP is less than at-leaving excess.
        var gmp = PasaExample5Data.CreateGmpResult();
        decimal excessAtLeavingM = PasaExample5Data.Expected.ExcessMalePA;
        decimal excessAtLeavingF = PasaExample5Data.Expected.ExcessFemalePA;

        // Build a minimal cash flow: one PIP year where excess has been reduced
        var entries = new List<CashFlowEntry>
        {
            // Exit year — no change expected
            new CashFlowEntry(
                TaxYear: 2006,
                StatusMale: GmpStatus.Exit,
                Pre88GmpMale: 776.56m, Post88GmpMale: 1141.65m,
                TotalGmpMale: 1918.21m, ExcessMale: excessAtLeavingM,
                TotalPensionMale: 1918.21m + excessAtLeavingM,
                StatusFemale: GmpStatus.Exit,
                Pre88GmpFemale: 776.56m, Post88GmpFemale: 1243.54m,
                TotalGmpFemale: 2020.10m, ExcessFemale: excessAtLeavingF,
                TotalPensionFemale: 2020.10m + excessAtLeavingF,
                Post88GmpIncFactor: 0.025m, ExcessIncFactor: 0.035m),

            // Female PIP year (first) — excess at leaving, revalued GMP
            new CashFlowEntry(
                TaxYear: 2021,
                StatusMale: GmpStatus.Deferred,
                Pre88GmpMale: 776.56m, Post88GmpMale: 1141.65m,
                TotalGmpMale: 1918.21m, ExcessMale: excessAtLeavingM,
                TotalPensionMale: 1918.21m + excessAtLeavingM,
                StatusFemale: GmpStatus.InPayment,
                Pre88GmpFemale: 1184.25m, Post88GmpFemale: 1896.40m,
                TotalGmpFemale: 3080.65m, ExcessFemale: excessAtLeavingF,
                TotalPensionFemale: 3080.65m + excessAtLeavingF,
                Post88GmpIncFactor: 0.025m, ExcessIncFactor: 0.035m),

            // Female PIP year 2 — excess artificially REDUCED (simulating overall method erosion)
            new CashFlowEntry(
                TaxYear: 2022,
                StatusMale: GmpStatus.Deferred,
                Pre88GmpMale: 776.56m, Post88GmpMale: 1141.65m,
                TotalGmpMale: 1918.21m, ExcessMale: excessAtLeavingM,
                TotalPensionMale: 1918.21m + excessAtLeavingM,
                StatusFemale: GmpStatus.InPayment,
                Pre88GmpFemale: 1184.25m, Post88GmpFemale: 1943.81m, // Increased at LPI3
                TotalGmpFemale: 3128.06m,
                ExcessFemale: 15000m,  // *** Artificially reduced from ~19856 ***
                TotalPensionFemale: 3128.06m + 15000m,
                Post88GmpIncFactor: 0.025m, ExcessIncFactor: 0.035m),
        };

        var adjusted = AntiFrankingCalculator.ApplyFloor(
            entries.AsReadOnly(), gmp, excessAtLeavingM, excessAtLeavingF,
            PasaExample5Data.CreateFactors(), PasaExample5Data.Assumptions);

        // Exit year: unchanged
        Assert.Equal(entries[0].ExcessFemale, adjusted[0].ExcessFemale);

        // First PIP year: excess at leaving, no reduction — unchanged
        Assert.Equal(entries[1].ExcessFemale, adjusted[1].ExcessFemale);

        // Second PIP year: excess was reduced to 15000, but AFM requires it higher
        // AFM = revalued pre88 (1184.25) + AFM post88 (1896.40 * 1.025 = 1943.81) + excess at leaving (19856.44)
        // AFM = 1184.25 + 1943.81 + 19856.44 = 22984.50
        // Actual total GMP = 3128.06, so required excess = 22984.50 - 3128.06 = 19856.44
        // Since 15000 < 19856.44, the floor kicks in
        Assert.True(adjusted[2].ExcessFemale > 15000m,
            $"Anti-franking floor should increase excess from 15000 to at least {excessAtLeavingF}");
        Assert.Equal(excessAtLeavingF, adjusted[2].ExcessFemale);
    }

    [Fact]
    public void ExcessAboveFloor_NotReduced()
    {
        // If excess is already above the AFM, it should not be changed
        var gmp = PasaExample5Data.CreateGmpResult();
        decimal excessAtLeavingF = PasaExample5Data.Expected.ExcessFemalePA;

        var entries = new List<CashFlowEntry>
        {
            // Female PIP year 1 with HIGHER excess than at-leaving (e.g., from LPI5 increase)
            new CashFlowEntry(
                TaxYear: 2021,
                StatusMale: GmpStatus.Deferred,
                Pre88GmpMale: 776.56m, Post88GmpMale: 1141.65m,
                TotalGmpMale: 1918.21m, ExcessMale: 19958.33m,
                TotalPensionMale: 1918.21m + 19958.33m,
                StatusFemale: GmpStatus.InPayment,
                Pre88GmpFemale: 1184.25m, Post88GmpFemale: 1896.40m,
                TotalGmpFemale: 3080.65m,
                ExcessFemale: 25000m,  // Higher than at-leaving
                TotalPensionFemale: 3080.65m + 25000m,
                Post88GmpIncFactor: 0.025m, ExcessIncFactor: 0.035m),
        };

        var adjusted = AntiFrankingCalculator.ApplyFloor(
            entries.AsReadOnly(), gmp, 19958.33m, excessAtLeavingF,
            PasaExample5Data.CreateFactors(), PasaExample5Data.Assumptions);

        // Excess above floor — should not be reduced
        Assert.Equal(25000m, adjusted[0].ExcessFemale);
    }

    [Fact]
    public void DeferredYears_NotAffected()
    {
        // Anti-franking only applies in PIP, not in deferred period
        var gmp = PasaExample5Data.CreateGmpResult();

        var entries = new List<CashFlowEntry>
        {
            new CashFlowEntry(
                TaxYear: 2010,
                StatusMale: GmpStatus.Deferred,
                Pre88GmpMale: 776.56m, Post88GmpMale: 1141.65m,
                TotalGmpMale: 1918.21m, ExcessMale: 100m,  // Artificially low
                TotalPensionMale: 1918.21m + 100m,
                StatusFemale: GmpStatus.Deferred,
                Pre88GmpFemale: 776.56m, Post88GmpFemale: 1243.54m,
                TotalGmpFemale: 2020.10m, ExcessFemale: 100m,  // Artificially low
                TotalPensionFemale: 2020.10m + 100m,
                Post88GmpIncFactor: 0.025m, ExcessIncFactor: 0.035m),
        };

        var adjusted = AntiFrankingCalculator.ApplyFloor(
            entries.AsReadOnly(), gmp, 19958.33m, 19856.44m,
            PasaExample5Data.CreateFactors(), PasaExample5Data.Assumptions);

        // Deferred: no anti-franking adjustment
        Assert.Equal(100m, adjusted[0].ExcessMale);
        Assert.Equal(100m, adjusted[0].ExcessFemale);
    }

    [Fact]
    public void FullPipeline_WithAntiFranking_StillWorks()
    {
        // End-to-end: enable anti-franking on Case 4 (GMP-only, separate method)
        // Should produce same results as without (no excess to frank)
        var schemeWithAF = Case4Data.Scheme with { AntiFrankingApplies = true };

        var resultWithAF = GmpCalculator.Calculate(
            Case4Data.Member, schemeWithAF, Case4Data.CreateFactors());
        var resultWithout = GmpCalculator.Calculate(
            Case4Data.Member, Case4Data.Scheme, Case4Data.CreateFactors());

        Assert.Equal(resultWithout.TotalCompensation, resultWithAF.TotalCompensation);
    }
}
