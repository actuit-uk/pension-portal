using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PensionPortal.CalcLib;
using PensionPortal.Web.Models;
using PensionPortal.Web.Services;

namespace PensionPortal.Web.Controllers;

[AllowAnonymous]
public class GmpEqualisationController : Controller
{
    private readonly PensionDataService _data;

    public GmpEqualisationController(PensionDataService data) => _data = data;

    public IActionResult Index()
    {
        var roleKey = HttpContext.Session.GetString("Role");
        var role = RoleConfig.Find(roleKey ?? "");
        if (role == null)
            return RedirectToAction("Index", "Home");

        ViewBag.Role = role;

        var members = _data.GetMembersWithGmp();

        // Apply role-based filtering
        if (role.PersonId.HasValue)
        {
            var filtered = members.Clone();
            foreach (System.Data.DataRow row in members.Rows)
            {
                if (Convert.ToInt32(row["person_id"]) == role.PersonId.Value)
                    filtered.ImportRow(row);
            }
            members = filtered;
        }
        else if (role.SchemeIds != null)
        {
            var filtered = members.Clone();
            foreach (System.Data.DataRow row in members.Rows)
            {
                if (role.SchemeIds.Contains(row["scheme_id"].ToString()!))
                    filtered.ImportRow(row);
            }
            members = filtered;
        }

        return View(members);
    }

    public IActionResult Run(int id)
    {
        var roleKey = HttpContext.Session.GetString("Role");
        var role = RoleConfig.Find(roleKey ?? "");
        if (role == null)
            return RedirectToAction("Index", "Home");

        ViewBag.Role = role;

        // Load member detail (includes section rules)
        var memberDetail = _data.GetMemberDetail(id);
        if (memberDetail.Rows.Count == 0)
            return NotFound();

        var m = memberDetail.Rows[0];

        // Role access check
        if (role.PersonId.HasValue && Convert.ToInt32(m["person_id"]) != role.PersonId.Value)
            return RedirectToAction("Index");

        if (role.SchemeIds != null && !role.SchemeIds.Contains(m["scheme_id"].ToString()!))
            return RedirectToAction("Index");

        var gmpRecords = _data.GetGmpRecords(id);
        if (gmpRecords.Rows.Count == 0)
            return RedirectToAction("Index");

        // Bridge DB data → CalcLib inputs
        var sex = m["gender"]?.ToString()?.Trim().StartsWith("F", StringComparison.OrdinalIgnoreCase) == true
            ? Sex.Female : Sex.Male;
        var dob = Convert.ToDateTime(m["date_of_birth"]);
        var dateJoined = Convert.ToDateTime(m["date_of_joining"]);
        var dateLeft = m["date_of_leaving"] == DBNull.Value
            ? dateJoined.AddYears(20)
            : Convert.ToDateTime(m["date_of_leaving"]);

        // CO dates from GMP records
        var coStart = Convert.ToDateTime(gmpRecords.Rows[0]["accrual_start_date"]);
        var coEnd = Convert.ToDateTime(gmpRecords.Rows[gmpRecords.Rows.Count - 1]["accrual_end_date"]);

        // Check for transferred-in GMP
        bool hasTransfer = false;
        foreach (System.Data.DataRow g in gmpRecords.Rows)
        {
            var source = g["gmp_source"]?.ToString() ?? "";
            if (source.Contains("transfer", StringComparison.OrdinalIgnoreCase))
            {
                hasTransfer = true;
                break;
            }
        }

        // Synthesise earnings via EarningsEstimator
        var factors = new DictionaryFactorProvider();
        var memberData = EarningsEstimator.Estimate(
            sex, dob, dateJoined, dateLeft,
            salary1990: 15000m, factors) with { HasTransferredInGmp = hasTransfer };

        // Override CO dates from GMP records (more accurate than estimator defaults)
        memberData = memberData with { DateCOStart = coStart, DateCOEnd = coEnd };

        // Build SchemeConfig from section rules
        int maleNra = m["male_nra"] == DBNull.Value ? 65 : Convert.ToInt32(m["male_nra"]);
        int femaleNra = m["female_nra"] == DBNull.Value ? 60 : Convert.ToInt32(m["female_nra"]);
        int accrualDenom = m["accrual_denominator"] == DBNull.Value ? 60 : Convert.ToInt32(m["accrual_denominator"]);
        bool antiFranking = m["anti_franking_applies"] != DBNull.Value && Convert.ToBoolean(m["anti_franking_applies"]);

        var pipMethod = ParsePipMethod(m["pip_method"]?.ToString());
        var increaseMethod = ParseIncreaseMethod(m["increase_method"]?.ToString());
        var revMethod = ParseRevaluationMethod(
            m["default_revaluation"]?.ToString(),
            gmpRecords.Rows[0]["revaluation_basis"]?.ToString());

        var assumptions = new FutureAssumptions(
            FuturePost88GmpIncRate: 0.025m,
            FuturePipRate: 0.025m,
            FutureDiscountRate: -0.017m,
            ProjectionEndYear: 2060);

        var schemeConfig = new SchemeConfig(
            PreEqNraMale: maleNra,
            PreEqNraFemale: femaleNra,
            PostEqNra: maleNra,
            DateOfEqualisation: new DateTime(1990, 5, 17),
            AccrualRateDenominator: accrualDenom,
            PipMethod: pipMethod,
            GmpRevMethod: revMethod,
            Assumptions: assumptions,
            IncreaseMethod: increaseMethod,
            AntiFrankingApplies: antiFranking);

        // Run the calculation
        var result = GmpCalculator.Calculate(memberData, schemeConfig, factors);

        ViewBag.Member = m;
        ViewBag.MemberData = memberData;
        ViewBag.SchemeConfig = schemeConfig;
        return View("Result", result);
    }

    private static GmpRevaluationMethod ParseRevaluationMethod(string? sectionDefault, string? gmpBasis)
    {
        var value = (gmpBasis ?? sectionDefault ?? "").Trim();
        if (value.Contains("148", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("S148", StringComparison.OrdinalIgnoreCase))
            return GmpRevaluationMethod.Section148;
        if (value.Contains("Limited", StringComparison.OrdinalIgnoreCase))
            return GmpRevaluationMethod.LimitedRate;
        return GmpRevaluationMethod.FixedRate;
    }

    private static PipIncreaseMethod ParsePipMethod(string? value)
    {
        value = (value ?? "").Trim();
        if (value.Contains("LPI3", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("LPI 3", StringComparison.OrdinalIgnoreCase))
            return PipIncreaseMethod.LPI3;
        if (value.Contains("Public", StringComparison.OrdinalIgnoreCase))
            return PipIncreaseMethod.PublicSector;
        return PipIncreaseMethod.LPI5;
    }

    private static PensionIncreaseMethod ParseIncreaseMethod(string? value)
    {
        value = (value ?? "").Trim();
        if (value.Contains("Overall", StringComparison.OrdinalIgnoreCase))
            return PensionIncreaseMethod.Overall;
        return PensionIncreaseMethod.Separate;
    }
}
