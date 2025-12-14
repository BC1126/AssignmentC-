using AssignmentC_;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssignmentC_.Models;

namespace AssignmentC_.Controllers;

public class AdminController : Controller
{
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

    // ----------------------------------------------------------------
    // 1. READ (List All Movies) - GET: /Admin/Movies
    // ----------------------------------------------------------------
    // Displays a list/table of all movies for the Admin dashboard.
    public async Task<IActionResult> Movies()
    private readonly DB db;
    private readonly Helper hp;

    public AdminController(DB db, Helper hp)
    {
        this.db = db;
        this.hp = hp;
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



}
