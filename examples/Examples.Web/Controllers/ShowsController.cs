using Microsoft.AspNetCore.Mvc;

namespace Examples.Web.Controllers;

// The MVC controller from "ASP.NET Core".  Its view lives at
// Views/Shows/Index.cshtml, by convention.
public class ShowsController : Controller
{
    private readonly IShowRepo _repo;

    public ShowsController(IShowRepo repo)
    {
        _repo = repo;
    }

    // GET /Shows
    public async Task<IActionResult> Index()
    {
        var shows = await _repo.GetShowsAsync();
        return View(shows);
    }

    // POST /Shows/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TvShow show)
    {
        if (!ModelState.IsValid)
        {
            return View(show);
        }

        await _repo.InsertAsync(show);
        return RedirectToAction(nameof(Index));
    }
}
