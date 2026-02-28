using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PensionPortal.Web.Models;
using PensionPortal.Web.Services;

namespace PensionPortal.Web.Controllers;

[AllowAnonymous]
public class MemberController : Controller
{
    private readonly PensionDataService _data;

    public MemberController(PensionDataService data) => _data = data;

    public IActionResult Detail(int id)
    {
        var roleKey = HttpContext.Session.GetString("Role");
        var role = RoleConfig.Find(roleKey ?? "");
        if (role == null)
            return RedirectToAction("Index", "Home");

        ViewBag.Role = role;

        var member = _data.GetMemberDetail(id);
        if (member.Rows.Count == 0)
            return NotFound();

        var row = member.Rows[0];

        // For member role, verify they can only see their own record
        if (role.PersonId.HasValue && Convert.ToInt32(row["person_id"]) != role.PersonId.Value)
            return RedirectToAction("Index", "Scheme");

        ViewBag.Member = row;
        ViewBag.GmpRecords = _data.GetGmpRecords(id);

        return View();
    }
}
