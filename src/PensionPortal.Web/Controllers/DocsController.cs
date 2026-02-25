using Markdig;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PensionPortal.Web.Controllers;

[Authorize]
public class DocsController : Controller
{
    private readonly IWebHostEnvironment _env;

    public DocsController(IWebHostEnvironment env) => _env = env;

    public IActionResult Article(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return RedirectToAction("Article", new { name = "about" });

        var path = Path.Combine(_env.ContentRootPath, "Docs", $"{name}.md");
        if (!System.IO.File.Exists(path))
            return NotFound();

        var markdown = System.IO.File.ReadAllText(path);
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        ViewBag.HtmlContent = Markdown.ToHtml(markdown, pipeline);
        ViewBag.ArticleName = name;
        return View();
    }
}
