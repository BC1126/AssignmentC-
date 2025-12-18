using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssignmentC_.Controllers;

public class HomeController : Controller
{
    private readonly DB _db;

    public HomeController(DB db)
    {
        _db = db;
    }
    // In HomeController.cs
    public IActionResult Index()
    {
        var today = DateTime.Today;

        var nowShowing = _db.Movies
                            .Where(m => m.PremierDate <= today)
                            .OrderByDescending(m => m.PremierDate)
                            .Take(8) // Increased to 8 so we have enough for Carousel + Grid
                            .ToList();

        var comingSoon = _db.Movies
                            .Where(m => m.PremierDate > today)
                            .OrderBy(m => m.PremierDate)
                            .Take(4)
                            .ToList();

        var viewModel = new HomeViewModel
        {
            NowShowing = nowShowing,
            ComingSoon = comingSoon
        };

        return View(viewModel);
    }

    public IActionResult movie()
    {
        return View();
    }

    public IActionResult detail()
    {
        return View();
    }

    public IActionResult blog()
    {
        return View();
    }

    public IActionResult blogDetail()
    {
        return View();
    }

    public IActionResult ticket()
    {
        return View();
    }

    public IActionResult about()
    {
        return View();
    }

    public IActionResult contact()
    {
        return View();
    }

    public IActionResult faq()
    {
        return View();
    }

    public IActionResult review()
    {
        return View();
    }

}
