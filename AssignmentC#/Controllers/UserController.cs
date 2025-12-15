using Microsoft.AspNetCore.Mvc;

namespace AssignmentC_.Controllers;

public class UserController : Controller
{
    private readonly DB db;
    private readonly Helper hp;

    public UserController(DB db, Helper hp)
    {
        this.db = db;
        this.hp = hp;
    }

    // In AdminController.cs
    public IActionResult MemberList()
    {
        //[Authorize(Role = "Admin")]
        // Declare the list as the type the view expects: IEnumerable<Member>
        IEnumerable<Member> memberList = new List<Member>();

        try
        {
            // 1. Fetch only Member objects (assuming your DB context supports this)
            // AND/OR
            // 2. Explicitly cast the entire collection to the required type.
            // The "as Member" cast will only succeed if the object is actually a Member
            memberList = db.Users.OfType<Member>().ToList();
        }
        catch (Exception ex)
        {
            // Log the exception
        }

        return View(memberList); // Now passing IEnumerable<Member>
    }

    public IActionResult StaffList()
    {
        //[Authorize(Role = "Admin")]
        // Declare the list as the type the view expects: IEnumerable<Member>
        IEnumerable<Staff> staffList = new List<Staff>();

        try
        {
            // 1. Fetch only Member objects (assuming your DB context supports this)
            // AND/OR
            // 2. Explicitly cast the entire collection to the required type.
            // The "as Member" cast will only succeed if the object is actually a Member
            staffList = db.Users.OfType<Staff>().ToList();
        }
        catch (Exception ex)
        {
            // Log the exception
        }

        return View(staffList); // Now passing IEnumerable<Member>
    }

    public IActionResult AdminList()
    {
        //[Authorize(Role = "Admin")]
        // Declare the list as the type the view expects: IEnumerable<Member>
        IEnumerable<Admin> adminList = new List<Admin>();

        try
        {
            // 1. Fetch only Member objects (assuming your DB context supports this)
            // AND/OR
            // 2. Explicitly cast the entire collection to the required type.
            // The "as Member" cast will only succeed if the object is actually a Member
            adminList = db.Users.OfType<Admin>().ToList();
        }
        catch (Exception ex)
        {
            // Log the exception
        }

        return View(adminList); // Now passing IEnumerable<Member>
    }

}
