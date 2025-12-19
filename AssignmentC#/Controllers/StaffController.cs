// StaffController.cs

using AssignmentC_;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

// IMPORTANT: This attribute ensures only users with the "Staff" role can access this controller.
[Authorize(Roles = "Staff")]
public class StaffController(DB db, Helper hp) : Controller
{
    // The framework looks for this method: /Staff/Dashboard
    public IActionResult StaffDashboard()
    {
        // This will attempt to find a view at Views/Staff/Dashboard.cshtml
        return View("~/Views/Home/StaffDashboard.cshtml");
    }

    [Authorize(Roles = "Staff")]
    // QR Scan page
    public IActionResult Scan()
    {
        return View(); // Views/Staff/Scan.cshtml
    }

    // Complete claim from scanned order
    [HttpPost]
    public IActionResult CompleteClaim(int orderId)
    {
        var order = db.Orders
            .Include(o => o.OrderLines)
            .FirstOrDefault(o => o.Id == orderId);

        if (order == null)
            return BadRequest("Order not found.");

        order.Claim = true;
        db.SaveChanges();

        return Ok();
    }


    // You can add other Staff-related actions here, e.g.,
    // public IActionResult ProductList() { ... }
}