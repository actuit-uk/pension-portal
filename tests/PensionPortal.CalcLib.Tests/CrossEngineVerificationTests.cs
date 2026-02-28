using PensionPortal.CalcLib;
using PensionPortal.CalcLib.Internal;
using PensionPortal.CalcLib.Tests.TestData;

namespace PensionPortal.CalcLib.Tests;

/// <summary>
/// Tests that the cross-engine verification fields (anti-franking flags,
/// Barber scaling, per-year interest) are correctly populated on CalcLib
/// output records. These fields exist so that an actuary can diff CalcLib
/// vs pension-engine results year by year.
/// </summary>
public class CrossEngineVerificationTests
{
    // ── Anti-franking transparency fields ────────────────────────────

    [Fact]
    public void AntiFranking_FloorBinds_FieldsPopulated()
    {
        // Build a cash flow where excess has been eroded (simulating overall method)
        var gmp = PasaExample5Data.CreateGmpResult();
        decimal excessAtLeavingM = PasaExample5Data.Expected.ExcessMalePA;
        decimal excessAtLeavingF = PasaExample5Data.Expected.ExcessFemalePA;

        var entries = new List<CashFlowEntry>
        {
            // Female first PIP year
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

            // Female second PIP year — excess artificially reduced
            new CashFlowEntry(
                TaxYear: 2022,
                StatusMale: GmpStatus.Deferred,
                Pre88GmpMale: 776.56m, Post88GmpMale: 1141.65m,
                TotalGmpMale: 1918.21m, ExcessMale: excessAtLeavingM,
                TotalPensionMale: 1918.21m + excessAtLeavingM,
                StatusFemale: GmpStatus.InPayment,
                Pre88GmpFemale: 1184.25m, Post88GmpFemale: 1943.81m,
                TotalGmpFemale: 3128.06m,
                ExcessFemale: 15000m, // Reduced from ~19856
                TotalPensionFemale: 3128.06m + 15000m,
                Post88GmpIncFactor: 0.025m, ExcessIncFactor: 0.035m),
        };

        var adjusted = AntiFrankingCalculator.ApplyFloor(
            entries.AsReadOnly(), gmp, excessAtLeavingM, excessAtLeavingF,
            PasaExample5Data.CreateFactors(), PasaExample5Data.Assumptions);

        // Second PIP year: floor should bind on female side
        Assert.True(adjusted[1].AntiFrankingAppliedFemale,
            "AF Applied flag should be true when floor binds");
        Assert.True(adjusted[1].GmpFloorFemale > 0m,
            "GMP Floor should be populated when AF is active");

        // Male side is deferred — AF should not apply
        Assert.False(adjusted[1].AntiFrankingAppliedMale);
    }

    [Fact]
    public void AntiFranking_FloorDoesNotBind_FieldsStillShowFloor()
    {
        // When AF is active but floor doesn't bind, we still want the floor value
        // for cross-engine comparison
        var gmp = PasaExample5Data.CreateGmpResult();
        decimal excessAtLeavingF = PasaExample5Data.Expected.ExcessFemalePA;

        var entries = new List<CashFlowEntry>
        {
            // Female PIP year with high excess (floor won't bind)
            new CashFlowEntry(
                TaxYear: 2021,
                StatusMale: GmpStatus.Deferred,
                Pre88GmpMale: 776.56m, Post88GmpMale: 1141.65m,
                TotalGmpMale: 1918.21m, ExcessMale: 19958.33m,
                TotalPensionMale: 1918.21m + 19958.33m,
                StatusFemale: GmpStatus.InPayment,
                Pre88GmpFemale: 1184.25m, Post88GmpFemale: 1896.40m,
                TotalGmpFemale: 3080.65m,
                ExcessFemale: 25000m, // Higher than at-leaving
                TotalPensionFemale: 3080.65m + 25000m,
                Post88GmpIncFactor: 0.025m, ExcessIncFactor: 0.035m),
        };

        var adjusted = AntiFrankingCalculator.ApplyFloor(
            entries.AsReadOnly(), gmp, 19958.33m, excessAtLeavingF,
            PasaExample5Data.CreateFactors(), PasaExample5Data.Assumptions);

        // Floor doesn't bind, but floor value should still be visible
        Assert.False(adjusted[0].AntiFrankingAppliedFemale);
        Assert.True(adjusted[0].GmpFloorFemale > 0m,
            "GMP Floor should be reported even when floor doesn't bind");
    }

    [Fact]
    public void AntiFranking_DeferredYears_NoFloorFields()
    {
        var gmp = PasaExample5Data.CreateGmpResult();

        var entries = new List<CashFlowEntry>
        {
            new CashFlowEntry(
                TaxYear: 2010,
                StatusMale: GmpStatus.Deferred,
                Pre88GmpMale: 776.56m, Post88GmpMale: 1141.65m,
                TotalGmpMale: 1918.21m, ExcessMale: 100m,
                TotalPensionMale: 2018.21m,
                StatusFemale: GmpStatus.Deferred,
                Pre88GmpFemale: 776.56m, Post88GmpFemale: 1243.54m,
                TotalGmpFemale: 2020.10m, ExcessFemale: 100m,
                TotalPensionFemale: 2120.10m,
                Post88GmpIncFactor: 0.025m, ExcessIncFactor: 0.035m),
        };

        var adjusted = AntiFrankingCalculator.ApplyFloor(
            entries.AsReadOnly(), gmp, 19958.33m, 19856.44m,
            PasaExample5Data.CreateFactors(), PasaExample5Data.Assumptions);

        // Deferred: no AF fields set
        Assert.False(adjusted[0].AntiFrankingAppliedMale);
        Assert.False(adjusted[0].AntiFrankingAppliedFemale);
        Assert.Equal(0m, adjusted[0].GmpFloorMale);
        Assert.Equal(0m, adjusted[0].GmpFloorFemale);
    }

    // ── Barber scaling / RawDifference fields ───────────────────────

    [Fact]
    public void Case4_RawDifference_ConsistentWithCompensation()
    {
        // Case 4 has Barber proportions < 1, so RawDifference should differ
        // from CompensationCashFlow. Verify relationship:
        // CompensationCashFlow ≈ RawDifference * BarberProportion (approximately)
        var gmp = GmpCalculator.CalculateGmp(
            Case4Data.Member, GmpRevaluationMethod.Section148, Case4Data.CreateFactors());
        var cashFlow = CashFlowBuilder.Build(
            gmp, Case4Data.Member, Case4Data.Scheme, Case4Data.CreateFactors());
        var (entries, _) = CompensationCalculator.Calculate(
            cashFlow, Case4Data.Member.Sex, Case4Data.Scheme, Case4Data.CreateFactors(),
            gmp.BarberWindowProportion, gmp.BarberServiceProportion);

        // All entries should carry the Barber proportions
        foreach (var e in entries)
        {
            Assert.Equal(gmp.BarberWindowProportion, e.BarberGmpProportion);
            Assert.Equal(gmp.BarberServiceProportion, e.BarberServiceProportion);
        }

        // For active PIP years (where compensation != 0), RawDifference should
        // be populated and |RawDifference| >= |CompensationCashFlow| when proportions < 1
        var activeYears = entries.Where(e => e.CompensationCashFlow != 0m).ToList();
        Assert.True(activeYears.Count > 0, "Should have active compensation years");

        foreach (var e in activeYears)
        {
            // RawDifference is unscaled, CompensationCashFlow is Barber-scaled
            // When Barber proportions < 1, raw >= scaled (for positive values)
            if (e.CompensationCashFlow > 0)
            {
                Assert.True(e.RawDifference >= e.CompensationCashFlow,
                    $"Year {e.TaxYear}: RawDifference ({e.RawDifference}) should be >= " +
                    $"CompensationCashFlow ({e.CompensationCashFlow}) when Barber < 1");
            }
        }
    }

    [Fact]
    public void BarberProportionsAt1_RawDifferenceEqualsCompensation()
    {
        // With Barber proportions = 1 (default), RawDifference == CompensationCashFlow
        var gmp = GmpCalculator.CalculateGmp(
            Case4Data.Member, GmpRevaluationMethod.Section148, Case4Data.CreateFactors());
        var cashFlow = CashFlowBuilder.Build(
            gmp, Case4Data.Member, Case4Data.Scheme, Case4Data.CreateFactors());

        // Pass proportions = 1 (no Barber scaling)
        var (entries, _) = CompensationCalculator.Calculate(
            cashFlow, Case4Data.Member.Sex, Case4Data.Scheme, Case4Data.CreateFactors(),
            barberGmpProportion: 1m, barberServiceProportion: 1m);

        foreach (var e in entries)
        {
            Assert.Equal(1m, e.BarberGmpProportion);
            Assert.Equal(1m, e.BarberServiceProportion);
            Assert.Equal(e.RawDifference, e.CompensationCashFlow);
        }
    }

    // ── Per-year interest fields ────────────────────────────────────

    [Fact]
    public void InterestPerYear_SumsToTotal()
    {
        // Run full pipeline with settlement date, verify per-year interest sums to total
        var result = GmpCalculator.Calculate(
            Case4Data.Member, Case4Data.Scheme, Case4Data.CreateFactors(),
            settlementDate: new DateTime(2025, 12, 1));

        Assert.True(result.InterestOnArrears > 0m, "Should have positive interest");

        // Sum per-year interest amounts
        decimal sumOfPerYear = result.Compensation
            .Where(c => c.InterestAmount > 0m)
            .Sum(c => c.InterestAmount);

        Assert.Equal(result.InterestOnArrears, sumOfPerYear);
    }

    [Fact]
    public void InterestPerYear_RateAndAmountPopulated()
    {
        var result = GmpCalculator.Calculate(
            Case4Data.Member, Case4Data.Scheme, Case4Data.CreateFactors(),
            settlementDate: new DateTime(2025, 12, 1));

        // Years with positive compensation before settlement should have interest
        var yearsWithInterest = result.Compensation
            .Where(c => c.InterestAmount > 0m)
            .ToList();

        Assert.True(yearsWithInterest.Count > 0, "Should have years with interest");

        foreach (var c in yearsWithInterest)
        {
            Assert.True(c.InterestRate > 0m,
                $"Year {c.TaxYear}: InterestRate should be positive when InterestAmount > 0");
            Assert.True(c.InterestAmount > 0m);

            // Verify simple interest formula: amount = compensation × rate × years
            int settlementYear = TaxYearHelper.TaxYearFromDate(new DateTime(2025, 12, 1));
            int years = settlementYear - c.TaxYear;
            decimal expectedAmount = Math.Round(c.CompensationCashFlow * c.InterestRate * years, 2);
            Assert.Equal(expectedAmount, c.InterestAmount);
        }
    }

    [Fact]
    public void NoSettlementDate_NoInterestFields()
    {
        // Without settlement date, interest fields should remain at defaults
        var result = GmpCalculator.Calculate(
            Case4Data.Member, Case4Data.Scheme, Case4Data.CreateFactors());

        Assert.Equal(0m, result.InterestOnArrears);
        Assert.All(result.Compensation, c =>
        {
            Assert.Equal(0m, c.InterestRate);
            Assert.Equal(0m, c.InterestAmount);
        });
    }

    [Fact]
    public void NegativeCompensationYears_NoInterestFields()
    {
        // Years with negative compensation (member advantaged) should not accrue interest
        var result = GmpCalculator.Calculate(
            Case4Data.Member, Case4Data.Scheme, Case4Data.CreateFactors(),
            settlementDate: new DateTime(2025, 12, 1));

        var negativeYears = result.Compensation
            .Where(c => c.CompensationCashFlow < 0m)
            .ToList();

        // Case 4 may not have negative years, but verify the constraint holds
        foreach (var c in negativeYears)
        {
            Assert.Equal(0m, c.InterestRate);
            Assert.Equal(0m, c.InterestAmount);
        }
    }

    // ── Full pipeline integration ───────────────────────────────────

    [Fact]
    public void FullPipeline_WithAF_FieldsOnResult()
    {
        // Run full pipeline with anti-franking enabled, verify AF fields appear on result
        var schemeWithAF = PasaExample5Data.Scheme with { AntiFrankingApplies = true };
        var gmp = PasaExample5Data.CreateGmpResult();

        var result = GmpCalculator.Calculate(
            PasaExample5Data.Member, schemeWithAF, PasaExample5Data.CreateFactors());

        // Under separate increase method, AF doesn't bind — but fields should still
        // show the floor values for cross-engine comparison
        var pipYears = result.CashFlow
            .Where(cf => cf.StatusFemale == GmpStatus.InPayment)
            .ToList();

        Assert.True(pipYears.Count > 0, "Should have PIP years");

        // GmpFloor should be populated for PIP years
        foreach (var cf in pipYears)
        {
            Assert.True(cf.GmpFloorFemale > 0m,
                $"Year {cf.TaxYear}: GmpFloorFemale should be > 0 during PIP");
        }
    }
}
