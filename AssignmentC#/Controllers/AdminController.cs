using AssignmentC_;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssignmentC_.Controllers;

// Restrict access to this entire controller to users with the "Admin" role
//[Authorize(Roles = "Admin")]
public class AdminController(DB db, Helper hp) : Controller
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
    {
        // Fetch all movie records from the database
        var movieList = await db.Movies.ToListAsync();

        // Pass the list to the view (will be found at Views/Admin/Movies.cshtml)
        return View(movieList);
    }

    // ----------------------------------------------------------------
    // 2. CREATE / UPDATE (Upsert Movie) - GET: /Admin/UpsertMovie
    // ----------------------------------------------------------------
    // Loads the form for either adding a new movie (id=null) or editing an existing one (id!=null).
    public async Task<IActionResult> UpsertMovie(int? id)
    {
        if (id == null)
        {
            // CREATE: Return an empty ViewModel for adding a new movie
            return View(new MovieViewModel() { PremierDate = DateTime.Today });
        }

        // UPDATE: Find the existing movie
        var movie = await db.Movies.FindAsync(id);

        if (movie == null)
        {
            return NotFound();
        }

        // Map the Movie model data to the ViewModel for the form to display existing values
        var viewModel = new MovieViewModel
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

        return View(viewModel);
    }

    // ----------------------------------------------------------------
    // 3. CREATE / UPDATE (Upsert Movie) - POST: /Admin/UpsertMovie
    // ----------------------------------------------------------------
    // Handles form submission, file upload, validation, and database save.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpsertMovie(MovieViewModel viewModel)
    {
        // For an existing movie (Update), remove required validation on file uploads 
        // if the user is not uploading a new image (i.e., PosterUrl exists).
        if (viewModel.MovieId != 0)
        {
            ModelState.Remove(nameof(MovieViewModel.PosterFile));
            ModelState.Remove(nameof(MovieViewModel.BannerFile));

            // Re-validate if the image URLs are empty when files are not provided
            if (string.IsNullOrEmpty(viewModel.PosterUrl) && viewModel.PosterFile == null)
                ModelState.AddModelError(nameof(MovieViewModel.PosterFile), "Poster Image is required if no file is uploaded.");

            if (string.IsNullOrEmpty(viewModel.BannerUrl) && viewModel.BannerFile == null)
                ModelState.AddModelError(nameof(MovieViewModel.BannerFile), "Banner Image is required if no file is uploaded.");
        }

        if (ModelState.IsValid)
        {
            // 1. Handle File Uploads (using SaveImage/DeleteImage from Step 8)
            // Save new poster/banner, and delete old files if updating
            if (viewModel.PosterFile != null)
            {
                if (viewModel.MovieId != 0 && !string.IsNullOrEmpty(viewModel.PosterUrl))
                {
                    hp.DeleteImage(viewModel.PosterUrl);
                }
                // SaveImage is assumed to return the URL path (e.g., "/img/unique.jpg")
                viewModel.PosterUrl = hp.SaveImage(viewModel.PosterFile);
            }

            if (viewModel.BannerFile != null)
            {
                if (viewModel.MovieId != 0 && !string.IsNullOrEmpty(viewModel.BannerUrl))
                {
                    hp.DeleteImage(viewModel.BannerUrl);
                }
                viewModel.BannerUrl = hp.SaveImage(viewModel.BannerFile);
            }

            // 2. Map ViewModel to Model
            var movie = new Movie
            {
                MovieId = viewModel.MovieId,
                Title = viewModel.Title,
                Description = viewModel.Description,
                Genre = viewModel.Genre,
                DurationMinutes = viewModel.DurationMinutes,
                Rating = viewModel.Rating,
                Director = viewModel.Director,
                Writer = viewModel.Writer,
                PremierDate = viewModel.PremierDate,
                PosterUrl = viewModel.PosterUrl,
                BannerUrl = viewModel.BannerUrl,
                TrailerUrl = viewModel.TrailerUrl,
            };

            // 3. Database Operation
            if (viewModel.MovieId == 0)
            {
                // CREATE
                db.Movies.Add(movie);
                TempData["Info"] = $"Movie '{movie.Title}' added successfully.";
            }
            else
            {
                // UPDATE
                db.Movies.Update(movie);
                TempData["Info"] = $"Movie '{movie.Title}' updated successfully.";
            }

            await db.SaveChangesAsync();
            return RedirectToAction("Movies");
        }

        // If validation fails, return the form with the errors
        return View(viewModel);
    }

    // ----------------------------------------------------------------
    // 4. DELETE (Delete Movie) - POST: /Admin/DeleteMovie
    // ----------------------------------------------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMovie(int id)
    {
        var movie = await db.Movies.FindAsync(id);

        if (movie == null)
        {
            TempData["Error"] = "Movie not found.";
            return RedirectToAction("Movies");
        }

        // 1. Delete associated images from the server's disk using the Helper
        if (!string.IsNullOrEmpty(movie.PosterUrl))
        {
            hp.DeleteImage(movie.PosterUrl);
        }
        if (!string.IsNullOrEmpty(movie.BannerUrl))
        {
            hp.DeleteImage(movie.BannerUrl);
        }

        // 2. Delete the movie from the database
        db.Movies.Remove(movie);
        await db.SaveChangesAsync();

        TempData["Info"] = $"Movie '{movie.Title}' deleted successfully.";
        return RedirectToAction("Movies");

    }
    public IActionResult EditHallSeats()
    {
        return View();
    }
    public IActionResult ManageHalls()
    {
        return View();
    }
    public IActionResult AddHall()
    {
        return View();
    }
}