using PensionPortal.CalcLib;

namespace PensionPortal.CalcLib.Tests.TestData;

/// <summary>
/// Test data inspired by PASA Conversion Examples - Example 5.
/// Male, deferred, DOB 15 Feb 1962, leaving 10 Jul 2006, NRD 15 Feb 2025 (age 63).
/// Total pension at leaving: £21,876.54 pa. Total GMP: £1,918.21 pa.
///
/// Note: The PASA example uses a different GMP methodology (same working life for both
/// sexes, GMP split into pre-88/1988-90/post-90). Our model uses sex-specific working
/// lives from DOB. This test data adapts the PASA figures to our model's structure,
/// providing the GMP and pension amounts directly to test the cash flow and compensation
/// logic rather than the GMP calculation from earnings.
///
/// The PASA example shows the member is disadvantaged (male post-1990 GMP £873.87
/// vs female £975.76), with a C2 uplift of £229.26 pa at NRD (0.7%).
/// </summary>
public static class PasaExample5Data
{
    public static MemberData Member => new(
        Sex: Sex.Male,
        DateOfBirth: new DateTime(1962, 2, 15),
        DateCOStart: new DateTime(1985, 6, 1),   // Approximate
        DateCOEnd: new DateTime(1997, 4, 5),
        DateOfLeaving: new DateTime(2006, 7, 10),
        DateOfRetirement: new DateTime(2025, 2, 15),
        DateOfDeath: null,
        Earnings: new Dictionary<int, decimal>(), // Not used — GMP provided directly
        PensionAtLeaving: 21876.54m);

    public static SchemeConfig Scheme => new(
        PreEqNraMale: 63,       // Example 5 has NRD at 63
        PreEqNraFemale: 60,
        PostEqNra: 63,
        DateOfEqualisation: new DateTime(1990, 5, 17),
        AccrualRateDenominator: 60,  // 1/60ths
        PipMethod: PipIncreaseMethod.LPI5,  // Excess increases at RPI capped 5%
        GmpRevMethod: GmpRevaluationMethod.Section148,
        Assumptions: Assumptions);

    public static FutureAssumptions Assumptions => new(
        FuturePost88GmpIncRate: 0.025m,   // CPI ~2.5%
        FuturePipRate: 0.035m,            // RPI ~3.5%
        FutureDiscountRate: 0.025m,
        ProjectionEndYear: 2058);

    /// <summary>
    /// Creates factors with LPI3 and LPI5 increase rates for projection.
    /// Uses assumed rates since the PASA example doesn't provide historical factors.
    /// </summary>
    public static DictionaryFactorProvider CreateFactors()
    {
        var f = new DictionaryFactorProvider();

        // LPI3 factors (post-88 GMP increases)
        for (int year = 2006; year <= 2058; year++)
            f.AddPipFactor(PipIncreaseMethod.LPI3, year, 0.025m);

        // LPI5 factors (excess pension increases)
        for (int year = 2006; year <= 2058; year++)
            f.AddPipFactor(PipIncreaseMethod.LPI5, year, 0.035m);

        return f;
    }

    /// <summary>
    /// Constructs a GmpResult directly from the PASA Example 5 figures.
    /// Bypasses CalculateGmp (which needs raw earnings + S148 factors) and provides
    /// the at-leaving and revalued GMP breakdowns for testing the downstream pipeline.
    ///
    /// Male revalued amounts at age 65 are taken from the PASA Annex table (15/02/2027).
    /// Female revalued amounts at age 60 are estimated using a proportional revaluation factor.
    /// </summary>
    public static GmpResult CreateGmpResult()
    {
        // At-leaving breakdowns (from PASA Table 14)
        var maleAtLeaving = BuildBreakdown(Expected.Pre88GmpPA, Expected.Post88GmpMalePA);
        var femaleAtLeaving = BuildBreakdown(Expected.Pre88GmpPA, Expected.Post88GmpFemalePA);

        // Revalued male at age 65 (from PASA Annex at 15/02/2027)
        var maleRevalued = BuildBreakdown(Expected.RevaluedPre88MalePA, Expected.RevaluedPost88MalePA);

        // Estimated female revalued at age 60.
        // Male revaluation covers 19 tax years (2007-2025), female covers 14 (2007-2020).
        // Scale: factor_F = factor_M^(14/19) ≈ 1.772^0.7368 ≈ 1.525
        var femaleRevalued = BuildBreakdown(Expected.RevaluedPre88FemalePA, Expected.RevaluedPost88FemalePA);

        // Barber service proportion computed from CO dates
        decimal barberServiceProp = BarberWindow.CalculateServiceProportion(
            Member.DateCOStart, Member.DateOfLeaving);

        return new GmpResult(
            WorkingLifeMale: 48,    // 2026 - 1978 (DOB 15 Feb 1962)
            WorkingLifeFemale: 43,  // 2021 - 1978
            TaxYearOfLeaving: 2006,
            MaleAtLeaving: maleAtLeaving,
            FemaleAtLeaving: femaleAtLeaving,
            MaleRevalued: maleRevalued,
            FemaleRevalued: femaleRevalued,
            RevaluationMethod: GmpRevaluationMethod.Section148,
            RevaluationFactorMale: 1.772m,
            RevaluationFactorFemale: 1.525m,
            BarberWindowProportion: Expected.BarberGmpProportion,
            BarberServiceProportion: barberServiceProp,
            TaxYearDetails: new List<TaxYearDetail>().AsReadOnly());
    }

    private static GmpBreakdown BuildBreakdown(decimal pre88Annual, decimal post88Annual)
    {
        decimal totalAnnual = pre88Annual + post88Annual;
        return new GmpBreakdown(
            Pre88Annual: pre88Annual,
            Post88Annual: post88Annual,
            TotalAnnual: totalAnnual,
            Pre88Weekly: Math.Round(pre88Annual / 52m, 2),
            Post88Weekly: Math.Round(post88Annual / 52m, 2),
            TotalWeekly: Math.Round(totalAnnual / 52m, 2));
    }

    /// <summary>
    /// Key figures from the PASA example for validation.
    /// </summary>
    public static class Expected
    {
        // GMP at leaving (from PASA Example 5 Table 14)
        public const decimal TotalGmpMalePA = 1918.21m;
        public const decimal TotalGmpFemalePA = 2020.10m;  // Comparator (female)
        public const decimal Pre88GmpPA = 776.56m;          // Same for both sexes

        // Post-88 GMP (combined 1988-90 and post-1990 periods)
        // 1988-90: £267.78 (same both sexes), Post-1990: Male £873.87, Female £975.76
        public const decimal Post88GmpMalePA = 1141.65m;    // 267.78 + 873.87
        public const decimal Post88GmpFemalePA = 1243.54m;  // 267.78 + 975.76

        // Revalued male GMP at age 65 (from PASA Annex, 15/02/2027 row)
        public const decimal RevaluedPre88MalePA = 1376.10m;
        public const decimal RevaluedPost88MalePA = 2023.07m;
        public const decimal RevaluedTotalMalePA = 3399.17m;

        // Estimated female revalued GMP at age 60 (proportional factor from male data)
        public const decimal RevaluedPre88FemalePA = 1184.25m;
        public const decimal RevaluedPost88FemalePA = 1896.40m;
        public const decimal RevaluedTotalFemalePA = 3080.65m;

        // Barber window: post-1990 GMP / total post-88 GMP (1988-90 is outside Barber)
        // Male: 873.87 / 1141.65 = 0.7654, Female: 975.76 / 1243.54 = 0.7847
        // Using approximate midpoint
        public const decimal BarberGmpProportion = 0.765m;

        // Excess at leaving
        // Male: 21876.54 - 1918.21 = 19958.33
        // Female: 21876.54 - 2020.10 = 19856.44
        public const decimal ExcessMalePA = 19958.33m;
        public const decimal ExcessFemalePA = 19856.44m;

        // The member is disadvantaged (male gets lower post-1990 GMP)
        // C2 uplift at NRD: £229.26 pa (0.7%)
        public const decimal C2UpliftAtNrd = 229.26m;
    }
}
