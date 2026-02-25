using System.Data;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PensionPortal.CalcLib;
using PensionPortal.Web.Services;

namespace PensionPortal.Web.Controllers;

[Authorize]
public class CalculationController : Controller
{
    private readonly DatabaseService _db;

    public CalculationController(DatabaseService db) => _db = db;

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public IActionResult RunAll(string calculationDate)
    {
        if (!DateTime.TryParse(calculationDate, out var calcDate))
        {
            TempData["Error"] = "Invalid calculation date.";
            return RedirectToAction("Index");
        }

        var people = _db.ExecuteSproc("spGetPeople");
        var factors = _db.ExecuteSproc("spGetFactors");

        // Build a lookup dictionary from the factor table
        var factorLookup = new Dictionary<int, double>();
        foreach (DataRow row in factors.Rows)
        {
            factorLookup[(int)row["AgeInMonths"]] = (double)row["FactorValue"];
        }

        int count = 0;
        foreach (DataRow person in people.Rows)
        {
            var dob = (DateTime)person["DateOfBirth"];
            var personId = (int)person["PersonID"];

            // Call the C# calculation library
            int ageInMonths = AgeCalculator.CompleteMonths(dob, calcDate);

            // Look up factor — find the nearest lower key
            double? factorValue = null;
            var nearestKey = factorLookup.Keys
                .Where(k => k <= ageInMonths)
                .OrderByDescending(k => k)
                .FirstOrDefault();
            if (factorLookup.ContainsKey(nearestKey))
                factorValue = factorLookup[nearestKey];

            // Save result
            var parameters = new List<SqlParameter>
            {
                new("@PersonID", personId),
                new("@CalculationDate", calcDate),
                new("@AgeInMonths", ageInMonths)
            };
            if (factorValue.HasValue)
                parameters.Add(new SqlParameter("@FactorValue", factorValue.Value));
            else
                parameters.Add(new SqlParameter("@FactorValue", DBNull.Value));

            _db.ExecuteNonQuery("spSaveResult", parameters.ToArray());
            count++;
        }

        TempData["Success"] = $"Calculated {count} record(s) as at {calcDate:dd MMM yyyy}.";
        return RedirectToAction("Results");
    }

    public IActionResult Results()
    {
        var results = _db.ExecuteSproc("spGetResults");
        return View(results);
    }

    public IActionResult ExportCsv()
    {
        var results = _db.ExecuteSproc("spGetResults");
        var sb = new StringBuilder();
        sb.AppendLine("FirstName,Surname,DateOfBirth,CalculationDate,AgeInMonths,FactorValue,CalcTimeStamp");

        foreach (DataRow row in results.Rows)
        {
            sb.AppendLine(string.Join(",",
                row["FirstName"],
                row["Surname"],
                ((DateTime)row["DateOfBirth"]).ToString("yyyy-MM-dd"),
                ((DateTime)row["CalculationDate"]).ToString("yyyy-MM-dd"),
                row["AgeInMonths"],
                row["FactorValue"] == DBNull.Value ? "" : row["FactorValue"],
                ((DateTime)row["CalcTimeStamp"]).ToString("yyyy-MM-dd HH:mm:ss")));
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"results-{timestamp}.csv");
    }
}
