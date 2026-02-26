using PensionPortal.CalcLib;
using PensionPortal.CalcLib.Internal;
using PensionPortal.CalcLib.Tests.TestData;

namespace PensionPortal.CalcLib.Tests;

public class InterestCalculatorTests
{
    [Fact]
    public void SimpleInterest_KnownValues()
    {
        // £100 compensation in year 2020, settlement in 2025 (5 years)
        // Base rate 0.5% + 1% = 1.5%, simple interest = 100 * 0.015 * 5 = £7.50
        var entries = new List<CompensationEntry>
        {
            MakeEntry(2020, 100m),
        };
        var factors = new DictionaryFactorProvider();
        factors.AddBaseRate(2020, 0.005m);

        decimal interest = InterestCalculator.Calculate(entries, 2025, factors, 0.01m);

        Assert.Equal(7.50m, interest);
    }

    [Fact]
    public void MultipleYears_SummedCorrectly()
    {
        // Year 2018: £200 comp, 4 years to settlement at base rate 0.75%
        //   Interest: 200 * 0.0175 * 4 = 14.00
        // Year 2019: £150 comp, 3 years to settlement at base rate 0.75%
        //   Interest: 150 * 0.0175 * 3 = 7.88 (rounded)
        // Total: 21.88
        var entries = new List<CompensationEntry>
        {
            MakeEntry(2018, 200m),
            MakeEntry(2019, 150m),
        };
        var factors = new DictionaryFactorProvider();
        factors.AddBaseRate(2018, 0.0075m);
        factors.AddBaseRate(2019, 0.0075m);

        decimal interest = InterestCalculator.Calculate(entries, 2022, factors, 0.01m);

        // 200 * 0.0175 * 4 = 14.00
        // 150 * 0.0175 * 3 = 7.875 → 7.88
        Assert.Equal(14.00m + 7.88m, interest);
    }

    [Fact]
    public void NegativeCompensation_NotIncluded()
    {
        // Only positive compensation accrues interest
        var entries = new List<CompensationEntry>
        {
            MakeEntry(2020, -50m),  // Member was advantaged this year
            MakeEntry(2021, 100m),  // Member was disadvantaged
        };
        var factors = new DictionaryFactorProvider();
        factors.AddBaseRate(2020, 0.01m);
        factors.AddBaseRate(2021, 0.01m);

        decimal interest = InterestCalculator.Calculate(entries, 2025, factors, 0.01m);

        // Only year 2021: 100 * 0.02 * 4 = 8.00
        Assert.Equal(8.00m, interest);
    }

    [Fact]
    public void FutureYears_Excluded()
    {
        // Compensation at or after settlement year should not accrue interest
        var entries = new List<CompensationEntry>
        {
            MakeEntry(2020, 100m),
            MakeEntry(2025, 100m),  // Settlement year — excluded
            MakeEntry(2026, 100m),  // Future — excluded
        };
        var factors = new DictionaryFactorProvider();
        factors.AddBaseRate(2020, 0.01m);

        decimal interest = InterestCalculator.Calculate(entries, 2025, factors, 0.01m);

        // Only year 2020: 100 * 0.02 * 5 = 10.00
        Assert.Equal(10.00m, interest);
    }

    [Fact]
    public void FallbackBaseRate_UsedWhenNoFactor()
    {
        var entries = new List<CompensationEntry>
        {
            MakeEntry(2020, 100m),
        };
        var factors = new DictionaryFactorProvider(); // No base rates added

        // Fallback rate = 3%, so interest rate = 3% + 1% = 4%
        decimal interest = InterestCalculator.Calculate(entries, 2025, factors, 0.03m);

        // 100 * 0.04 * 5 = 20.00
        Assert.Equal(20.00m, interest);
    }

    [Fact]
    public void ZeroCompensation_NoInterest()
    {
        var entries = new List<CompensationEntry>
        {
            MakeEntry(2020, 0m),
        };
        var factors = new DictionaryFactorProvider();
        factors.AddBaseRate(2020, 0.05m);

        decimal interest = InterestCalculator.Calculate(entries, 2025, factors, 0.01m);

        Assert.Equal(0m, interest);
    }

    [Fact]
    public void FullPipeline_WithSettlement_HasInterest()
    {
        // Run the full pipeline with a settlement date and verify interest > 0
        var result = GmpCalculator.Calculate(
            Case4Data.Member, Case4Data.Scheme, Case4Data.CreateFactors(),
            settlementDate: new DateTime(2025, 12, 1));

        Assert.True(result.InterestOnArrears > 0,
            "Case 4 male (disadvantaged) should have positive interest on arrears");
        Assert.Equal(result.TotalCompensation + result.InterestOnArrears, result.TotalWithInterest);
    }

    [Fact]
    public void FullPipeline_NoSettlement_ZeroInterest()
    {
        // Without a settlement date, interest should be zero
        var result = GmpCalculator.Calculate(
            Case4Data.Member, Case4Data.Scheme, Case4Data.CreateFactors());

        Assert.Equal(0m, result.InterestOnArrears);
        Assert.Equal(result.TotalCompensation, result.TotalWithInterest);
    }

    private static CompensationEntry MakeEntry(int taxYear, decimal compensation)
    {
        return new CompensationEntry(
            TaxYear: taxYear,
            ActualCashFlow: 0m,
            OppSexCashFlow: compensation,
            CompensationCashFlow: compensation,
            DiscountRate: 0.025m,
            DiscountFactor: 1m);
    }
}
