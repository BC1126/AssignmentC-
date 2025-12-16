using AssignmentC_.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AssignmentC_.Models;

namespace AssignmentC_.Controllers;

#nullable disable warnings
public class UserController : Controller
{
    private readonly DB db;
    private readonly Helper hp;
    private readonly IWebHostEnvironment en;

    public UserController(DB db, Helper hp, IWebHostEnvironment en)
    {
        this.db = db;
        this.hp = hp;
        this.en = en;
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
        string photoUrl;

        if (vm.Photo != null)
        {
            // Use your existing helper for uploads
            photoUrl = hp.SavePhoto(vm.Photo, "photos");
        }
        else
        {
            // 1. Generate a unique name just like your helper does
            string fileName = Guid.NewGuid().ToString("n") + ".jpg";

            // 2. Define where the master default is and where the new user photo goes
            string sourceFile = Path.Combine(en.WebRootPath, "img", "default.jpg"); // Path to your original
            string destFolder = Path.Combine(en.WebRootPath, "photos");
            string destFile = Path.Combine(destFolder, fileName);

            // 3. Ensure folder exists and copy
            if (!Directory.Exists(destFolder)) Directory.CreateDirectory(destFolder);
            System.IO.File.Copy(sourceFile, destFile);

            photoUrl = $"/photos/{fileName}";
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
    public IActionResult Login(string returnUrl = null)
    {
        if (!string.IsNullOrEmpty(returnUrl))
        {
            TempData["Info"] = "Please Login First";
        }

        ViewBag.ReturnUrl = returnUrl;
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

    // GET: Account/AccessDenied
    public IActionResult AccessDenied(string? returnURL)
    {
        return View();
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

    // UserController.cs

    // GET: /User/EditProfile
    [Authorize]
    public async Task<IActionResult> EditProfile()
    {
        var userEmail = User.Identity?.Name;

        // Check 1: Ensure user is logged in (should be handled by [Authorize], but good safety)
        if (string.IsNullOrEmpty(userEmail))
        {
            // If somehow the identity is missing, redirect to login
            return RedirectToAction("Login", "Account");
        }

        // 1. Fetch the User record based on logged-in email
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

        // Check 2: Handle case where User record doesn't exist
        if (user == null)
        {
            TempData["Error"] = "Error: Your User record was not found in the system. Contact IT.";
            // Log out the user as their authentication cookie is invalid
            // await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); 
            return RedirectToAction("Login", "Account");
        }

        // 2. Fetch the associated Member record using the UserId
        // This is the CRITICAL point of failure for Admin accounts
        var member = await db.Members.FirstOrDefaultAsync(m => m.UserId == user.UserId);

        // Check 3: Handle case where Member record doesn't exist (e.g., Admin was created manually)
        if (member == null)
        {
            // If the Member record is missing, you must redirect to an action that can create it, 
            // or show an error and prompt them to contact support.
            TempData["Error"] = "Error: Your profile data is incomplete. Member record missing.";
            return RedirectToAction("Profile", "User"); // Redirect to profile view or dashboard
        }

        // 3. Map entities to ViewModel (NOW SAFE because user and member are not null)
        var vm = new EditProfileVM
        {
            Id = user.UserId,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,

            // Assuming Phone and Gender are on the Member entity
            Phone = member.Phone,
            Gender = member.Gender,

            CurrentPhotoUrl = member.PhotoURL
        };

        return View(vm);
    }

    // POST: /User/EditProfile
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(EditProfileVM vm)
    {
        // 1. Email Uniqueness Check (Your existing logic)
        var existingUser = await db.Users.FirstOrDefaultAsync(u =>
            u.Email == vm.Email && u.UserId != vm.Id);

        if (existingUser != null)
        {
            ModelState.AddModelError("Email", "This email is already registered by another user.");
        }

        // IMPORTANT: Check if the user is attempting to save the default photo URL back
        if (vm.NewPhoto == null && string.IsNullOrEmpty(vm.CurrentPhotoUrl))
        {
            // If they cleared the photo and didn't upload a new one, force the default.jpg path
            vm.CurrentPhotoUrl = "/img/default.jpg";
        }

        // 2. Validate the ViewModel
        if (ModelState.IsValid)
        {
            var userToUpdate = await db.Users.FirstOrDefaultAsync(u => u.UserId == vm.Id);
            // CRITICAL: Fetch the Member record
            var memberToUpdate = await db.Members.FirstOrDefaultAsync(m => m.UserId == vm.Id);

            if (userToUpdate == null || memberToUpdate == null)
            {
                TempData["Error"] = "User profile record could not be found for update.";
                // hp.SignOut(); 
                return RedirectToAction("Profile");
            }

            // --- PHOTO UPLOAD AND DELETE LOGIC ---
            if (vm.NewPhoto != null)
            {
                // a. Delete old photo (if not the default)
                if (memberToUpdate.PhotoURL != "/img/default.jpg" && !string.IsNullOrEmpty(memberToUpdate.PhotoURL))
                {
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", memberToUpdate.PhotoURL.TrimStart('~', '/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                // b. Save new photo
                // Generate a unique file name using UserId and a tick/timestamp
                var fileName = $"{userToUpdate.UserId}_{DateTime.Now.Ticks}_{Path.GetExtension(vm.NewPhoto.FileName)}";
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await vm.NewPhoto.CopyToAsync(stream);
                }

                // c. Update PhotoUrl in Member table
                memberToUpdate.PhotoURL = $"/img/{fileName}";
            }
            // --- END PHOTO UPLOAD/DELETE LOGIC ---


            // 3. Update properties on User and Member entities
            userToUpdate.Name = vm.Name;
            userToUpdate.Email = vm.Email;

            // Assuming Phone and Gender are on the Member table (adjust if they are on User)
            memberToUpdate.Phone = vm.Phone;
            memberToUpdate.Gender = vm.Gender;

            // 4. Save changes (will save changes to both User and Member records tracked by the context)
            await db.SaveChangesAsync();

            // 5. Re-sign in the user if the Email or Name changed (if required)
            // hp.SignIn(userToUpdate.Email, userToUpdate.Role, true); // Uncomment if Sign In logic is handled here

            TempData["Info"] = "Your profile has been updated successfully!";
            return RedirectToAction("Profile");
        }

        // If validation failed, return the ViewModel to the view with errors
        // Note: You must ensure vm.CurrentPhotoUrl is populated correctly here if validation fails.
        // Since CurrentPhotoUrl is a hidden field in the view, it should persist on failure.
        return View(vm);
    }

    [Authorize]
    public IActionResult ChangePassword()
    {
        return View();
    }

    // POST: Account/UpdatePassword
    // UserController.cs (assuming this is where the action resides)

    // POST: /User/ChangePassword
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken] // Recommended best practice for security
    public async Task<IActionResult> ChangePassword(ChangePasswordVM vm)
    {
        // 1. Retrieve the user's email from the authentication claim
        var userEmail = User.Identity?.Name;

        // 2. CRITICAL FIX: Use FirstOrDefaultAsync to query by Email
        var u = await db.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

        // If the user is null, redirect (this handles the primary key mismatch you had)
        if (u == null)
        {
            TempData["Error"] = "User record could not be found. Please log in again.";
            // Assuming hp.SignOut() and redirection to login is necessary
            // hp.SignOut();
            return RedirectToAction("Login", "Account");
        }

        // 3. Verify Current Password
        // Check if ModelState is already invalid before doing further checks (for performance)
        if (ModelState.IsValid)
        {
            // 4. Verification Check
            if (!hp.VerifyPassword(u.PasswordHash, vm.Current))
            {
                ModelState.AddModelError("Current", "Current Password not matched.");
                // If verification fails, we must return the view immediately
                return View(vm);
            }

            // 5. Update and Save
            u.PasswordHash = hp.HashPassword(vm.New);
            await db.SaveChangesAsync(); // Use the asynchronous SaveChanges

            TempData["Info"] = "Password updated.";
            // Redirect to the Profile page or another suitable location
            return RedirectToAction("Profile");
        }

        // If ModelState was invalid from the start (e.g., New/Confirm mismatch)
        return View(vm);
    }
}
