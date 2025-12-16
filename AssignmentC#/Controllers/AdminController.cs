using AssignmentC_;
using AssignmentC_.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace AssignmentC_.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly DB db;
    private readonly Helper hp;
    private readonly IWebHostEnvironment he;

    public AdminController(DB db, Helper hp, IWebHostEnvironment he)
    {
        this.db = db;
        this.hp = hp;
        this.he = he;
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

        var member = await db.Members.FirstOrDefaultAsync(m => m.UserId == id);

        // If Member record is missing, create a temp object for the VM
        if (member == null)
        {
            member = new Member
            {
                UserId = user.UserId,
                Phone = "",
                Gender = "",
                PhotoURL = "/img/default.jpg"
            };
        }

        var vm = new AdminEditUserVM
        {
            Id = user.UserId, // Matches new VM property name
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            Phone = member.Phone,
            Gender = member.Gender,
            CurrentPhotoUrl = member.PhotoURL // Matches new VM property name
        };

        return View("~/Views/User/AdminEditUser.cshtml", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdminEditUser(AdminEditUserVM vm)
    {
        // 1. Validation Check
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Validation failed. Please check your inputs.";
            return View("~/Views/User/AdminEditUser.cshtml", vm);
        }

        // 2. Fetch current entities using vm.Id
        var userToUpdate = await db.Users.FirstOrDefaultAsync(u => u.UserId == vm.Id);
        var memberToUpdate = await db.Members.FirstOrDefaultAsync(m => m.UserId == vm.Id);

        if (userToUpdate == null)
        {
            TempData["Error"] = "Error: User not found.";
            return RedirectToAction("MemberList","User");
        }

        // 3. Handle Member record creation if it doesn't exist
        bool isNewMember = false;
        if (memberToUpdate == null)
        {
            memberToUpdate = new Member { UserId = vm.Id, PhotoURL = "/img/default.jpg" };
            db.Members.Add(memberToUpdate);
            isNewMember = true;
        }

        // 4. Photo Upload Logic (NewPhoto)
        if (vm.NewPhoto != null && vm.NewPhoto.Length > 0)
        {
            var wwwRootPath = he.WebRootPath;

            // Delete old file if it's not the default
            if (memberToUpdate.PhotoURL != "/img/default.jpg" && !string.IsNullOrEmpty(memberToUpdate.PhotoURL))
            {
                var oldPath = Path.Combine(wwwRootPath, memberToUpdate.PhotoURL.TrimStart('/'));
                if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
            }

            // Save new file
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

        // 5. Update Properties
        userToUpdate.Name = vm.Name;
        userToUpdate.Email = vm.Email;
        memberToUpdate.Phone = vm.Phone;
        memberToUpdate.Gender = vm.Gender;

        // 6. Save Changes
        try
        {
            db.Users.Update(userToUpdate);
            if (!isNewMember) db.Members.Update(memberToUpdate);

            await db.SaveChangesAsync();
            TempData["Info"] = "User updated successfully!";
            return RedirectToAction("MemberList","User");
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Save failed: " + ex.Message;
            return View("~/Views/User/AdminEditUser.cshtml", vm);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdminDeleteUser(string id)
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
                var filePath = Path.Combine(he.WebRootPath, member.PhotoURL.TrimStart('/'));
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

        return RedirectToAction("MemberList", "User");
    }













    // ========== MOVIE LIST ==========
    public IActionResult Movies()
    {
        var movieList = db.Movies.ToList();
        return View(movieList);
    }

    // ========== UPSERT MOVIE (GET) ==========
    [HttpGet]
    public IActionResult UpsertMovie(int? id)
    {
        if (id == null || id == 0)
        {
            // ADD MODE — return empty ViewModel
            return View(new MovieViewModel());
        }

        // EDIT MODE — load from DB
        var movie = db.Movies.FirstOrDefault(m => m.MovieId == id);
        if (movie == null)
        {
            TempData["Error"] = "Movie not found.";
            return RedirectToAction("Movies");
        }

        // Convert Movie → MovieViewModel
        var vm = new MovieViewModel
        {
            MovieId = movie.MovieId,
            Title = movie.Title,
            Description = movie.Description,
            Genre = movie.Genre,
            DurationMinutes = movie.DurationMinutes,
            Rating = movie.Rating,
            Director = movie.Director,
            Writer = movie.Writer,
            PremierDate = movie.PremierDate,
            PosterUrl = movie.PosterUrl,
            BannerUrl = movie.BannerUrl,
            TrailerUrl = movie.TrailerUrl
        };

        return View(vm);
    }

    // ========== UPSERT MOVIE (POST) ==========
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpsertMovie(MovieViewModel vm)
    {
        // -------------------------------
        // Validate required files for new movie
        // -------------------------------
        ModelState.Remove("PosterUrl");
        ModelState.Remove("BannerUrl");

        if (vm.MovieId == 0)
        {
            if (vm.PosterFile == null)
                ModelState.AddModelError("PosterFile", "Poster image is required.");
            if (vm.BannerFile == null)
                ModelState.AddModelError("BannerFile", "Banner image is required.");
        }

        // -------------------------------
        // Validate file types & size
        // -------------------------------
        if (vm.PosterFile != null)
        {
            var err = hp.ValidatePhoto(vm.PosterFile);
            if (!string.IsNullOrEmpty(err))
                ModelState.AddModelError("PosterFile", err);
        }

        if (vm.BannerFile != null)
        {
            var err = hp.ValidatePhoto(vm.BannerFile);
            if (!string.IsNullOrEmpty(err))
                ModelState.AddModelError("BannerFile", err);
        }

        if (!ModelState.IsValid)
            return View(vm);

        Movie movie;

        if (vm.MovieId == 0)
        {
            // -------------------------------
            // ADD NEW MOVIE
            // -------------------------------
            movie = new Movie
            {
                PosterUrl = vm.PosterFile != null ? hp.SavePhotoNoResize(vm.PosterFile, "uploads") : null,
                BannerUrl = vm.BannerFile != null ? hp.SavePhotoNoResize(vm.BannerFile, "uploads") : null
            };

            db.Movies.Add(movie);
            TempData["Info"] = "Movie added successfully!";
        }
        else
        {
            // -------------------------------
            // EDIT EXISTING MOVIE
            // -------------------------------
            movie = db.Movies.FirstOrDefault(m => m.MovieId == vm.MovieId);
            if (movie == null)
                return NotFound();

            // Replace poster if new file uploaded
            if (vm.PosterFile != null)
            {
                if (!string.IsNullOrEmpty(movie.PosterUrl))
                    hp.DeletePhoto(movie.PosterUrl, "uploads");

                movie.PosterUrl = hp.SavePhotoNoResize(vm.PosterFile, "uploads");
            }

            // Replace banner if new file uploaded
            if (vm.BannerFile != null)
            {
                if (!string.IsNullOrEmpty(movie.BannerUrl))
                    hp.DeletePhoto(movie.BannerUrl, "uploads");

                movie.BannerUrl = hp.SavePhotoNoResize(vm.BannerFile, "uploads");
            }

            TempData["Info"] = "Movie updated successfully!";
        }

        // -------------------------------
        // Copy other fields
        // -------------------------------
        movie.Title = vm.Title;
        movie.Description = vm.Description;
        movie.Genre = vm.Genre;
        movie.DurationMinutes = vm.DurationMinutes;
        movie.Rating = vm.Rating;
        movie.Director = vm.Director;
        movie.Writer = vm.Writer;
        movie.PremierDate = vm.PremierDate.Value;
        movie.TrailerUrl = vm.TrailerUrl;

        db.SaveChanges();

        return RedirectToAction("Movies");

    }

}