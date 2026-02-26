using PensionPortal.CalcLib;

namespace PensionPortal.CalcLib.Tests.TestData;

/// <summary>
/// Test data for GMPEQ Case 5 (Fixed rate revaluation).
/// Same member as Case 4 (Male, DOB 29 Dec 1951, CO 1986-2002) but with
/// FixedRate revaluation instead of Section 148.
/// Source: tblCalculationInput and tblCalculationResult in GMPEQ database.
/// </summary>
public static class Case5Data
{
    /// <summary>
    /// Same member as Case 4 — identical earnings and dates.
    /// </summary>
    public static MemberData Member => Case4Data.Member;

    /// <summary>
    /// Populates a DictionaryFactorProvider with S148 earnings factors (same as Case 4)
    /// plus fixed rate bands from tblFactorValues WHERE FactorTableName = 'GMPFixedRates'.
    /// </summary>
    public static DictionaryFactorProvider CreateFactors()
    {
        // Start from Case 4 factors (S148 earnings factors and LPI3 are reused)
        var f = Case4Data.CreateFactors();

        // Fixed rate revaluation bands (from GMPEQ tblFactorValues)
        f.AddFixedRate(1978, 1987, 0.085m);
        f.AddFixedRate(1988, 1992, 0.075m);
        f.AddFixedRate(1993, 1996, 0.07m);
        f.AddFixedRate(1997, 2001, 0.0625m);
        f.AddFixedRate(2002, 2006, 0.045m);
        f.AddFixedRate(2007, 2011, 0.04m);
        f.AddFixedRate(2012, 2016, 0.0475m);
        f.AddFixedRate(2017, 2099, 0.035m);

        return f;
    }

    /// <summary>
    /// Scheme configuration for Case 5: same as Case 4 but with FixedRate revaluation.
    /// </summary>
    public static SchemeConfig Scheme => new(
        PreEqNraMale: 65,
        PreEqNraFemale: 60,
        PostEqNra: 65,
        DateOfEqualisation: new DateTime(1990, 5, 17),
        AccrualRateDenominator: 80,
        PipMethod: PipIncreaseMethod.LPI3,
        GmpRevMethod: GmpRevaluationMethod.FixedRate,
        Assumptions: Case4Data.Assumptions);

    /// <summary>
    /// Expected results from tblCalculationResult WHERE CaseID = 5.
    /// GMP at leaving is identical to Case 4 (same member, same earnings, same S148 factors).
    /// Only revaluation differs (FixedRate 4.5% compound vs Section148 factor lookup).
    /// </summary>
    public static class Expected
    {
        // GMP at date of leaving — identical to Case 4
        public const decimal TotalGmpMalePA = Case4Data.Expected.TotalGmpMalePA;      // 1576.97
        public const decimal TotalGmpFemalePA = Case4Data.Expected.TotalGmpFemalePA;  // 1815.91
        public const decimal TotalGmpMalePW = Case4Data.Expected.TotalGmpMalePW;      // 30.33
        public const decimal TotalGmpFemalePW = Case4Data.Expected.TotalGmpFemalePW;  // 34.92
        public const decimal Pre88GmpMalePW = Case4Data.Expected.Pre88GmpMalePW;      // 5.75
        public const decimal Pre88GmpFemalePW = Case4Data.Expected.Pre88GmpFemalePW;  // 6.62

        // Revaluation factors — Fixed rate 4.5% compound
        // Male: (1.045)^13 = 1.7722, Female: (1.045)^8 = 1.4221
        public const decimal RevFactorMale = 1.772m;   // rounded to 3dp like Case 4
        public const decimal RevFactorFemale = 1.422m;

        // Revalued GMP (per week)
        public const decimal TotalRevaluedGmpMalePW = 53.75m;
        public const decimal TotalRevaluedGmpFemalePW = 49.66m;
        public const decimal Pre88RevaluedGmpMalePW = 10.19m;
        public const decimal Pre88RevaluedGmpFemalePW = 9.41m;

        // Compensation from legacy system
        public const decimal CompensationTo2026 = 12355.20m;
    }
}
