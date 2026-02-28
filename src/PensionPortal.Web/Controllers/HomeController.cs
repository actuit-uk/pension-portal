using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PensionPortal.Web.Models;

namespace PensionPortal.Web.Controllers;

[AllowAnonymous]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View(RoleConfig.All);
    }

    [HttpGet]
    public IActionResult SelectRole(string role)
    {
        var config = RoleConfig.Find(role);
        if (config == null)
            return RedirectToAction("Index");

        HttpContext.Session.SetString("Role", role);
        return RedirectToAction("Index", "Scheme");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [Route("/Home/StatusCode")]
    public IActionResult HttpError(int code)
    {
        ViewBag.StatusCode = code;
        return View("StatusCode");
    }
}
