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
    public IActionResult Index()
    {
        var movies = _db.Movies
                             .OrderByDescending(m => m.PremierDate)
                             .Take(3)  
                             .ToList();

        return View(movies);
    }

    public IActionResult movie()
    {
        return View();
    }

    public IActionResult detail()
    {
        return View();
    }

    public IActionResult login()
    {
        return View();
    }

    public IActionResult register()
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
