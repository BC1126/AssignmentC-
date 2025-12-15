using AssignmentC_.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Security.Claims;

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

        }

        return View(memberList);
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

        }

        return View(staffList);
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

        }

        return View(adminList);
    }
    // ====================================================================
    // 1. REGISTER ACTIONS
    // ====================================================================

    public IActionResult Register()
    {
        // FIX 1: Explicitly specify the view path since the file is in Views/Home/
        return View("~/Views/Home/Register.cshtml");
    }

    // POST: User/Register (Route is /User/Register)
    [HttpPost]
    [ValidateAntiForgeryToken] // FIX 2: Security - Prevents CSRF Attacks
    public IActionResult Register(RegisterVM vm)
    {
        // 1. Check for email existence 
        if (ModelState.GetValidationState("Email") != ModelValidationState.Invalid &&
            db.Members.Any(u => u.Email == vm.Email))
        {
            ModelState.AddModelError("Email", "Duplicated Email.");
        }

        // --- PHOTO REQUIRED LOGIC ---
        string photoUrl = null;

        // The framework handles the "is null" check via the [Required] attribute on the Photo property.
        // We only need to run custom validation and saving if the Model Binder received a file.
        if (vm.Photo != null)
        {
            // 2a. Validate photo file content/size 
            if (ModelState.GetValidationState("Photo") != ModelValidationState.Invalid)
            {
                var err = hp.ValidatePhoto(vm.Photo);
                if (err != "") ModelState.AddModelError("Photo", err);
            }

            // 2b. If photo validation passed, generate the URL/path
            if (!ModelState.ContainsKey("Photo") || ModelState["Photo"].ValidationState != ModelValidationState.Invalid)
            {
                photoUrl = hp.SavePhoto(vm.Photo, "photos");
            }
        }
        // --- END PHOTO LOGIC ---

        if (ModelState.IsValid)
        {
            try
            {

                // NEW STEP: Generate the Primary Key (UserId)
                string newUserId = hp.GenerateNextUserId("M"); // Assuming 'M' is the prefix for Member/User

                // Create and add new Member
                db.Members.Add(new Member
                {
                    // CRITICAL FIX: Assign the generated Primary Key
                    UserId = newUserId,

                    Email = vm.Email,
                    PasswordHash = hp.HashPassword(vm.Password),
                    Name = vm.Name,
                    PhotoURL = photoUrl,
                    Gender = vm.Gender,
                    Phone = vm.Phone
                });

                db.SaveChanges();

                TempData["Info"] = "Register successfully. Please login.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                // Log ex here for true errors (e.g., unique constraint violations, etc.)
            }
            
        }
        return View("~/Views/Home/Register.cshtml", vm);
    }












    // ====================================================================
    // 2. LOGIN ACTIONS
    // ====================================================================

    // GET: Account/Login
    public IActionResult Login()
    {
        return View("~/Views/Home/Login.cshtml");
    }

    // POST: Account/Login - Handles user authentication
    // UserController.cs

    // ... (existing Register actions and GET Login action) ...

    // POST: User/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Login(LoginVM vm)
    {
        // 1. Basic Model State Validation
        if (!ModelState.IsValid)
        {
            // Re-display the view if client-side validation failed
            return View("~/Views/Home/Login.cshtml", vm);
        }

        // 2. Find the user by email
        var user = db.Users.FirstOrDefault(u => u.Email == vm.Email);

        if (user == null)
        {
            // Check if the user exists in the base Users table (if Members is empty)
            // Note: For a robust system, you might check the base Users table first if Admin/Staff logins are handled here too.
            ModelState.AddModelError("Email", "Invalid login attempt.");
            return View("~/Views/Home/Login.cshtml", vm);
        }

        // 3. Verify the password hash
        bool passwordMatch = hp.VerifyPassword(user.PasswordHash, vm.Password);

        if (!passwordMatch)
        {
            ModelState.AddModelError("Password", "Invalid login attempt.");
            return View("~/Views/Home/Login.cshtml", vm);
        }

        // 4. Authentication and Sign In
        try
        {
            hp.SignIn(user.Email, user.Role, vm.RememberMe); // user.Role should now be "Admin" or "Member"

            TempData["Info"] = $"Welcome back, {user.Name}!";

            // 5. Role-Based Redirect Logic
            if (user.Role == "Admin")
            {
                return RedirectToAction("AdminDashboard", "Admin"); // <-- Redirect to AdminController
            }
            else if (user.Role == "Staff")
            {
                return RedirectToAction("StaffDashboard", "Staff"); // <-- Redirect to StaffController
            }
            else
            {
                return RedirectToAction("Index", "Home"); // Default for Members and Guests
            }
        }
        catch (Exception ex)
        {
            // ... (catch block) ...
        }

        // 5. Fallback: Return the view on failure
        return View("~/Views/Home/Login.cshtml", vm);
    }

    // ====================================================================
    // 3. LOGOUT ACTION
    // ====================================================================

    // GET: User/SignOut
    public IActionResult SignOut()
    {
        // 1. Call your helper method to clear the authentication cookie/session
        hp.SignOut();

        // 2. Add a message confirming the sign-out
        TempData["Info"] = "You have been successfully signed out.";

        // 3. Redirect the user back to the Login page or the Home page
        return RedirectToAction("Login", "User");
    }

    // GET: /User/Profile
    [Authorize] // Ensure only logged-in users can access this page
    public IActionResult Profile()
    {
        // 1. Get the current user's email from the claims identity
        var userEmail = User.Identity.Name;

        if (string.IsNullOrEmpty(userEmail))
        {
            // Should not happen if [Authorize] is used, but good safeguard
            return RedirectToAction("Login");
        }

        // 2. Fetch the user details from the base Users DbSet
        // This will correctly load the Admin, Staff, or Member object via TPH.
        var user = db.Users.FirstOrDefault(u => u.Email == userEmail);

        if (user == null)
        {
            TempData["Error"] = "User profile could not be found.";
            hp.SignOut(); // Log them out if the user record is missing
            return RedirectToAction("Login");
        }

        // 3. Pass the user object to the View
        return View(user);
    }
}
