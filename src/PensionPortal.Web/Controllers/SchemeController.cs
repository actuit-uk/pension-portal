using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PensionPortal.Web.Models;
using PensionPortal.Web.Services;

namespace PensionPortal.Web.Controllers;

[AllowAnonymous]
public class SchemeController : Controller
{
    private readonly PensionDataService _data;

    public SchemeController(PensionDataService data) => _data = data;

    public IActionResult Index()
    {
        var roleKey = HttpContext.Session.GetString("Role");
        var role = RoleConfig.Find(roleKey ?? "");
        if (role == null)
            return RedirectToAction("Index", "Home");

        ViewBag.Role = role;

        var schemes = role.PersonId.HasValue
            ? _data.GetSchemesForPerson(role.PersonId.Value)
            : role.SchemeIds != null
                ? _data.GetSchemes(role.SchemeIds)
                : _data.GetSchemes();

        return View(schemes);
    }

    public IActionResult Detail(string id)
    {
        var roleKey = HttpContext.Session.GetString("Role");
        var role = RoleConfig.Find(roleKey ?? "");
        if (role == null)
            return RedirectToAction("Index", "Home");

        // Verify role has access to this scheme
        if (role.SchemeIds != null && !role.SchemeIds.Contains(id))
            return RedirectToAction("Index");

        ViewBag.Role = role;

        var scheme = _data.GetSchemeDetail(id);
        if (scheme.Rows.Count == 0)
            return NotFound();

        ViewBag.Scheme = scheme.Rows[0];
        ViewBag.Sections = _data.GetSections(id);

        // For member role, filter to their memberships only
        var members = _data.GetMembers(id);
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

        return View(members);
    }
}
