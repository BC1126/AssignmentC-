using AssignmentC_;
using AssignmentC_.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using X.PagedList.Extensions;

namespace AssignmentC_.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly DB db;
    private readonly Helper hp;
    private readonly IWebHostEnvironment en;

    public AdminController(DB db, Helper hp, IWebHostEnvironment en)
    {
        this.db = db;
        this.hp = hp;
        this.en = en;
    }

    public IActionResult AdminDashboard()
    {
        return View("~/Views/Home/AdminDashboard.cshtml");
    }

    [HttpGet]
    public async Task<IActionResult> AdminEditUser(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            TempData["Error"] = "User ID not specified.";
            return RedirectToAction("MemberList");
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == id);
        if (user == null)
        {
            TempData["Error"] = $"User with ID {id} not found.";
            return RedirectToAction("MemberList");
        }

        // Initialize temporary variables to hold profile data
        string phone = "";
        string gender = "";
        string photo = "/img/default.jpg";

        // --- MODIFIED PART: ROLE-BASED DATA RETRIEVAL ---
        if (user.Role == "Staff")
        {
            // Try to get data from Staff table
            var staff = await db.Staffs.FirstOrDefaultAsync(s => s.UserId == id);
            if (staff != null)
            {
                phone = staff.Phone;
                gender = staff.Gender;
            }
            // Staff photo is ALWAYS forced to default as per your request
            photo = "/img/default.jpg";
        }
        else
        {
            // Try to get data from Member table
            var member = await db.Members.FirstOrDefaultAsync(m => m.UserId == id);
            if (member != null)
            {
                phone = member.Phone;
                gender = member.Gender;
                photo = member.PhotoURL;
            }
        }
        // --- END OF MODIFIED PART ---

        var vm = new AdminEditUserVM
        {
            Id = user.UserId,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            Phone = phone,
            Gender = gender,
            CurrentPhotoUrl = photo
        };

        return View("~/Views/User/AdminEditUser.cshtml", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdminEditUser(AdminEditUserVM vm, string returnUrl)
    {
        // 1. Validation Check
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Validation failed. Please check your inputs.";
            return View("~/Views/User/AdminEditUser.cshtml", vm);
        }

        // 2. Fetch User first to check the Role
        var userToUpdate = await db.Users.FirstOrDefaultAsync(u => u.UserId == vm.Id);
        if (userToUpdate == null)
        {
            TempData["Error"] = "Error: User not found.";
            return RedirectToAction("MemberList", "User");
        }

        // 3. Update common User info
        userToUpdate.Name = vm.Name;
        userToUpdate.Email = vm.Email;

        // --- MODIFIED PART: IF-ELSE FOR STAFF VS MEMBER ---
        if (userToUpdate.Role == "Staff")
        {
            // Get existing Staff record
            var staffToUpdate = await db.Staffs.FirstOrDefaultAsync(s => s.UserId == vm.Id);
            if (staffToUpdate != null)
            {
                staffToUpdate.Phone = vm.Phone;
                staffToUpdate.Gender = vm.Gender;
            }
        }
        else
        {
            // Get existing Member record
            var memberToUpdate = await db.Members.FirstOrDefaultAsync(m => m.UserId == vm.Id);

            // If member doesn't exist, create it (avoiding the tracking error)
            if (memberToUpdate == null)
            {
                memberToUpdate = new Member { UserId = vm.Id, PhotoURL = "/img/default.jpg" };
                db.Members.Add(memberToUpdate);
            }

            memberToUpdate.Phone = vm.Phone;
            memberToUpdate.Gender = vm.Gender;

            // Photo Upload Logic for regular members
            if (vm.NewPhoto != null && vm.NewPhoto.Length > 0)
            {
                var wwwRootPath = en.WebRootPath;
                if (memberToUpdate.PhotoURL != "/img/default.jpg" && !string.IsNullOrEmpty(memberToUpdate.PhotoURL))
                {
                    var oldPath = Path.Combine(wwwRootPath, memberToUpdate.PhotoURL.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }

                var fileName = $"{vm.Id}_{DateTime.Now.Ticks}{Path.GetExtension(vm.NewPhoto.FileName)}";
                var uploadDir = Path.Combine(wwwRootPath, "img");
                if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

                var filePath = Path.Combine(uploadDir, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await vm.NewPhoto.CopyToAsync(stream);
                }
                memberToUpdate.PhotoURL = $"/img/{fileName}";
            }
        }
        // --- END OF MODIFIED PART ---

        // 6. Save Changes
        try
        {
            // SaveChangesAsync handles both Users and Members/Staff updates automatically
            await db.SaveChangesAsync();
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            TempData["Info"] = "User updated successfully!";
            return RedirectToAction("MemberList", "User");
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Save failed: " + ex.Message;
            return View("~/Views/User/AdminEditUser.cshtml", vm);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdminDeleteUser(string id, string returnUrl)
    {
        if (string.IsNullOrEmpty(id))
        {
            TempData["Error"] = "User ID not specified.";
            return RedirectToAction("MemberList", "User");
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == id);
        var member = await db.Members.FirstOrDefaultAsync(m => m.UserId == id);

        if (user == null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction("MemberList", "User");
        }

        try
        {
            // 1. Delete the physical photo file if it's not the default
            if (member != null && !string.IsNullOrEmpty(member.PhotoURL) && member.PhotoURL != "/img/default.jpg")
            {
                var filePath = Path.Combine(en.WebRootPath, member.PhotoURL.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            // 2. Remove records from database
            if (member != null) db.Members.Remove(member);
            db.Users.Remove(user);

            await db.SaveChangesAsync();
            TempData["Info"] = $"User {id} deleted successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Delete failed: " + ex.Message;
        }
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("MemberList", "User");
    }

    [HttpGet]
    public IActionResult AdminAddUser(string role)
    {
        // Default to Staff if nothing is passed, or handle errors
        var vm = new AddUserVM
        {
            Role = role ?? "Staff"
        };

        return View("~/Views/User/AdminAddUser.cshtml", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AdminAddUser(AddUserVM vm)
    {
        // 1. Check for email existence
        if (ModelState.GetValidationState("Email") != ModelValidationState.Invalid &&
            db.Users.Any(u => u.Email == vm.Email))
        {
            ModelState.AddModelError("Email", "Duplicated Email.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                if (vm.Role == "Staff")
                {
                    var newStaff = new Staff
                    {
                        UserId = hp.GenerateNextUserId("S"),
                        Name = vm.Name,
                        Email = vm.Email,
                        Phone = vm.Phone ?? "",
                        PasswordHash = hp.HashPassword("Default123!"),
                        Gender = vm.Gender.Substring(0, 1).ToUpper()
                    };
                    db.Users.Add(newStaff);
                }
                else // Role is Member
                {
                    // --- PHOTO LOGIC: Only for Members ---
                    string photoUrl = "";
                    string fileName = Guid.NewGuid().ToString("n") + ".jpg";
                    string sourceFile = Path.Combine(en.WebRootPath, "img", "default.jpg");
                    string destFolder = Path.Combine(en.WebRootPath, "photos");
                    string destFile = Path.Combine(destFolder, fileName);

                    if (!Directory.Exists(destFolder)) Directory.CreateDirectory(destFolder);

                    if (System.IO.File.Exists(sourceFile))
                    {
                        System.IO.File.Copy(sourceFile, destFile);
                        photoUrl = $"/photos/{fileName}";
                    }

                    var newMember = new Member
                    {
                        UserId = hp.GenerateNextUserId("M"),
                        Name = vm.Name,
                        Email = vm.Email,
                        Phone = vm.Phone ?? "",
                        PasswordHash = hp.HashPassword("Default123!"),
                        Gender = vm.Gender.Substring(0, 1).ToUpper(),
                        PhotoURL = photoUrl
                    };
                    db.Users.Add(newMember);
                }

                db.SaveChanges();

                TempData["Info"] = $"{vm.Role} added successfully!";

                return vm.Role == "Staff"
                    ? RedirectToAction("StaffList", "User")
                    : RedirectToAction("MemberList", "User");
            }
            catch (Exception ex)
            {
                var error = ex.InnerException?.Message ?? ex.Message;
                ModelState.AddModelError("", "Database Error: " + error);
            }
        }

        return View("../User/AdminAddUser", vm);
    }

    [Authorize(Roles = "Admin")]
    public IActionResult ViewOrder(string search, string sort = "Id", string dir = "desc", int page = 1)
    {
        var orders = db.Orders
            .Include(o => o.OrderLines)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            orders = orders.Where(o =>
                o.Id.ToString().Contains(search) ||
                o.Cinema.Contains(search) ||
                o.MemberEmail.Contains(search));
        }

        orders = (sort, dir) switch
        {
            ("Date", "asc") => orders.OrderBy(o => o.Date),
            ("Date", "des") => orders.OrderByDescending(o => o.Date),
            ("Id", "asc") => orders.OrderBy(o => o.Id),
            ("Id", "des") => orders.OrderByDescending(o => o.Id),
            _ => orders.OrderByDescending(o => o.Id)
        };

        var model = orders.ToPagedList(page, 5);

        ViewBag.Search = search;
        ViewBag.Sort = sort;
        ViewBag.Dir = dir;

        if (Request.IsAjax())
        {
            return PartialView("_AdminProductOrder", model);
        }
        else
        {
            return View("~/Views/Admin/AdminViewProductOrder.cshtml", model);
        }
    }



    [Authorize(Roles = "Admin")]
    public IActionResult ViewOrderDetails(int id)
    {
        var order = db.Orders
            .Include(o => o.OrderLines)
            .ThenInclude(ol => ol.Product)
            .FirstOrDefault(o => o.Id == id);

        if (order == null)
        {
            TempData["Error"] = "Order not found.";
            return RedirectToAction("ViewOrder");
        }

        return View("~/Views/Admin/AdminViewProductOrderDetails.cshtml", order);
    }


}