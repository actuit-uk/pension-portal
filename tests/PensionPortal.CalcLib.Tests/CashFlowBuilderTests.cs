using PensionPortal.CalcLib;
using PensionPortal.CalcLib.Internal;
using PensionPortal.CalcLib.Tests.TestData;

namespace PensionPortal.CalcLib.Tests;

public class CashFlowBuilderTests
{
    private static GmpResult GetCase4Gmp() =>
        GmpCalculator.CalculateGmp(
            Case4Data.Member, GmpRevaluationMethod.Section148, Case4Data.CreateFactors());

    private static IReadOnlyList<CashFlowEntry> BuildCase4CashFlow()
    {
        var gmp = GetCase4Gmp();
        return CashFlowBuilder.Build(gmp, Case4Data.Member, Case4Data.CreateFactors(), Case4Data.Assumptions);
    }

    [Fact]
    public void Case4_CashFlow_StartsAtLeavingYear()
    {
        var cf = BuildCase4CashFlow();
        Assert.Equal(2002, cf[0].TaxYear);
    }

    [Fact]
    public void Case4_CashFlow_EndsAtProjectionYear()
    {
        var cf = BuildCase4CashFlow();
        Assert.Equal(2026, cf[^1].TaxYear);
        Assert.Equal(25, cf.Count); // 2002 to 2026 inclusive
    }

    [Fact]
    public void Case4_ExitYear_HasCorrectStatus()
    {
        var cf = BuildCase4CashFlow();
        var exit = cf[0];
        Assert.Equal(GmpStatus.Exit, exit.StatusMale);
        Assert.Equal(GmpStatus.Exit, exit.StatusFemale);
    }

    [Fact]
    public void Case4_DeferredYears_MaleAndFemaleCorrect()
    {
        var cf = BuildCase4CashFlow();
        // 2003 = first DEF year for both
        var def2003 = cf.First(e => e.TaxYear == 2003);
        Assert.Equal(GmpStatus.Deferred, def2003.StatusMale);
        Assert.Equal(GmpStatus.Deferred, def2003.StatusFemale);

        // 2010 = last DEF year for female (enters PIP 2011)
        var def2010 = cf.First(e => e.TaxYear == 2010);
        Assert.Equal(GmpStatus.Deferred, def2010.StatusMale);
        Assert.Equal(GmpStatus.Deferred, def2010.StatusFemale);
    }

    [Fact]
    public void Case4_FemalePipStartsAt2011()
    {
        var cf = BuildCase4CashFlow();
        var pip2011 = cf.First(e => e.TaxYear == 2011);
        Assert.Equal(GmpStatus.Deferred, pip2011.StatusMale);  // Male still DEF
        Assert.Equal(GmpStatus.InPayment, pip2011.StatusFemale);
    }

    [Fact]
    public void Case4_MalePipStartsAt2016()
    {
        var cf = BuildCase4CashFlow();
        var pip2016 = cf.First(e => e.TaxYear == 2016);
        Assert.Equal(GmpStatus.InPayment, pip2016.StatusMale);
        Assert.Equal(GmpStatus.InPayment, pip2016.StatusFemale);
    }

    [Fact]
    public void Case4_DeferredValues_MatchAtLeaving()
    {
        var cf = BuildCase4CashFlow();
        var gmp = GetCase4Gmp();
        // All DEF years should have at-leaving values
        foreach (var entry in cf.Where(e => e.StatusMale == GmpStatus.Deferred))
        {
            Assert.Equal(gmp.MaleAtLeaving.Pre88Annual, entry.Pre88GmpMale);
            Assert.Equal(gmp.MaleAtLeaving.Post88Annual, entry.Post88GmpMale);
        }
    }

    [Fact]
    public void Case4_FemaleFirstPip_UsesRevaluedValues()
    {
        var cf = BuildCase4CashFlow();
        var pip2011 = cf.First(e => e.TaxYear == 2011);
        // Female revalued pre-88
        Assert.Equal(Case4Data.Expected.Pre88FemaleRevalued, pip2011.Pre88GmpFemale);
        // Female revalued post-88
        Assert.Equal(Case4Data.Expected.Post88FemalePIP2011, pip2011.Post88GmpFemale);
    }

    [Fact]
    public void Case4_MaleFirstPip_UsesRevaluedValues()
    {
        var cf = BuildCase4CashFlow();
        var pip2016 = cf.First(e => e.TaxYear == 2016);
        Assert.Equal(Case4Data.Expected.Pre88MaleRevalued, pip2016.Pre88GmpMale);
        Assert.Equal(Case4Data.Expected.Post88MalePIP2016, pip2016.Post88GmpMale);
    }

    [Fact]
    public void Case4_FemalePost88_IncreasesAtLPI3()
    {
        var cf = BuildCase4CashFlow();
        // 2012: 1928.16 * (1 + 0.03) = 1986.00
        Assert.Equal(Case4Data.Expected.Post88FemalePIP2012,
            cf.First(e => e.TaxYear == 2012).Post88GmpFemale);
        // 2015
        Assert.Equal(Case4Data.Expected.Post88FemalePIP2015,
            cf.First(e => e.TaxYear == 2015).Post88GmpFemale);
        // 2016: female still increasing while male enters PIP
        Assert.Equal(Case4Data.Expected.Post88FemalePIP2016,
            cf.First(e => e.TaxYear == 2016).Post88GmpFemale);
    }

    [Fact]
    public void Case4_Pre88Gmp_StaysFlat()
    {
        var cf = BuildCase4CashFlow();
        // Pre-88 never increases, even in PIP
        foreach (var entry in cf.Where(e => e.StatusFemale == GmpStatus.InPayment))
        {
            Assert.Equal(Case4Data.Expected.Pre88FemaleRevalued, entry.Pre88GmpFemale);
        }
        foreach (var entry in cf.Where(e => e.StatusMale == GmpStatus.InPayment))
        {
            Assert.Equal(Case4Data.Expected.Pre88MaleRevalued, entry.Pre88GmpMale);
        }
    }

    [Fact]
    public void Case4_FinalYear2026_MatchesExpected()
    {
        var cf = BuildCase4CashFlow();
        var final = cf.First(e => e.TaxYear == 2026);
        Assert.Equal(Case4Data.Expected.Post88FemalePIP2026, final.Post88GmpFemale);
        Assert.Equal(Case4Data.Expected.Post88MalePIP2026, final.Post88GmpMale);
    }

    [Fact]
    public void Case4_ExcessIsZero()
    {
        var cf = BuildCase4CashFlow();
        Assert.All(cf, e =>
        {
            Assert.Equal(0m, e.ExcessMale);
            Assert.Equal(0m, e.ExcessFemale);
        });
    }

    [Fact]
    public void Case4_TotalEqualsGmpPlusExcess()
    {
        var cf = BuildCase4CashFlow();
        Assert.All(cf, e =>
        {
            Assert.Equal(e.TotalGmpMale + e.ExcessMale, e.TotalPensionMale);
            Assert.Equal(e.TotalGmpFemale + e.ExcessFemale, e.TotalPensionFemale);
        });
    }
}
