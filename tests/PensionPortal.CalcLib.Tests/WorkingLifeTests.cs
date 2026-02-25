using PensionPortal.CalcLib.Internal;
using PensionPortal.CalcLib.Tests.TestData;

namespace PensionPortal.CalcLib.Tests;

public class WorkingLifeTests
{
    [Fact]
    public void Case4_MaleWorkingLife_Is38()
    {
        // DOB 29 Dec 1951, male GMP age 65
        // 16th birthday: 29 Dec 1967, tax year 1967
        // But 1967 < 1978, so start = 1978
        // GMP age 65: 29 Dec 2016, tax year 2016
        // Working life = 2016 - 1978 = 38
        var result = WorkingLife.Calculate(
            new DateTime(1951, 12, 29), gmpPayableAge: 65);
        Assert.Equal(Case4Data.Expected.WorkingLifeMale, result);
    }

    [Fact]
    public void Case4_FemaleWorkingLife_Is33()
    {
        // DOB 29 Dec 1951, female GMP age 60
        // Start = 1978 (same as male, capped)
        // GMP age 60: 29 Dec 2011, tax year 2011
        // Working life = 2011 - 1978 = 33
        var result = WorkingLife.Calculate(
            new DateTime(1951, 12, 29), gmpPayableAge: 60);
        Assert.Equal(Case4Data.Expected.WorkingLifeFemale, result);
    }

    [Fact]
    public void YoungMember_StartsCappedAt1978()
    {
        // DOB 1 Jan 1960, 16th birthday 1 Jan 1976 (tax year 1975)
        // Capped to 1978
        var result = WorkingLife.Calculate(
            new DateTime(1960, 1, 1), gmpPayableAge: 65);
        // GMP age 65: 1 Jan 2025, tax year 2024
        // Working life = 2024 - 1978 = 46
        Assert.Equal(46, result);
    }

    [Fact]
    public void MemberBornAfter1962_StartsAfter1978()
    {
        // DOB 1 Jul 1963, 16th birthday 1 Jul 1979, tax year 1979
        // 1979 > 1978, so start = 1979
        var result = WorkingLife.Calculate(
            new DateTime(1963, 7, 1), gmpPayableAge: 65);
        // GMP age 65: 1 Jul 2028, tax year 2028
        // Working life = 2028 - 1979 = 49
        Assert.Equal(49, result);
    }
}
