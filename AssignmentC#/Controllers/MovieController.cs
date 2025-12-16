using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace AssignmentC_.Controllers;

public class MovieController : Controller
    {

        private readonly DB db;
        private readonly Helper hp;
        private readonly IWebHostEnvironment _env;

    public MovieController(DB db, Helper hp, IWebHostEnvironment env)
        {
            this.db = db;
            this.hp = hp;
            this._env = env;

    }

    //admin
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

    public IActionResult Delete(int id)
    {
        var movie = db.Movies.FirstOrDefault(m => m.MovieId == id);
        if (movie == null)
        {
            return NotFound();
        }

        return View(movie);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int movieId)
    {
        var movie = db.Movies
            .Include(m => m.ShowTimes)
            .Include(m => m.Reviews)
            .FirstOrDefault(m => m.MovieId == movieId);

        if (movie == null)
        {
            return NotFound();
        }

        // 🔥 Delete poster file
        if (!string.IsNullOrEmpty(movie.PosterUrl))
        {
            var posterPath = Path.Combine(_env.WebRootPath, "uploads", movie.PosterUrl);
            if (System.IO.File.Exists(posterPath))
            {
                System.IO.File.Delete(posterPath);
            }
        }

        // 🔥 Delete banner file
        if (!string.IsNullOrEmpty(movie.BannerUrl))
        {
            var bannerPath = Path.Combine(_env.WebRootPath, "uploads", movie.BannerUrl);
            if (System.IO.File.Exists(bannerPath))
            {
                System.IO.File.Delete(bannerPath);
            }
        }

        db.Movies.Remove(movie);
        db.SaveChanges();

        TempData["Info"] = "Movie deleted successfully.";
        return RedirectToAction("Movies");
    }


    // ======================
    // USER / GUEST
    // ======================

    // Movie listing for users
    public IActionResult Index(int page = 1)
    {
        int pageSize = 8; // movies per page

        var totalMovies = db.Movies.Count();
        var movies = db.Movies
            .OrderBy(m => m.PremierDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var vm = new MovieListVM
        {
            Movies = movies,
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = totalMovies
        };

        return View(vm);
    }


    public IActionResult Details(int id)
    {
        var movie = db.Movies.FirstOrDefault(m => m.MovieId == id);

        if (movie == null)
            return NotFound();

        return View(movie);
    }

}
