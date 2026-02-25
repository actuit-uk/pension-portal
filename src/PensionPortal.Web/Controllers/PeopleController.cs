using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PensionPortal.Web.Services;

namespace PensionPortal.Web.Controllers;

[Authorize]
public class PeopleController : Controller
{
    private readonly DatabaseService _db;

    public PeopleController(DatabaseService db) => _db = db;

    public IActionResult Index()
    {
        var people = _db.ExecuteSproc("spGetPeople");
        return View(people);
    }

    [HttpGet]
    public IActionResult Add()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Add(string firstName, string surname, string dateOfBirth)
    {
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(surname))
        {
            ViewBag.Error = "First name and surname are required.";
            return View();
        }

        if (!DateTime.TryParse(dateOfBirth, out var dob))
        {
            ViewBag.Error = "Invalid date of birth.";
            return View();
        }

        _db.ExecuteNonQuery("spAddPerson",
            new SqlParameter("@FirstName", firstName),
            new SqlParameter("@Surname", surname),
            new SqlParameter("@DateOfBirth", dob));

        return RedirectToAction("Index");
    }

    [HttpGet]
    public IActionResult Upload()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            ViewBag.Error = "Please select a CSV file.";
            return View();
        }

        int imported = 0;
        var errors = new List<string>();

        using var reader = new StreamReader(file.OpenReadStream());
        string? line;
        int lineNumber = 0;

        while ((line = reader.ReadLine()) != null)
        {
            lineNumber++;
            if (lineNumber == 1) continue; // skip header

            var parts = line.Split(',');
            if (parts.Length < 3)
            {
                errors.Add($"Line {lineNumber}: expected 3 columns, got {parts.Length}");
                continue;
            }

            var firstName = parts[0].Trim();
            var surname = parts[1].Trim();

            if (!DateTime.TryParse(parts[2].Trim(), out var dob))
            {
                errors.Add($"Line {lineNumber}: invalid date '{parts[2].Trim()}'");
                continue;
            }

            _db.ExecuteNonQuery("spAddPerson",
                new SqlParameter("@FirstName", firstName),
                new SqlParameter("@Surname", surname),
                new SqlParameter("@DateOfBirth", dob));
            imported++;
        }

        TempData["Success"] = $"Imported {imported} person(s).";
        if (errors.Count > 0)
            TempData["Warning"] = string.Join(" | ", errors.Take(10));

        return RedirectToAction("Index");
    }
}
