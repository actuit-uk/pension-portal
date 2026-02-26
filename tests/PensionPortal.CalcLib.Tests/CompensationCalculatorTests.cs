using PensionPortal.CalcLib;
using PensionPortal.CalcLib.Internal;
using PensionPortal.CalcLib.Tests.TestData;

namespace PensionPortal.CalcLib.Tests;

public class CompensationCalculatorTests
{
    private static IReadOnlyList<CashFlowEntry> GetCase4CashFlow()
    {
        var gmp = GmpCalculator.CalculateGmp(
            Case4Data.Member, GmpRevaluationMethod.Section148, Case4Data.CreateFactors());
        return CashFlowBuilder.Build(gmp, Case4Data.Member, Case4Data.Scheme, Case4Data.CreateFactors());
    }

    private static (IReadOnlyList<CompensationEntry> Entries, decimal Total) GetCase4Compensation()
    {
        var cashFlow = GetCase4CashFlow();
        return CompensationCalculator.Calculate(
            cashFlow, Case4Data.Member.Sex, Case4Data.Scheme, Case4Data.CreateFactors());
    }

    [Fact]
    public void Case4_CompensationEntryCount_MatchesCashFlow()
    {
        var (entries, _) = GetCase4Compensation();
        Assert.Equal(25, entries.Count); // 2002 to 2026 inclusive
    }

    [Fact]
    public void Case4_FirstYear_DiscountFactorIsOne()
    {
        var (entries, _) = GetCase4Compensation();
        Assert.Equal(1.0m, entries[0].DiscountFactor);
    }

    [Fact]
    public void Case4_DiscountFactors_DecreaseOverTime()
    {
        var (entries, _) = GetCase4Compensation();
        for (int i = 1; i < entries.Count; i++)
        {
            Assert.True(entries[i].DiscountFactor < entries[i - 1].DiscountFactor,
                $"DF should decrease: year {entries[i].TaxYear}");
        }
    }

    [Fact]
    public void Case4_MaleMember_CompensationNeverNegative()
    {
        // For this male member, the female (opposite-sex) post-88 GMP is always
        // higher, so signed compensation is non-negative in all active years.
        // (For female members, compensation CAN be negative — that's correct C2 behaviour.)
        var (entries, _) = GetCase4Compensation();
        Assert.All(entries, e => Assert.True(e.CompensationCashFlow >= 0m,
            $"Year {e.TaxYear}: for this male member, compensation should be non-negative"));
    }

    [Fact]
    public void Case4_PrePipYears_ZeroCompensation()
    {
        var (entries, _) = GetCase4Compensation();
        // 2002-2011: before female second PIP year (female PIP starts 2011, second is 2012)
        foreach (var e in entries.Where(e => e.TaxYear <= 2011))
        {
            Assert.Equal(0m, e.CompensationCashFlow);
            Assert.Equal(0m, e.ActualCashFlow);
            Assert.Equal(0m, e.OppSexCashFlow);
        }
    }

    [Fact]
    public void Case4_FemaleSecondPipYear_CompensationStarts()
    {
        var (entries, _) = GetCase4Compensation();
        var e2012 = entries.First(e => e.TaxYear == 2012);
        // Female in second PIP year, male still deferred → actual=0, oppSex=female post-88
        Assert.Equal(0m, e2012.ActualCashFlow);
        Assert.Equal(Case4Data.Expected.Post88FemalePIP2012, e2012.OppSexCashFlow);
        Assert.Equal(Case4Data.Expected.Post88FemalePIP2012, e2012.CompensationCashFlow);
    }

    [Fact]
    public void Case4_MaleSecondPipYear_ActualKicksIn()
    {
        var (entries, _) = GetCase4Compensation();
        // Male PIP starts 2016, second PIP year = 2017
        var e2017 = entries.First(e => e.TaxYear == 2017);
        Assert.True(e2017.ActualCashFlow > 0, "Male post-88 should appear from 2017");
        Assert.True(e2017.OppSexCashFlow > e2017.ActualCashFlow,
            "Female post-88 should still exceed male in 2017");
    }

    [Fact]
    public void Case4_2016_MaleFirstPip_ActualStillZero()
    {
        var (entries, _) = GetCase4Compensation();
        // 2016 is male's FIRST PIP year — no increase yet, so actual = 0
        var e2016 = entries.First(e => e.TaxYear == 2016);
        Assert.Equal(0m, e2016.ActualCashFlow);
        Assert.Equal(Case4Data.Expected.Post88FemalePIP2016, e2016.OppSexCashFlow);
    }

    [Fact]
    public void Case4_TotalCompensation_MatchesLegacy()
    {
        // Legacy GMPEQ sum of undiscounted compensation cashflows.
        // The legacy system was incomplete so this may need revision —
        // treat as a baseline, not gospel.
        var (_, total) = GetCase4Compensation();
        Assert.Equal(Case4Data.Expected.CompensationTo2026, total);
    }

    [Fact]
    public void Case4_TotalCompensation_EqualsSumOfEntries()
    {
        var (entries, total) = GetCase4Compensation();
        decimal sum = entries.Sum(e => e.CompensationCashFlow);
        Assert.Equal(total, Math.Round(sum, 2));
    }

    [Fact]
    public void Case4_FinalYear_MatchesExpectedAmounts()
    {
        var (entries, _) = GetCase4Compensation();
        var final2026 = entries.First(e => e.TaxYear == 2026);
        Assert.Equal(Case4Data.Expected.Post88MalePIP2026, final2026.ActualCashFlow);
        Assert.Equal(Case4Data.Expected.Post88FemalePIP2026, final2026.OppSexCashFlow);
    }

    [Fact]
    public void Case4_DiscountRate_Uses2_5Percent()
    {
        var (entries, _) = GetCase4Compensation();
        // No specific discount rates seeded, so all use future assumption (2.5%)
        Assert.All(entries, e => Assert.Equal(0.025m, e.DiscountRate));
    }
}
