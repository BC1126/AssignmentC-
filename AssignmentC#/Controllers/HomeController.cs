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
        // --- 1. Check for Admin/Staff Redirect FIRST ---
        if (User.Identity.IsAuthenticated)
        {
            if (User.IsInRole("Admin"))
            {
                // Redirect to Admin Controller, AdminDashboard Action
                return RedirectToAction("AdminDashboard", "Admin");
            }

            if (User.IsInRole("Staff"))
            {
                // Redirect to Staff Controller, StaffDashboard Action
                return RedirectToAction("StaffDashboard", "Staff");
            }
        }

        // --- 2. If it's a Member or Guest, load the movies ---
        var today = DateTime.Today;

        var nowShowing = _db.Movies
                            .Where(m => m.PremierDate <= today)
                            .OrderByDescending(m => m.PremierDate)
                            .Take(8)
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
