using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PensionPortal.Web.Services;

namespace PensionPortal.Web.Controllers;

public class AccountController : Controller
{
    private readonly DatabaseService _db;

    public AccountController(DatabaseService db) => _db = db;

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string pin)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(pin))
        {
            ViewBag.Error = "Username and PIN are required.";
            return View();
        }

        if (!int.TryParse(pin, out var pinValue))
        {
            ViewBag.Error = "PIN must be a number.";
            return View();
        }

        var result = _db.ExecuteSproc("spCheckUserPIN",
            new SqlParameter("@UserName", username),
            new SqlParameter("@PIN", pinValue));

        if (result.Rows.Count > 0 && Convert.ToInt32(result.Rows[0]["Authenticated"]) == 1)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, username)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            return RedirectToAction("Index", "Home");
        }

        ViewBag.Error = "Invalid username or PIN.";
        return View();
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}
