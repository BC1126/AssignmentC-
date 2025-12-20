using AssignmentC_.Models;
using AssignmentC_.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.IO;
using System.Net.Mail;
using System.Security.Claims;

namespace AssignmentC_.Controllers;

#nullable disable warnings
public class UserController : Controller
{
    private readonly DB db;
    private readonly Helper hp;
    private readonly IWebHostEnvironment en;
    private readonly IDataProtector _protector;
    private readonly IMemoryCache _cache;

    public UserController(DB db, Helper hp, IWebHostEnvironment en, IDataProtectionProvider provider, IMemoryCache cache)
    {
        this.db = db;
        this.hp = hp;
        this.en = en;
        _protector = provider.CreateProtector("PasswordResetPurpose");
        _protector = provider.CreateProtector("CaptchaProtector");
        _cache = cache;
    }

    [Authorize(Roles = "Admin, Staff")]
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

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            // If the user is Staff, send them the Read-Only table
            if (User.IsInRole("Staff"))
            {
                return PartialView("_StaffMemberList", model);
            }

            // Everyone else (Admin) gets the full Edit/Delete table
            return PartialView("_MemberList", model);
        }

        // Standard request returns the full page
        return View(model);
    }

    [Authorize(Roles = "Admin")]
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

    [Authorize(Roles = "Admin")]
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

    // POST: User/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Register(RegisterVM vm, string encryptedCaptcha)
    {
        // --- 1. CAPTCHA CHECK ---
        try
        {
            string actualCode = _protector.Unprotect(encryptedCaptcha);
            if (string.IsNullOrEmpty(vm.CaptchaInput) || vm.CaptchaInput.ToUpper() != actualCode)
            {
                ModelState.AddModelError("CaptchaInput", "Invalid verification code.");
            }
        }
        catch
        {
            ModelState.AddModelError("CaptchaInput", "Verification expired.");
        }

        // --- 2. DUPLICATE CHECK ---
        if (db.Users.Any(u => u.Email == vm.Email))
        {
            ModelState.AddModelError("Email", "Email already in use.");
        }

        // --- 3. PHOTO LOGIC ---
        string photoUrl = "/photos/default.jpg"; // Default
        if (vm.Photo != null)
        {
            photoUrl = hp.SavePhoto(vm.Photo, "photos");
        }
        // (You can add your default image copy logic here if needed)

        // =========================================================
        // MAIN LOGIC START
        // =========================================================
        if (ModelState.IsValid)
        {
            // A. CREATE USER OBJECT
            string newUserId = hp.GenerateNextUserId("M");
            var newMember = new Member
            {
                UserId = newUserId,
                Email = vm.Email,
                PasswordHash = hp.HashPassword(vm.Password),
                Name = vm.Name,
                PhotoURL = photoUrl,
                Gender = vm.Gender,
                Phone = vm.Phone,
                IsEmailConfirmed = false
            };

            // B. TRY TO SAVE TO DB
            try
            {
                db.Members.Add(newMember);
                db.SaveChanges(); // <--- CRITICAL MOMENT
            }
            catch (Exception ex)
            {
                // If DB fails, show error and STOP.
                ModelState.AddModelError("", "Database Error: " + ex.Message);
                goto Fail; // Jump to the end to reload page
            }

            // C. TRY TO SEND EMAIL (Separate Try/Catch)
            // If this fails, we STILL redirect because the user is already created!
            string secureToken = _protector.Protect(newUserId);
            var verifyUrl = Url.Action("Verify", "User", new { token = secureToken }, protocol: Request.Scheme);

            try
            {
                var mail = new System.Net.Mail.MailMessage
                {
                    Subject = "Welcome to YSL Cinema! Please Verify Your Email",
                    IsBodyHtml = true,
                    Body = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 5px;'>
                            <h2 style='color: #333;'>Welcome to YSL Cinema Ticketing!</h2>
        
                            <p>Hi there,</p>

                            <p>Thank you for signing up with <strong>YSL Cinema Ticketing</strong>. We are excited to have you on board!</p>
        
                            <p>To get started and access your account, please verify your email address by clicking the button below:</p>

                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{verifyUrl}' style='background-color: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; font-weight: bold;'>
                                    VERIFY MY EMAIL
                                </a>
                            </div>

                            <p>If the button doesn't work, you can copy and paste this link into your browser:</p>
                            <p><a href='{verifyUrl}'>{verifyUrl}</a></p>

                            <hr style='border: 0; border-top: 1px solid #eee; margin: 20px 0;'>

                            <p style='font-size: 12px; color: #777;'>
                                If you didn't create an account with YSL Cinema Ticketing, you can safely delete this email.
                            </p>

                            <p style='margin-top: 20px;'>
                                Cheers,<br>
                                <strong>The YSL Cinema Ticketing Team</strong>
                            </p>
                        </div>"
                };
                mail.To.Add(vm.Email);
                hp.SendEmail(mail);
            }
            catch (Exception ex)
            {
                // Just log it to TempData, don't stop the redirect
                TempData["Error"] = "User registered, but email failed: " + ex.Message;
                TempData["DebugLink"] = verifyUrl; // Keep this so you can verify manually
            }

            // D. SUCCESS REDIRECT
            return RedirectToAction("VerifyEmailSent", "User");
        }

    // =========================================================
    // FAILURE HANDLER
    // =========================================================
    Fail:
        string newCode = hp.GenerateCaptchaCode();
        ViewBag.CaptchaCode = newCode;
        ViewBag.EncryptedCaptcha = _protector.Protect(newCode);
        return View("~/Views/Home/Register.cshtml", vm);
    }

    public IActionResult Verify(string token)
    {
        if (string.IsNullOrEmpty(token)) return BadRequest("Token is missing");

        try
        {
            string userId = _protector.Unprotect(token);

            var user = db.Users.FirstOrDefault(u => u.UserId == userId);
            if (user == null) return NotFound("User not found.");

            if (!user.IsEmailConfirmed)
            {
                user.IsEmailConfirmed = true;
                db.SaveChanges();
            }

            return View("VerifySuccess");
        }
        catch
        {
            return BadRequest("Invalid or expired token.");
        }
    }

    public IActionResult VerifyEmailSent()
    {
        return View();
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Login(LoginVM vm, string encryptedCaptcha)
    {
        string cacheKey = $"LoginFail_{vm.Email}";
        string loginViewPath = "~/Views/Home/Login.cshtml";

        // 1. Lockout Check
        if (_cache.TryGetValue(cacheKey, out int fails) && fails >= 3)
        {
            ModelState.AddModelError("", "Too many failed attempts. Locked for 15 minutes.");
            goto SkipToView;
        }

        // 2. CAPTCHA Verification
        try
        {
            string actualCode = _protector.Unprotect(encryptedCaptcha);
            if (string.IsNullOrEmpty(vm.CaptchaInput) || vm.CaptchaInput.ToUpper() != actualCode)
            {
                ModelState.AddModelError("CaptchaInput", "Invalid verification code.");
            }
        }
        catch
        {
            ModelState.AddModelError("CaptchaInput", "Verification expired.");
        }

        // 3. Sequential Database Search
        if (ModelState.IsValid)
        {
            // Start by checking Members
            User user = db.Members.FirstOrDefault(u => u.Email == vm.Email);

            // Fallback to Staff
            if (user == null) user = db.Staffs.FirstOrDefault(u => u.Email == vm.Email);

            // Fallback to Admin
            if (user == null) user = db.Admins.FirstOrDefault(u => u.Email == vm.Email);

            if (user != null)
            {
                if (hp.VerifyPassword(user.PasswordHash, vm.Password))
                {
                    // ============================================================
                    // MODIFIED PART: Check Email Verification Status
                    // ============================================================
                    if (!user.IsEmailConfirmed)
                    {
                        ModelState.AddModelError("", "Please verify your email address before logging in.");
                        // Jump to end to refresh Captcha and show view
                        goto SkipToView;
                    }
                    // ============================================================

                    _cache.Remove(cacheKey);
                    hp.SignIn(user.Email, user.Role, vm.RememberMe);
                    TempData["Info"] = $"Welcome, {user.Name}!";

                    if (user is Admin) return RedirectToAction("AdminDashboard", "Admin");
                    if (user is Staff) return RedirectToAction("StaffDashboard", "Staff");
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    // 1. Increment cache counter
                    if (!_cache.TryGetValue(cacheKey, out fails)) fails = 0;
                    fails++;

                    // Set lockout duration (e.g., 15 minutes)
                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(15));
                    _cache.Set(cacheKey, fails, cacheOptions);

                    int remaining = 3 - fails;

                    if (remaining <= 0)
                    {
                        // This keeps the message visible after they click "Login" again
                        ModelState.AddModelError("Password", "Incorrect password. Account locked.");
                        ModelState.AddModelError("", "Too many failed attempts. Locked for 15 minutes.");
                        goto SkipToView;
                    }
                    else
                    {
                        // This triggers on the 1st and 2nd fail
                        ModelState.AddModelError("Password", $"Incorrect password. {remaining} attempts left.");
                    }
                }
            }
            else
            {
                ModelState.AddModelError("Email", "User account not found.");
            }
        }

    SkipToView:
        // 4. Refresh CAPTCHA and return
        string newCode = hp.GenerateCaptchaCode();
        ViewBag.CaptchaCode = newCode;
        ViewBag.EncryptedCaptcha = _protector.Protect(newCode);

        return View(loginViewPath, vm);
    }

    // ====================================================================
    // 3. LOGOUT ACTION
    // ====================================================================

    // GET: User/SignOut
    [Authorize]
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
    // GET: /User/EditProfile
    // GET: /User/EditProfile
    // GET: /User/EditProfile
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> EditProfile()
    {
        string userId = null;

        // 1. Try to get ID from Claims (The standard way)
        userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                 ?? User.FindFirst("UserId")?.Value;

        // 2. FALLBACK: If ID is null, use the Email (User.Identity.Name) to find the user
        if (userId == null && User.Identity.IsAuthenticated)
        {
            string email = User.Identity.Name; // This holds the email from hp.SignIn()

            // Find the user in the main Users table by Email
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null)
            {
                userId = user.UserId; // Recover the ID!
            }
        }

        // 3. If userId is STILL null, then we really must redirect to login
        if (userId == null) return RedirectToAction("Login", "User");

        // ---------------------------------------------------------------
        // From here on, the logic is exactly the same as before...
        // ---------------------------------------------------------------

        var vm = new EditProfileVM { Id = userId };

        if (User.IsInRole("Member"))
        {
            var member = await db.Members.FirstOrDefaultAsync(m => m.UserId == userId);
            if (member == null) return RedirectToAction("Profile");

            vm.Name = member.Name;
            vm.Email = member.Email;
            vm.Role = "Member";
            vm.Phone = member.Phone;
            vm.Gender = member.Gender;
            vm.CurrentPhotoUrl = member.PhotoURL;
        }
        else if (User.IsInRole("Staff"))
        {
            var staff = await db.Staffs.FirstOrDefaultAsync(s => s.UserId == userId);
            if (staff == null) return RedirectToAction("Profile");

            vm.Name = staff.Name;
            vm.Email = staff.Email;
            vm.Role = "Staff";
            vm.Phone = staff.Phone;
            vm.Gender = staff.Gender;
            vm.CurrentPhotoUrl = "/img/default.jpg"; // Default for Staff
        }
        else if (User.IsInRole("Admin"))
        {
            var admin = await db.Admins.FirstOrDefaultAsync(a => a.UserId == userId);
            if (admin == null) return RedirectToAction("Profile");

            vm.Name = admin.Name;
            vm.Email = admin.Email;
            vm.Role = "Admin";
            vm.Phone = admin.Phone;
            vm.Gender = admin.Gender;
            vm.CurrentPhotoUrl = "/img/default.jpg"; // Default for Admin
        }

        return View(vm);
    }

    // POST: /User/EditProfile
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(EditProfileVM vm)
    {
        if (!ModelState.IsValid) return View(vm);

        // 1. Save Data based on Role
        if (User.IsInRole("Member"))
        {
            var member = await db.Members.FirstOrDefaultAsync(m => m.UserId == vm.Id);
            if (member != null)
            {
                member.Name = vm.Name;
                member.Phone = vm.Phone;
                member.Gender = vm.Gender;

                // --- PHOTO LOGIC ONLY FOR MEMBERS ---
                if (vm.NewPhoto != null)
                {
                    var fileName = $"{vm.Id}_{DateTime.Now.Ticks}_{Path.GetExtension(vm.NewPhoto.FileName)}";
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img", fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await vm.NewPhoto.CopyToAsync(stream);
                    }
                    // Update the database path
                    member.PhotoURL = $"/img/{fileName}";
                }
                // ------------------------------------
            }
        }
        else if (User.IsInRole("Staff"))
        {
            var staff = await db.Staffs.FirstOrDefaultAsync(s => s.UserId == vm.Id);
            if (staff != null)
            {
                staff.Name = vm.Name;
                staff.Phone = vm.Phone;
                staff.Gender = vm.Gender;
                // No Photo update for Staff
            }
        }
        else if (User.IsInRole("Admin"))
        {
            var admin = await db.Admins.FirstOrDefaultAsync(a => a.UserId == vm.Id);
            if (admin != null)
            {
                admin.Name = vm.Name;
                admin.Phone = vm.Phone;
                admin.Gender = vm.Gender;
                // No Photo update for Admin
            }
        }

        // 2. Sync the main User table
        var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == vm.Id);
        if (user != null)
        {
            user.Name = vm.Name;
        }

        await db.SaveChangesAsync();

        TempData["Info"] = "Profile updated successfully!";
        return RedirectToAction("Profile");
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
        mail.Body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 5px;'>
                <h2 style='color: #333;'>Password Reset Request</h2>
        
                <p>Dear <strong>{user.Name}</strong>,</p>

                <p>We received a request to reset the password for your <strong>YSL Cinema Ticketing</strong> account.</p>
        
                <p>If you made this request, please click the button below to verify your identity and set a new password:</p>

                <div style='text-align: center; margin: 30px 0;'>
                    <a href='{resetLink}' style='background-color: #d9534f; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; font-weight: bold;'>
                        VERIFY AND RESET PASSWORD
                    </a>
                </div>

                <p>If the button above doesn't work, you can copy and paste the link below into your web browser:</p>
                <p><a href='{resetLink}'>{resetLink}</a></p>

                <hr style='border: 0; border-top: 1px solid #eee; margin: 20px 0;'>

                <p style='font-size: 12px; color: #777;'>
                    <strong>Security Notice:</strong> This link is valid for a limited time only. 
                    If you did not request a password reset, please ignore this email. Your password will remain unchanged.
                </p>

                <p style='margin-top: 20px;'>
                    Best Regards,<br>
                    <strong>The YSL Cinema Ticketing Team</strong>
                </p>
            </div>";

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
