using PensionPortal.CalcLib;

namespace PensionPortal.CalcLib.Tests.TestData;

/// <summary>
/// Test data for GMPEQ Case 4 (Section 148 revaluation).
/// Male, DOB 29 Dec 1951, CO period 6 Apr 1986 to 10 Nov 2002.
/// Source: tblCalculationInput and tblCalculationResult in GMPEQ database.
/// </summary>
public static class Case4Data
{
    public static MemberData Member => new(
        Sex: Sex.Male,
        DateOfBirth: new DateTime(1951, 12, 29),
        DateCOStart: new DateTime(1986, 4, 6),
        DateCOEnd: new DateTime(2002, 11, 10),
        DateOfLeaving: new DateTime(2002, 11, 10),
        DateOfRetirement: null,
        DateOfDeath: null,
        Earnings: new Dictionary<int, decimal>
        {
            // Pre-1988: contracted-out NICs (only 1987 has data)
            [1987] = 544.66m,
            // Post-1988: band earnings
            [1988] = 11024m,
            [1989] = 12204m,
            [1990] = 10586m,
            [1991] = 16112m,
            [1992] = 17407m,
            [1993] = 18233m,
            [1994] = 18985m,
            [1995] = 19643m,
            [1996] = 20329m,
            [1997] = 21100m,
        });

    /// <summary>
    /// Populates a DictionaryFactorProvider with the S148 earnings factors
    /// needed for Case 4 (Order 2002 and revaluation orders).
    /// </summary>
    public static DictionaryFactorProvider CreateFactors()
    {
        var f = new DictionaryFactorProvider();

        // S148 Earnings Factor Order 2002 (for GMP at date of leaving)
        f.AddEarningsFactor(1979, 2002, 500.7m);
        f.AddEarningsFactor(1980, 2002, 430.2m);
        f.AddEarningsFactor(1981, 2002, 342.9m);
        f.AddEarningsFactor(1982, 2002, 270.9m);
        f.AddEarningsFactor(1983, 2002, 236.9m);
        f.AddEarningsFactor(1984, 2002, 212.8m);
        f.AddEarningsFactor(1985, 2002, 189.7m);
        f.AddEarningsFactor(1986, 2002, 171.7m);
        f.AddEarningsFactor(1987, 2002, 149.5m);
        f.AddEarningsFactor(1988, 2002, 132.3m);
        f.AddEarningsFactor(1989, 2002, 113.7m);
        f.AddEarningsFactor(1990, 2002, 92.9m);
        f.AddEarningsFactor(1991, 2002, 79.8m);
        f.AddEarningsFactor(1992, 2002, 63.3m);
        f.AddEarningsFactor(1993, 2002, 53.3m);
        f.AddEarningsFactor(1994, 2002, 46.0m);
        f.AddEarningsFactor(1995, 2002, 41.6m);
        f.AddEarningsFactor(1996, 2002, 35.7m);
        f.AddEarningsFactor(1997, 2002, 32.0m);

        // S148 revaluation factors for GMP payable age
        // Male: tax year of leaving+1 = 2003, GMP age 65 = Dec 2016, ty before = 2015
        f.AddEarningsFactor(2003, 2015, 42.2m);
        // Female: tax year of leaving+1 = 2003, GMP age 60 = Dec 2011, ty before = 2010
        f.AddEarningsFactor(2003, 2010, 31.0m);

        return f;
    }

    // Expected results from tblCalculationResult
    public static class Expected
    {
        public const int WorkingLifeMale = 38;
        public const int WorkingLifeFemale = 33;
        public const int TaxYearForEarnings = 2002;

        // GMP at date of leaving (per annum)
        public const decimal Pre88GmpMalePA = 298.99m;
        public const decimal Pre88GmpFemalePA = 344.3m;
        public const decimal TotalGmpMalePA = 1576.97m;
        public const decimal TotalGmpFemalePA = 1815.91m;

        // GMP at date of leaving (per week)
        public const decimal Pre88GmpMalePW = 5.75m;
        public const decimal Pre88GmpFemalePW = 6.62m;
        public const decimal TotalGmpMalePW = 30.33m;
        public const decimal TotalGmpFemalePW = 34.92m;
        public const decimal Post88GmpMalePW = 24.58m;
        public const decimal Post88GmpFemalePW = 28.3m;

        // Revaluation
        public const decimal RevFactorMale = 1.422m;
        public const decimal RevFactorFemale = 1.31m;
        public const decimal TotalRevaluedGmpMalePW = 43.13m;
        public const decimal TotalRevaluedGmpFemalePW = 45.75m;
        public const decimal Pre88RevaluedGmpMalePW = 8.18m;
        public const decimal Pre88RevaluedGmpFemalePW = 8.67m;

        // Compensation
        public const decimal CompensationTo2026 = 14323.6m;
    }
}
