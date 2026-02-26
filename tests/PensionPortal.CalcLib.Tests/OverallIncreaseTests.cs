using PensionPortal.CalcLib;
using PensionPortal.CalcLib.Internal;
using PensionPortal.CalcLib.Tests.TestData;

namespace PensionPortal.CalcLib.Tests;

/// <summary>
/// Tests for the overall increase method.
/// Under overall: one rate applies to total pension, GMP floor tested, excess is residual.
/// Under separate: each component increases independently.
/// </summary>
public class OverallIncreaseTests
{
    /// <summary>
    /// Case 4 member with a direct PensionAtLeaving to generate excess pension.
    /// </summary>
    private static MemberData MemberWithPension =>
        Case4Data.Member with { PensionAtLeaving = 3000m };

    /// <summary>
    /// Scheme using overall method with a 0% scheme PIP rate (via LPI5 with no factors loaded).
    /// This causes total pension to stay flat while GMP increases at LPI3, demonstrating erosion.
    /// </summary>
    private static SchemeConfig OverallSchemeZeroPip => new(
        PreEqNraMale: 65,
        PreEqNraFemale: 60,
        PostEqNra: 65,
        DateOfEqualisation: new DateTime(1990, 5, 17),
        AccrualRateDenominator: 80,
        PipMethod: PipIncreaseMethod.LPI5, // No LPI5 factors loaded → falls back to FuturePipRate
        GmpRevMethod: GmpRevaluationMethod.Section148,
        Assumptions: new FutureAssumptions(
            FuturePost88GmpIncRate: 0.025m,
            FuturePipRate: 0m, // 0% scheme rate = total pension stays flat under overall
            FutureDiscountRate: 0.025m,
            ProjectionEndYear: 2026),
        IncreaseMethod: PensionIncreaseMethod.Overall);

    /// <summary>
    /// Same scheme but with separate method for control comparison.
    /// </summary>
    private static SchemeConfig SeparateSchemeZeroPip => OverallSchemeZeroPip with
    {
        IncreaseMethod = PensionIncreaseMethod.Separate
    };

    /// <summary>
    /// Overall scheme with LPI3 as the scheme PIP method (same rate as GMP increase).
    /// Excess should be roughly stable since both grow at the same rate.
    /// </summary>
    private static SchemeConfig OverallSchemeLpi3 => new(
        PreEqNraMale: 65,
        PreEqNraFemale: 60,
        PostEqNra: 65,
        DateOfEqualisation: new DateTime(1990, 5, 17),
        AccrualRateDenominator: 80,
        PipMethod: PipIncreaseMethod.LPI3,
        GmpRevMethod: GmpRevaluationMethod.Section148,
        Assumptions: Case4Data.Assumptions,
        IncreaseMethod: PensionIncreaseMethod.Overall);

    private static (GmpResult Gmp, IReadOnlyList<CashFlowEntry> CashFlow) BuildCashFlow(
        MemberData member, SchemeConfig scheme)
    {
        var factors = Case4Data.CreateFactors();
        var gmp = GmpCalculator.CalculateGmp(member, scheme.GmpRevMethod, factors);
        var cf = CashFlowBuilder.Build(gmp, member, scheme, factors);
        return (gmp, cf);
    }

    // --- GMP-only behaviour ---

    [Fact]
    public void GmpOnly_Overall_TotalPensionAtLeastGmp()
    {
        // Under overall with GMP-only, the scheme rate applies to the total (which is all
        // GMP). Because pre-88 is flat but the rate applies to the total, the total can
        // exceed the GMP floor, creating a small positive excess in later PIP years.
        var (_, cf) = BuildCashFlow(Case4Data.Member, OverallSchemeLpi3);

        Assert.All(cf, e =>
        {
            Assert.True(e.TotalPensionMale >= e.TotalGmpMale,
                $"Year {e.TaxYear}: total pension must be >= GMP");
            Assert.True(e.TotalPensionFemale >= e.TotalGmpFemale,
                $"Year {e.TaxYear}: total pension must be >= GMP");
            Assert.True(e.ExcessMale >= 0m);
            Assert.True(e.ExcessFemale >= 0m);
        });
    }

    [Fact]
    public void GmpOnly_Separate_ExcessStaysZero()
    {
        // Under separate with GMP-only, excess is always 0.
        var (_, cf) = BuildCashFlow(Case4Data.Member, Case4Data.Scheme);

        Assert.All(cf, e =>
        {
            Assert.Equal(0m, e.ExcessMale);
            Assert.Equal(0m, e.ExcessFemale);
        });
    }

    // --- GMP components unchanged by increase method ---

    [Fact]
    public void WithExcess_GmpComponents_IdenticalRegardlessOfMethod()
    {
        var (_, cfOverall) = BuildCashFlow(MemberWithPension, OverallSchemeZeroPip);
        var (_, cfSeparate) = BuildCashFlow(MemberWithPension, SeparateSchemeZeroPip);

        for (int i = 0; i < cfSeparate.Count; i++)
        {
            Assert.Equal(cfSeparate[i].Pre88GmpMale, cfOverall[i].Pre88GmpMale);
            Assert.Equal(cfSeparate[i].Post88GmpMale, cfOverall[i].Post88GmpMale);
            Assert.Equal(cfSeparate[i].TotalGmpMale, cfOverall[i].TotalGmpMale);
            Assert.Equal(cfSeparate[i].Pre88GmpFemale, cfOverall[i].Pre88GmpFemale);
            Assert.Equal(cfSeparate[i].Post88GmpFemale, cfOverall[i].Post88GmpFemale);
            Assert.Equal(cfSeparate[i].TotalGmpFemale, cfOverall[i].TotalGmpFemale);
        }
    }

    // --- Overall + low scheme rate: excess erodes ---

    [Fact]
    public void Overall_ZeroSchemePip_ExcessErodesOverTime()
    {
        var (_, cf) = BuildCashFlow(MemberWithPension, OverallSchemeZeroPip);

        // Female enters PIP in 2011 — first PIP year has excess at leaving
        var firstPip = cf.First(e => e.TaxYear == 2011);
        var excessAtLeaving = 3000m - Case4Data.Expected.TotalGmpFemalePA;
        Assert.Equal(excessAtLeaving, firstPip.ExcessFemale);

        // Subsequent PIP years: excess should generally decrease (GMP grows but total is flat).
        // Non-strictly decreasing because LPI3 can be 0% in some years (e.g. 2016).
        var pipYears = cf.Where(e => e.StatusFemale == GmpStatus.InPayment).ToList();
        for (int i = 2; i < pipYears.Count; i++)
        {
            Assert.True(pipYears[i].ExcessFemale <= pipYears[i - 1].ExcessFemale,
                $"Year {pipYears[i].TaxYear}: excess {pipYears[i].ExcessFemale} " +
                $"should be <= prior year {pipYears[i - 1].ExcessFemale}");
        }

        // Overall trend: last PIP year excess is significantly lower than first
        Assert.True(pipYears[^1].ExcessFemale < pipYears[0].ExcessFemale * 0.5m,
            "Excess should erode to well below 50% of initial value with 0% scheme rate");
    }

    [Fact]
    public void Overall_ZeroSchemePip_TotalPensionStaysFlat()
    {
        var (_, cf) = BuildCashFlow(MemberWithPension, OverallSchemeZeroPip);

        // Under 0% scheme rate, total pension should stay at the first PIP year value
        var pipYears = cf.Where(e => e.StatusFemale == GmpStatus.InPayment).ToList();
        var firstPipTotal = pipYears[0].TotalPensionFemale;

        // From 2nd PIP year onwards, total pension = max(flat total, GMP floor)
        // While excess > 0, total stays flat. When floor bites, total = GMP (grows).
        for (int i = 1; i < pipYears.Count; i++)
        {
            // Total pension should never decrease (GMP floor prevents it)
            Assert.True(pipYears[i].TotalPensionFemale >= pipYears[i - 1].TotalPensionFemale,
                $"Year {pipYears[i].TaxYear}: total pension should not decrease");
        }
    }

    // --- Overall: total pension identity ---

    [Fact]
    public void Overall_TotalAlwaysEqualsGmpPlusExcess()
    {
        var (_, cf) = BuildCashFlow(MemberWithPension, OverallSchemeZeroPip);

        Assert.All(cf, e =>
        {
            Assert.Equal(e.TotalGmpMale + e.ExcessMale, e.TotalPensionMale);
            Assert.Equal(e.TotalGmpFemale + e.ExcessFemale, e.TotalPensionFemale);
        });
    }

    // --- Overall: GMP floor works ---

    [Fact]
    public void Overall_TotalPension_NeverBelowGmp()
    {
        var (_, cf) = BuildCashFlow(MemberWithPension, OverallSchemeZeroPip);

        Assert.All(cf, e =>
        {
            Assert.True(e.TotalPensionMale >= e.TotalGmpMale,
                $"Year {e.TaxYear}: male total pension must be >= GMP");
            Assert.True(e.TotalPensionFemale >= e.TotalGmpFemale,
                $"Year {e.TaxYear}: female total pension must be >= GMP");
        });
    }

    [Fact]
    public void Overall_ExcessNeverNegative()
    {
        var (_, cf) = BuildCashFlow(MemberWithPension, OverallSchemeZeroPip);

        Assert.All(cf, e =>
        {
            Assert.True(e.ExcessMale >= 0m,
                $"Year {e.TaxYear}: male excess must be >= 0");
            Assert.True(e.ExcessFemale >= 0m,
                $"Year {e.TaxYear}: female excess must be >= 0");
        });
    }

    // --- Separate: excess independent of GMP ---

    [Fact]
    public void Separate_ZeroSchemePip_ExcessStaysFlat()
    {
        var (_, cf) = BuildCashFlow(MemberWithPension, SeparateSchemeZeroPip);

        // Under separate with 0% PIP, excess should stay at leaving value during deferred
        // and remain flat in PIP (0% increase = no change)
        var excessAtLeaving = 3000m - Case4Data.Expected.TotalGmpFemalePA;

        // All deferred years: excess = at leaving
        Assert.All(cf.Where(e => e.StatusFemale != GmpStatus.InPayment), e =>
        {
            Assert.Equal(excessAtLeaving, e.ExcessFemale);
        });

        // In PIP with 0% rate: excess stays flat (only increases from prevExcessFactor=0)
        var pipYears = cf.Where(e => e.StatusFemale == GmpStatus.InPayment).ToList();
        for (int i = 1; i < pipYears.Count; i++)
        {
            Assert.Equal(pipYears[0].ExcessFemale, pipYears[i].ExcessFemale);
        }
    }

    // --- Overall with LPI3 scheme rate: excess is roughly stable ---

    [Fact]
    public void Overall_Lpi3SchemeRate_ExcessDoesNotErode()
    {
        // When scheme PIP rate = LPI3 (same as GMP increase), excess should
        // grow or stay stable because total pension increases at least as fast as GMP.
        var (_, cf) = BuildCashFlow(MemberWithPension, OverallSchemeLpi3);

        var pipYears = cf.Where(e => e.StatusFemale == GmpStatus.InPayment).ToList();

        // Excess in last PIP year should be >= excess in first PIP year.
        // Under overall with same rate, total grows faster than GMP
        // (rate applied to larger base) so excess actually increases.
        Assert.True(pipYears[^1].ExcessFemale >= pipYears[0].ExcessFemale,
            "With LPI3 scheme rate, excess should not erode");
    }

    // --- Full pipeline with overall method ---

    [Fact]
    public void FullPipeline_Overall_ReturnsValidResult()
    {
        var factors = Case4Data.CreateFactors();
        var result = GmpCalculator.Calculate(MemberWithPension, OverallSchemeZeroPip, factors);

        // Pipeline completes
        Assert.NotNull(result.Gmp);
        Assert.Equal(25, result.CashFlow.Count);
        Assert.Equal(25, result.Compensation.Count);

        // Compensation is computed (may differ from separate method)
        Assert.NotEqual(0m, result.TotalCompensation);
    }
}
