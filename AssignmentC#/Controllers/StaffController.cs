// StaffController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// IMPORTANT: This attribute ensures only users with the "Staff" role can access this controller.
[Authorize(Roles = "Staff")]
public class StaffController : Controller
{
    // The framework looks for this method: /Staff/Dashboard
    public IActionResult StaffDashboard()
    {
        // This will attempt to find a view at Views/Staff/Dashboard.cshtml
        return View("~/Views/Home/StaffDashboard.cshtml");
    }

    // You can add other Staff-related actions here, e.g.,
    // public IActionResult ProductList() { ... }
}