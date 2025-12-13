using Microsoft.AspNetCore.Mvc;

namespace AssignmentC_.Controllers;

public class TicketController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Checkout()
    {
        return View();
    }

    public IActionResult Purchase()
    {
        return View();
    }
}
