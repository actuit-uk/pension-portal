using PensionPortal.CalcLib.Internal;

namespace PensionPortal.CalcLib;

/// <summary>
/// Main GMP calculation engine. All methods are pure functions with no side effects.
/// </summary>
public static class GmpCalculator
{
    private const int MaleGmpAge = 65;
    private const int FemaleGmpAge = 60;

    /// <summary>
    /// Runs the full GMP equalisation pipeline: GMP calculation, cash flow projection,
    /// and compensation calculation, returning a complete EqualisationResult.
    /// </summary>
    /// <param name="member">Member data including earnings history.</param>
    /// <param name="revMethod">The GMP revaluation method to use.</param>
    /// <param name="factors">Factor provider for all lookups.</param>
    /// <param name="assumptions">Future projection assumptions.</param>
    public static EqualisationResult Calculate(
        MemberData member,
        GmpRevaluationMethod revMethod,
        IFactorProvider factors,
        FutureAssumptions assumptions)
    {
        var gmp = CalculateGmp(member, revMethod, factors);
        var cashFlow = CashFlowBuilder.Build(gmp, member, factors, assumptions);
        var (compensation, total) = CompensationCalculator.Calculate(
            cashFlow, member.Sex, factors, assumptions);

        return new EqualisationResult(
            Gmp: gmp,
            CashFlow: cashFlow,
            Compensation: compensation,
            TotalCompensation: total);
    }

    /// <summary>
    /// Calculates GMP at date of leaving and revalued to GMP payable age.
    /// </summary>
    /// <param name="member">Member data including earnings history.</param>
    /// <param name="revMethod">The GMP revaluation method to use.</param>
    /// <param name="factors">Factor provider for all lookups.</param>
    public static GmpResult CalculateGmp(
        MemberData member,
        GmpRevaluationMethod revMethod,
        IFactorProvider factors)
    {
        // Working life
        int workingLifeM = WorkingLife.Calculate(member.DateOfBirth, MaleGmpAge);
        int workingLifeF = WorkingLife.Calculate(member.DateOfBirth, FemaleGmpAge);

        // Tax year of leaving (for S148 factor lookup)
        int taxYearOfLeaving = TaxYearHelper.TaxYearFromDate(member.DateOfLeaving);

        // Accumulate GMP per tax year, collecting audit details
        decimal pre88TotalM = 0m, pre88TotalF = 0m;
        decimal totalM = 0m, totalF = 0m;
        var details = new List<TaxYearDetail>();

        foreach (var (taxYear, earnings) in member.Earnings.OrderBy(e => e.Key))
        {
            var detail = TaxYearGmp.Calculate(
                earnings, taxYear, taxYearOfLeaving,
                workingLifeM, workingLifeF, factors);

            details.Add(detail);

            totalM += detail.RawGmpMale;
            totalF += detail.RawGmpFemale;

            if (TaxYearGmp.IsPre88(taxYear))
            {
                pre88TotalM += detail.RawGmpMale;
                pre88TotalF += detail.RawGmpFemale;
            }
        }

        // Round totals
        pre88TotalM = Math.Round(pre88TotalM, 2);
        pre88TotalF = Math.Round(pre88TotalF, 2);
        totalM = Math.Round(totalM, 2);
        totalF = Math.Round(totalF, 2);

        // Build at-leaving breakdown
        var maleAtLeaving = BuildBreakdown(pre88TotalM, totalM);
        var femaleAtLeaving = BuildBreakdown(pre88TotalF, totalF);

        // Revalue to GMP payable age
        int taxYearBeforeGmpAgeM = TaxYearBeforeGmpAge(member.DateOfBirth, MaleGmpAge);
        int taxYearBeforeGmpAgeF = TaxYearBeforeGmpAge(member.DateOfBirth, FemaleGmpAge);

        decimal revFactorM = Revaluation.CalculateFactor(
            revMethod, taxYearOfLeaving, taxYearBeforeGmpAgeM, factors);
        decimal revFactorF = Revaluation.CalculateFactor(
            revMethod, taxYearOfLeaving, taxYearBeforeGmpAgeF, factors);

        // Apply revaluation to weekly amounts, then derive annual
        decimal revaluedTotalMpw = Math.Round(maleAtLeaving.TotalWeekly * revFactorM, 2);
        decimal revaluedPre88Mpw = Math.Round(maleAtLeaving.Pre88Weekly * revFactorM, 2);
        decimal revaluedTotalFpw = Math.Round(femaleAtLeaving.TotalWeekly * revFactorF, 2);
        decimal revaluedPre88Fpw = Math.Round(femaleAtLeaving.Pre88Weekly * revFactorF, 2);

        var maleRevalued = new GmpBreakdown(
            Pre88Annual: revaluedPre88Mpw * 52m,
            Post88Annual: (revaluedTotalMpw - revaluedPre88Mpw) * 52m,
            TotalAnnual: revaluedTotalMpw * 52m,
            Pre88Weekly: revaluedPre88Mpw,
            Post88Weekly: revaluedTotalMpw - revaluedPre88Mpw,
            TotalWeekly: revaluedTotalMpw);

        var femaleRevalued = new GmpBreakdown(
            Pre88Annual: revaluedPre88Fpw * 52m,
            Post88Annual: (revaluedTotalFpw - revaluedPre88Fpw) * 52m,
            TotalAnnual: revaluedTotalFpw * 52m,
            Pre88Weekly: revaluedPre88Fpw,
            Post88Weekly: revaluedTotalFpw - revaluedPre88Fpw,
            TotalWeekly: revaluedTotalFpw);

        return new GmpResult(
            WorkingLifeMale: workingLifeM,
            WorkingLifeFemale: workingLifeF,
            TaxYearOfLeaving: taxYearOfLeaving,
            MaleAtLeaving: maleAtLeaving,
            FemaleAtLeaving: femaleAtLeaving,
            MaleRevalued: maleRevalued,
            FemaleRevalued: femaleRevalued,
            RevaluationMethod: revMethod,
            RevaluationFactorMale: Math.Round(revFactorM, 3),
            RevaluationFactorFemale: Math.Round(revFactorF, 3),
            TaxYearDetails: details.AsReadOnly());
    }

    /// <summary>
    /// Builds a GmpBreakdown from annual pre-88 and total amounts.
    /// Derives weekly amounts by dividing by 52 and rounding.
    /// </summary>
    private static GmpBreakdown BuildBreakdown(decimal pre88Annual, decimal totalAnnual)
    {
        decimal post88Annual = totalAnnual - pre88Annual;
        decimal pre88Weekly = Math.Round(pre88Annual / 52m, 2);
        decimal totalWeekly = Math.Round(totalAnnual / 52m, 2);
        decimal post88Weekly = Math.Round(post88Annual / 52m, 2);

        return new GmpBreakdown(
            Pre88Annual: pre88Annual,
            Post88Annual: post88Annual,
            TotalAnnual: totalAnnual,
            Pre88Weekly: pre88Weekly,
            Post88Weekly: post88Weekly,
            TotalWeekly: totalWeekly);
    }

    /// <summary>
    /// Returns the last complete tax year before GMP payable age is reached.
    /// </summary>
    private static int TaxYearBeforeGmpAge(DateTime dateOfBirth, int gmpAge)
    {
        int gmpAgeYear = dateOfBirth.Year + gmpAge;
        var gmpAgeDate = new DateTime(gmpAgeYear, dateOfBirth.Month, dateOfBirth.Day);
        // Tax year containing GMP age date, minus 1 gives last complete tax year before
        return TaxYearHelper.TaxYearFromDate(gmpAgeDate) - 1;
    }
}
