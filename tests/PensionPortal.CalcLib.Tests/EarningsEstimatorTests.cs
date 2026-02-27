using PensionPortal.CalcLib.Tests.TestData;

namespace PensionPortal.CalcLib.Tests;

public class EarningsEstimatorTests
{
    // Minimal factor provider — EarningsEstimator doesn't use it, but the signature requires it.
    private static readonly IFactorProvider EmptyFactors = new DictionaryFactorProvider();

    // ─── CO Period Clamping ────────────────────────────────────────────

    [Fact]
    public void JoinedBefore1978_CoStartClampedTo6Apr1978()
    {
        var result = EarningsEstimator.Estimate(
            Sex.Male,
            new DateTime(1940, 1, 1),
            new DateTime(1965, 6, 1),  // joined well before 1978
            new DateTime(1995, 3, 31),
            15000m, EmptyFactors);

        Assert.Equal(new DateTime(1978, 4, 6), result.DateCOStart);
    }

    [Fact]
    public void LeftAfter1997_CoEndClampedTo5Apr1997()
    {
        var result = EarningsEstimator.Estimate(
            Sex.Male,
            new DateTime(1951, 12, 29),
            new DateTime(1986, 4, 6),
            new DateTime(2002, 11, 10),
            15000m, EmptyFactors);

        Assert.Equal(new DateTime(1997, 4, 5), result.DateCOEnd);
    }

    [Fact]
    public void DateOfLeaving_PreservedUnchanged()
    {
        var dateLeft = new DateTime(2002, 11, 10);
        var result = EarningsEstimator.Estimate(
            Sex.Male,
            new DateTime(1951, 12, 29),
            new DateTime(1986, 4, 6),
            dateLeft,
            15000m, EmptyFactors);

        Assert.Equal(dateLeft, result.DateOfLeaving);
    }

    // ─── Earnings Dictionary Shape ─────────────────────────────────────

    [Fact]
    public void Pre88Entries_AreNICs_SmallValues()
    {
        var result = EarningsEstimator.Estimate(
            Sex.Male,
            new DateTime(1940, 1, 1),
            new DateTime(1980, 4, 6),
            new DateTime(1987, 4, 5),  // leaves in tax year 1986
            15000m, EmptyFactors);

        // All entries should be for tax years ≤ 1986 (pre-88 NICs)
        foreach (var (ty, val) in result.Earnings)
        {
            Assert.True(ty <= 1987, $"Unexpected post-87 tax year: {ty}");
            // NICs are band earnings × 0.0685 — much smaller than band earnings
            Assert.True(val < 2000m, $"Pre-88 NIC {val} looks too large for tax year {ty}");
            Assert.True(val > 0m, $"Pre-88 NIC should be positive for tax year {ty}");
        }
    }

    [Fact]
    public void Post88Entries_AreBandEarnings_LargerValues()
    {
        var result = EarningsEstimator.Estimate(
            Sex.Male,
            new DateTime(1960, 1, 1),
            new DateTime(1990, 4, 6),
            new DateTime(1997, 4, 5),
            15000m, EmptyFactors);

        // All entries should be for tax years ≥ 1990 (post-88 band earnings)
        foreach (var (ty, val) in result.Earnings)
        {
            Assert.True(ty >= 1990, $"Unexpected pre-90 tax year: {ty}");
            // Band earnings = min(salary, UEL) - LEL — thousands of pounds
            Assert.True(val > 2000m, $"Post-88 band earnings {val} looks too small for tax year {ty}");
        }
    }

    [Fact]
    public void MixedService_Pre88AreSmaller_Post88AreLarger()
    {
        var result = EarningsEstimator.Estimate(
            Sex.Male,
            new DateTime(1951, 12, 29),
            new DateTime(1986, 4, 6),
            new DateTime(2002, 11, 10),
            15000m, EmptyFactors);

        var pre88 = result.Earnings.Where(e => e.Key <= 1987).ToList();
        var post88 = result.Earnings.Where(e => e.Key >= 1988).ToList();

        Assert.NotEmpty(pre88);
        Assert.NotEmpty(post88);

        decimal maxPre88 = pre88.Max(e => e.Value);
        decimal minPost88 = post88.Min(e => e.Value);

        // NICs (small) should be less than band earnings (large)
        Assert.True(maxPre88 < minPost88,
            $"Max pre-88 NIC ({maxPre88}) should be less than min post-88 band earnings ({minPost88})");
    }

    // ─── Salary Margin ─────────────────────────────────────────────────

    [Fact]
    public void SalaryMargin_ScalesAllEarningsProportionally()
    {
        var baseline = EarningsEstimator.Estimate(
            Sex.Male,
            new DateTime(1951, 12, 29),
            new DateTime(1986, 4, 6),
            new DateTime(2002, 11, 10),
            15000m, EmptyFactors, salaryMargin: 0m);

        var uplifted = EarningsEstimator.Estimate(
            Sex.Male,
            new DateTime(1951, 12, 29),
            new DateTime(1986, 4, 6),
            new DateTime(2002, 11, 10),
            15000m, EmptyFactors, salaryMargin: 0.10m);

        // Every uplifted entry should be >= the baseline
        foreach (var ty in baseline.Earnings.Keys)
        {
            Assert.True(uplifted.Earnings[ty] >= baseline.Earnings[ty],
                $"Uplifted earnings for {ty} ({uplifted.Earnings[ty]}) should be >= baseline ({baseline.Earnings[ty]})");
        }
    }

    [Fact]
    public void NegativeMargin_ReducesEarnings()
    {
        var baseline = EarningsEstimator.Estimate(
            Sex.Male,
            new DateTime(1960, 1, 1),
            new DateTime(1990, 4, 6),
            new DateTime(1997, 4, 5),
            15000m, EmptyFactors, salaryMargin: 0m);

        var reduced = EarningsEstimator.Estimate(
            Sex.Male,
            new DateTime(1960, 1, 1),
            new DateTime(1990, 4, 6),
            new DateTime(1997, 4, 5),
            15000m, EmptyFactors, salaryMargin: -0.10m);

        foreach (var ty in baseline.Earnings.Keys)
        {
            Assert.True(reduced.Earnings[ty] <= baseline.Earnings[ty],
                $"Reduced earnings for {ty} ({reduced.Earnings[ty]}) should be <= baseline ({baseline.Earnings[ty]})");
        }
    }

    // ─── FinalPensionableSalary ────────────────────────────────────────

    [Fact]
    public void FinalPensionableSalary_IsSet()
    {
        var result = EarningsEstimator.Estimate(
            Sex.Male,
            new DateTime(1951, 12, 29),
            new DateTime(1986, 4, 6),
            new DateTime(2002, 11, 10),
            15000m, EmptyFactors);

        Assert.NotNull(result.FinalPensionableSalary);
        Assert.True(result.FinalPensionableSalary > 0m);
    }

    [Fact]
    public void FinalPensionableSalary_ScalesWithSalaryAnchor()
    {
        var low = EarningsEstimator.Estimate(
            Sex.Male,
            new DateTime(1951, 12, 29),
            new DateTime(1986, 4, 6),
            new DateTime(2002, 11, 10),
            10000m, EmptyFactors);

        var high = EarningsEstimator.Estimate(
            Sex.Male,
            new DateTime(1951, 12, 29),
            new DateTime(1986, 4, 6),
            new DateTime(2002, 11, 10),
            20000m, EmptyFactors);

        Assert.True(high.FinalPensionableSalary > low.FinalPensionableSalary);
    }

    // ─── Edge Cases ────────────────────────────────────────────────────

    [Fact]
    public void EntirelyPre88_OnlyNICEntries()
    {
        var result = EarningsEstimator.Estimate(
            Sex.Male,
            new DateTime(1940, 1, 1),
            new DateTime(1978, 4, 6),
            new DateTime(1986, 4, 5),
            10000m, EmptyFactors);

        Assert.All(result.Earnings, e =>
            Assert.True(e.Key <= 1987, $"Should only have pre-88 entries, got {e.Key}"));
        Assert.NotEmpty(result.Earnings);
    }

    [Fact]
    public void EntirelyPost88_OnlyBandEarningsEntries()
    {
        var result = EarningsEstimator.Estimate(
            Sex.Male,
            new DateTime(1960, 1, 1),
            new DateTime(1990, 4, 6),
            new DateTime(1997, 4, 5),
            15000m, EmptyFactors);

        Assert.All(result.Earnings, e =>
            Assert.True(e.Key >= 1990, $"Should only have post-88 entries, got {e.Key}"));
        Assert.NotEmpty(result.Earnings);
    }

    [Fact]
    public void SingleTaxYear_SingleEntry()
    {
        // Joined and left within the 1990/91 tax year
        var result = EarningsEstimator.Estimate(
            Sex.Male,
            new DateTime(1960, 1, 1),
            new DateTime(1990, 6, 1),
            new DateTime(1991, 3, 31),
            15000m, EmptyFactors);

        Assert.Single(result.Earnings);
        Assert.True(result.Earnings.ContainsKey(1990));
    }

    [Fact]
    public void PensionAtLeaving_IsNull()
    {
        var result = EarningsEstimator.Estimate(
            Sex.Male,
            new DateTime(1960, 1, 1),
            new DateTime(1990, 4, 6),
            new DateTime(1997, 4, 5),
            15000m, EmptyFactors);

        Assert.Null(result.PensionAtLeaving);
    }

    // ─── Data Integrity ────────────────────────────────────────────────

    [Fact]
    public void NiThresholds_LEL_CoversAllRequiredYears()
    {
        for (int ty = 1978; ty <= 1997; ty++)
        {
            Assert.True(PensionPortal.CalcLib.Internal.NiThresholds.LEL.ContainsKey(ty),
                $"LEL missing for tax year {ty}");
        }
    }

    [Fact]
    public void NiThresholds_UEL_CoversAllRequiredYears()
    {
        for (int ty = 1978; ty <= 1997; ty++)
        {
            Assert.True(PensionPortal.CalcLib.Internal.NiThresholds.UEL.ContainsKey(ty),
                $"UEL missing for tax year {ty}");
        }
    }

    [Fact]
    public void NiThresholds_UEL_AlwaysGreaterThanLEL()
    {
        for (int ty = 1978; ty <= 1997; ty++)
        {
            var lel = PensionPortal.CalcLib.Internal.NiThresholds.LEL[ty];
            var uel = PensionPortal.CalcLib.Internal.NiThresholds.UEL[ty];
            Assert.True(uel > lel, $"UEL ({uel}) should exceed LEL ({lel}) for tax year {ty}");
        }
    }

    [Fact]
    public void NiThresholds_EarningsIndex_CoversAllRequiredYears()
    {
        for (int ty = 1978; ty <= 1997; ty++)
        {
            Assert.True(PensionPortal.CalcLib.Internal.NiThresholds.AverageWeeklyEarnings.ContainsKey(ty),
                $"Earnings index missing for tax year {ty}");
        }
    }

    [Fact]
    public void NiThresholds_EarningsIndex_MonotonicallyIncreasing()
    {
        decimal prev = 0m;
        for (int ty = 1978; ty <= 1997; ty++)
        {
            decimal current = PensionPortal.CalcLib.Internal.NiThresholds.AverageWeeklyEarnings[ty];
            Assert.True(current > prev,
                $"Earnings index should be increasing: {ty} = {current}, previous = {prev}");
            prev = current;
        }
    }

    // ─── Round-trip: Case 4 Comparison ─────────────────────────────────

    [Fact]
    public void Case4_RoundTrip_GmpWithinTolerance()
    {
        // Case 4: Male, DOB 29 Dec 1951, CO 6 Apr 1986 to 10 Nov 2002
        // Case 4's 1990 band earnings = 10,586
        // Back-derive approximate 1990 salary: band earnings + LEL[1990]
        // = 10,586 + 2,392 = 12,978
        decimal approxSalary1990 = 12978m;

        var estimated = EarningsEstimator.Estimate(
            Sex.Male,
            new DateTime(1951, 12, 29),
            new DateTime(1986, 4, 6),
            new DateTime(2002, 11, 10),
            approxSalary1990,
            Case4Data.CreateFactors());

        var gmp = GmpCalculator.CalculateGmp(
            estimated,
            GmpRevaluationMethod.Section148,
            Case4Data.CreateFactors());

        // Actual Case 4 GMP = 1576.97 pa (male at leaving)
        // Tolerance: 25% — estimation from a single salary anchor vs actual year-by-year data
        decimal actual = Case4Data.Expected.TotalGmpMalePA;
        decimal tolerance = actual * 0.25m;

        Assert.InRange(gmp.MaleAtLeaving.TotalAnnual,
            actual - tolerance,
            actual + tolerance);
    }

    // ─── Full Pipeline Integration ─────────────────────────────────────

    [Fact]
    public void FullPipeline_EstimatedMember_ProducesValidResult()
    {
        var estimated = EarningsEstimator.Estimate(
            Sex.Male,
            new DateTime(1955, 6, 15),
            new DateTime(1982, 1, 1),
            new DateTime(1999, 12, 31),
            15000m,
            Case4Data.CreateFactors());

        var result = GmpCalculator.Calculate(
            estimated, Case4Data.Scheme, Case4Data.CreateFactors());

        Assert.True(result.Gmp.MaleAtLeaving.TotalAnnual > 0m);
        Assert.True(result.CashFlow.Count > 0);
        Assert.True(result.Compensation.Count > 0);
    }

    [Fact]
    public void HigherSalary_ProducesHigherGmp()
    {
        var low = EarningsEstimator.Estimate(
            Sex.Male,
            new DateTime(1955, 6, 15),
            new DateTime(1982, 1, 1),
            new DateTime(1999, 12, 31),
            10000m,
            Case4Data.CreateFactors());

        var high = EarningsEstimator.Estimate(
            Sex.Male,
            new DateTime(1955, 6, 15),
            new DateTime(1982, 1, 1),
            new DateTime(1999, 12, 31),
            20000m,
            Case4Data.CreateFactors());

        var gmpLow = GmpCalculator.CalculateGmp(
            low, GmpRevaluationMethod.Section148, Case4Data.CreateFactors());
        var gmpHigh = GmpCalculator.CalculateGmp(
            high, GmpRevaluationMethod.Section148, Case4Data.CreateFactors());

        Assert.True(gmpHigh.MaleAtLeaving.TotalAnnual > gmpLow.MaleAtLeaving.TotalAnnual,
            $"Higher salary should produce higher GMP: low={gmpLow.MaleAtLeaving.TotalAnnual}, high={gmpHigh.MaleAtLeaving.TotalAnnual}");
    }

    [Fact]
    public void SalaryMargin_ProducesHigherGmp()
    {
        var baseline = EarningsEstimator.Estimate(
            Sex.Male,
            new DateTime(1955, 6, 15),
            new DateTime(1982, 1, 1),
            new DateTime(1999, 12, 31),
            15000m,
            Case4Data.CreateFactors(),
            salaryMargin: 0m);

        var uplifted = EarningsEstimator.Estimate(
            Sex.Male,
            new DateTime(1955, 6, 15),
            new DateTime(1982, 1, 1),
            new DateTime(1999, 12, 31),
            15000m,
            Case4Data.CreateFactors(),
            salaryMargin: 0.10m);

        var gmpBase = GmpCalculator.CalculateGmp(
            baseline, GmpRevaluationMethod.Section148, Case4Data.CreateFactors());
        var gmpUp = GmpCalculator.CalculateGmp(
            uplifted, GmpRevaluationMethod.Section148, Case4Data.CreateFactors());

        Assert.True(gmpUp.MaleAtLeaving.TotalAnnual > gmpBase.MaleAtLeaving.TotalAnnual,
            $"10% margin should produce higher GMP: base={gmpBase.MaleAtLeaving.TotalAnnual}, uplifted={gmpUp.MaleAtLeaving.TotalAnnual}");
    }
}
