using AssignmentC_.Models;
using AssignmentC_.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.IO;
using System.Security.Claims;

namespace AssignmentC_.Controllers;

#nullable disable warnings
public class UserController : Controller
{
    private readonly DB db;
    private readonly Helper hp;
    private readonly IWebHostEnvironment en;
    private readonly IDataProtector _protector;

    public UserController(DB db, Helper hp, IWebHostEnvironment en, IDataProtectionProvider provider)
    {
        this.db = db;
        this.hp = hp;
        this.en = en;
        _protector = provider.CreateProtector("PasswordResetPurpose");
        _protector = provider.CreateProtector("CaptchaProtector");
    }

    public async Task<IActionResult> MemberList(string sortOrder, string searchString, int? pageNumber)
    {
        // 1. Setup Sort Parameters for the UI (Toggle Logic)
        ViewData["CurrentSort"] = sortOrder;
        ViewData["NameSort"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
        ViewData["EmailSort"] = sortOrder == "Email" ? "email_desc" : "Email";
        ViewData["IdSort"] = sortOrder == "Id" ? "id_desc" : "Id";

        // 2. Start Query - Filter by Member type specifically
        var memberQuery = db.Users.OfType<Member>().AsQueryable();

        // 3. Search Logic: Filters by Name or Email
        if (!string.IsNullOrEmpty(searchString))
        {
            memberQuery = memberQuery.Where(m => m.Name.Contains(searchString) || m.Email.Contains(searchString));
        }

        // 4. Sort Logic
        memberQuery = sortOrder switch
        {
            "name_desc" => memberQuery.OrderByDescending(m => m.Name),
            "Email" => memberQuery.OrderBy(m => m.Email),
            "email_desc" => memberQuery.OrderByDescending(m => m.Email),
            "Id" => memberQuery.OrderBy(m => m.UserId),
            "id_desc" => memberQuery.OrderByDescending(m => m.UserId),
            _ => memberQuery.OrderBy(m => m.Name), // Default Sort
        };

        // 5. Paging Logic
        int pageSize = 10;
        int pageIndex = pageNumber ?? 1;

        // Execute count and fetch page items asynchronously
        var count = await memberQuery.CountAsync();
        var items = await memberQuery.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();

        // 6. Wrap results in the PaginatedList container
        var model = new PaginatedList<Member>(items, count, pageIndex, pageSize)
        {
            SearchString = searchString,
            SortOrder = sortOrder
        };

        // 7. AJAX Check: Return only the table rows if requested via JavaScript
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView("_MemberList", model);
        }

        // Standard request returns the full page
        return View(model);
    }

    public async Task<IActionResult> StaffList(string sortOrder, string searchString, int? pageNumber)
    {
        // 1. Setup Sort Parameters for the UI
        ViewData["CurrentSort"] = sortOrder;
        ViewData["NameSort"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
        ViewData["EmailSort"] = sortOrder == "Email" ? "email_desc" : "Email";
        ViewData["IdSort"] = sortOrder == "Id" ? "id_desc" : "Id";

        // 2. Start Query
        var staffQuery = db.Staffs.AsQueryable(); // Use your actual DB context and table

        // 3. Search Logic
        if (!String.IsNullOrEmpty(searchString))
        {
            staffQuery = staffQuery.Where(s => s.Name.Contains(searchString) || s.Email.Contains(searchString));
        }

        // 4. Sort Logic
        staffQuery = sortOrder switch
        {
            "name_desc" => staffQuery.OrderByDescending(s => s.Name),
            "Email" => staffQuery.OrderBy(s => s.Email),
            "email_desc" => staffQuery.OrderByDescending(s => s.Email),
            "Id" => staffQuery.OrderBy(s => s.UserId),
            "id_desc" => staffQuery.OrderByDescending(s => s.UserId),
            _ => staffQuery.OrderBy(s => s.Name),
        };

        // 5. Paging Logic
        int pageSize = 10; // Number of rows per page
        int pageIndex = pageNumber ?? 1;

        var count = await staffQuery.CountAsync();
        var items = await staffQuery.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();

        // 6. WRAP IN PAGINATED LIST (This fixes the error)
        var model = new PaginatedList<Staff>(items, count, pageIndex, pageSize)
        {
            SearchString = searchString,
            SortOrder = sortOrder
        };

        // 7. AJAX Check
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView("_StaffList", model);
        }

        return View(model);
    }

    public async Task<IActionResult> AdminList(string sortOrder, string searchString, int? pageNumber)
    {
        // 1. Setup Sort Parameters
        ViewData["CurrentSort"] = sortOrder;
        ViewData["NameSort"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
        ViewData["EmailSort"] = sortOrder == "Email" ? "email_desc" : "Email";
        ViewData["IdSort"] = sortOrder == "Id" ? "id_desc" : "Id";

        // 2. Start Query - Use OfType for inheritance
        var adminQuery = db.Users.OfType<Admin>().AsQueryable();

        // 3. Search Logic
        if (!string.IsNullOrEmpty(searchString))
        {
            adminQuery = adminQuery.Where(a => a.Name.Contains(searchString) || a.Email.Contains(searchString));
        }

        // 4. Sort Logic
        adminQuery = sortOrder switch
        {
            "name_desc" => adminQuery.OrderByDescending(a => a.Name),
            "Email" => adminQuery.OrderBy(a => a.Email),
            "email_desc" => adminQuery.OrderByDescending(a => a.Email),
            "Id" => adminQuery.OrderBy(a => a.UserId),
            "id_desc" => adminQuery.OrderByDescending(a => a.UserId),
            _ => adminQuery.OrderBy(a => a.Name),
        };

        // 5. Paging Logic
        int pageSize = 10;
        int pageIndex = pageNumber ?? 1;
        int count = await adminQuery.CountAsync();
        var items = await adminQuery.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();

        var model = new PaginatedList<Admin>(items, count, pageIndex, pageSize)
        {
            SearchString = searchString,
            SortOrder = sortOrder
        };

        // 6. AJAX Check for partial update
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView("_AdminList", model);
        }

        return View(model);
    }

    // ====================================================================
    // 1. REGISTER ACTIONS
    // ====================================================================

    [HttpGet]
    public IActionResult Register()
    {
        string code = hp.GenerateCaptchaCode();

        // 2. Set ViewBag BEFORE returning the view
        ViewBag.CaptchaCode = code;
        ViewBag.EncryptedCaptcha = _protector.Protect(code);

        // 3. Return the view directly
        return View("~/Views/Home/Register.cshtml");
    }

    // POST: User/Register (Route is /User/Register)
    [HttpPost]
    [ValidateAntiForgeryToken] // FIX 2: Security - Prevents CSRF Attacks
    public IActionResult Register(RegisterVM vm, string encryptedCaptcha)
    {
        try
        {
            // Decrypt the answer that was stored in the browser's hidden field
            string actualCode = _protector.Unprotect(encryptedCaptcha);

            if (string.IsNullOrEmpty(vm.CaptchaInput) || vm.CaptchaInput.ToUpper() != actualCode)
            {
                ModelState.AddModelError("CaptchaInput", "Invalid verification code.");
            }
        }
        catch
        {
            ModelState.AddModelError("CaptchaInput", "Verification expired. Please try again.");
        }
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

        string newCode = hp.GenerateCaptchaCode();
        ViewBag.CaptchaCode = newCode;
        ViewBag.EncryptedCaptcha = _protector.Protect(newCode);
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
        string code = hp.GenerateCaptchaCode();

        // 1. Show the code to the user
        ViewBag.CaptchaCode = code;

        // 2. Encrypt the answer for the hidden field
        ViewBag.EncryptedCaptcha = _protector.Protect(code);
        ViewBag.ReturnUrl = returnUrl;
        return View("~/Views/Home/Login.cshtml");
    }


    // POST: Account/Login - Handles user authentication
    // UserController.cs

    // ... (existing Register actions and GET Login action) ...

    // POST: User/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Login(LoginVM vm, string encryptedCaptcha)
    {
        try
        {
            // Decrypt the hidden answer sent back by the browser
            string actualCode = _protector.Unprotect(encryptedCaptcha);

            if (string.IsNullOrEmpty(vm.CaptchaInput) || vm.CaptchaInput.ToUpper() != actualCode)
            {
                ModelState.AddModelError("CaptchaInput", "Invalid verification code.");
            }
        }
        catch
        {
            ModelState.AddModelError("CaptchaInput", "Verification expired. Please try again.");
        }

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

        string newCode = hp.GenerateCaptchaCode();
        ViewBag.CaptchaCode = newCode;
        ViewBag.EncryptedCaptcha = _protector.Protect(newCode);
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

    [AllowAnonymous] // Ensure everyone can see the error page
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
    [HttpGet]
    public IActionResult ChangePassword()
    {
        string code = hp.GenerateCaptchaCode(); // Get random string from Helper

        ViewBag.CaptchaCode = code;
        ViewBag.EncryptedCaptcha = _protector.Protect(code); // Encrypt answer

        return View();
    }

    // POST: /User/ChangePassword
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken] // Recommended best practice for security
    public async Task<IActionResult> ChangePassword(ChangePasswordVM vm, string encryptedCaptcha)
    {
        try
        {
            // Decrypt the hidden answer to verify identity
            string actualCode = _protector.Unprotect(encryptedCaptcha);

            if (string.IsNullOrEmpty(vm.CaptchaInput) || vm.CaptchaInput.ToUpper() != actualCode)
            {
                ModelState.AddModelError("CaptchaInput", "Invalid verification code.");
            }
        }
        catch
        {
            ModelState.AddModelError("CaptchaInput", "Security check expired. Please try again.");
        }
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
            if (!hp.VerifyPassword(u.PasswordHash, vm.Token))
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

        string newCode = hp.GenerateCaptchaCode();
        ViewBag.CaptchaCode = newCode;
        ViewBag.EncryptedCaptcha = _protector.Protect(newCode);
        // If ModelState was invalid from the start (e.g., New/Confirm mismatch)
        return View(vm);
    }

    private void SendResetPasswordEmail(User u, string password)
    {
        var mail = new MailMessage();
        mail.To.Add(new MailAddress(u.Email, u.Name));
        mail.Subject = "Movie Theme - Your New Password";
        mail.IsBodyHtml = true;

        // Attach user photo logic (keeping your professional style)
        var path = u switch
        {
            Admin => Path.Combine(en.WebRootPath, "photos", "admin.jpg"),
            Member m => Path.Combine(en.WebRootPath, "photos", m.PhotoURL ?? "default.jpg"),
            _ => Path.Combine(en.WebRootPath, "img", "default.jpg"),
        };

        if (System.IO.File.Exists(path))
        {
            var att = new Attachment(path);
            att.ContentId = "photo";
            mail.Attachments.Add(att);
        }

        mail.Body = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; border: 1px solid #ddd; padding: 20px; text-align: center;'>
            <div style='background: #1a1a1a; padding: 15px; margin-bottom: 20px;'>
                <h2 style='color: #ff5500; margin: 0;'>MOVIE THEME</h2>
            </div>
            <img src='cid:photo' style='width: 100px; height: 100px; border-radius: 50%;'>
            <h3>Hello {u.Name},</h3>
            <p>Your password has been reset successfully. Please use the temporary password below to log in:</p>
            <div style='background: #f4f4f4; padding: 15px; font-size: 20px; font-weight: bold; color: #ff5500; letter-spacing: 2px;'>
                {password}
            </div>
            <p style='margin-top: 20px;'>We recommend changing this password immediately after logging in.</p>
            <p style='color: #888;'>From, 🐱 Super Admin</p>
        </div>";

        hp.SendEmail(mail);
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            TempData["Info"] = "<div class='alert alert-danger'>Email not found!</div>";
            return View();
        }

        // Generate a secure token (UserId + Expiration Ticks)
        string payload = $"{user.UserId}|{DateTime.UtcNow.AddHours(1).Ticks}";
        string token = _protector.Protect(payload);
        string resetLink = Url.Action("ResetPassword", "User", new { token = token }, Request.Scheme);

        // Create verification email
        var mail = new MailMessage();
        mail.To.Add(new MailAddress(user.Email));
        mail.Subject = "Identity Verification - Reset Your Password";
        mail.IsBodyHtml = true;
        mail.Body = $@"<h3>Hello {user.Name},</h3>
                  <p>Click the link below to verify your identity and set a new password:</p>
                  <p><a href='{resetLink}'>VERIFY AND RESET PASSWORD</a></p>";

        hp.SendEmail(mail);

        TempData["Info"] = "<div class='alert alert-success'>Verification link sent to your Gmail!</div>";
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult ResetPassword(string token)
    {
        // This passes the token from the email link into the hidden field in your View
        return View(new ResetPasswordVM { Token = token });
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(ResetPasswordVM vm)
    {
        try
        {
            // 1. Decrypt token to verify person's identity
            string decrypted = _protector.Unprotect(vm.Token);
            var parts = decrypted.Split('|');
            string userId = parts[0];
            long expiry = long.Parse(parts[1]);

            // 2. Check if link has expired
            if (DateTime.UtcNow.Ticks > expiry) return Content("Verification link expired.");

            // 3. Update the password now that identity is proven
            var user = await db.Users.FindAsync(userId);
            if (user != null)
            {
                user.PasswordHash = hp.HashPassword(vm.New);
                await db.SaveChangesAsync();
                TempData["Info"] = "Identity verified. Password updated!";
                return RedirectToAction("Login");
            }
        }
        catch
        {
            return Content("Invalid or tampered security link.");
        }
        return RedirectToAction("Login");
    }
}
