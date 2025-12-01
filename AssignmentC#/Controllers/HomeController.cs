using Microsoft.AspNetCore.Mvc;

namespace AssignmentC_.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
