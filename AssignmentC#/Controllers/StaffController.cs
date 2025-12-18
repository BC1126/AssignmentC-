using AssignmentC_;
using AssignmentC_.Models; // Added to ensure MemberDetailsVM is recognized
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "Staff")]
// Updated Primary Constructor to assign the field
public class StaffController(DB db, Helper hp) : Controller
{

    public IActionResult StaffDashboard()
    {
        return View("~/Views/Home/StaffDashboard.cshtml");
    }

    public IActionResult Scan()
    {
        return View();
    }

    [HttpPost]
    public IActionResult CompleteClaim(int orderId)
    {
        // Using 'db' as per your original logic
        var order = db.Orders
            .Include(o => o.OrderLines)
            .FirstOrDefault(o => o.Id == orderId);

        if (order == null)
            return BadRequest("Order not found.");

        order.Claim = true;
        db.SaveChanges();

        return Ok();
    }

    public IActionResult MemberDetails(string id)
    {
        var member = db.Users
            .Where(u => u.UserId == id) 
            .Select(u => new MemberDetailsVM
            {
                Name = u.Name,
                Email = u.Email,
                Phone = u.Phone,
                Gender = u.Gender,

                PhotoPath = (u as Member).PhotoURL,

                Role = u.Role
            })
            .FirstOrDefault();

        if (member == null || member.Role != "Member")
        {
            return NotFound();
        }

        return View("~/Views/User/MemberDetails.cshtml", member);
    }
}