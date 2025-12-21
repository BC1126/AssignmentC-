using Microsoft.AspNetCore.Authorization;
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


    [Authorize(Roles = "Admin,Staff")]
    public IActionResult Movies(string name, string sort = "Title", string dir = "asc", int page = 1)
    {
        ViewBag.Sort = sort;
        ViewBag.Dir = dir;
        ViewBag.Name = name; 

        IQueryable<Movie> query = db.Movies;

        if (!string.IsNullOrEmpty(name))
        {
            query = query.Where(m => m.Title.Contains(name) || m.Genre.Contains(name));
        }

        query = sort switch
        {
            "Genre" => dir == "des" ? query.OrderByDescending(m => m.Genre) : query.OrderBy(m => m.Genre),
            "Rating" => dir == "des" ? query.OrderByDescending(m => m.Rating) : query.OrderBy(m => m.Rating),
            "PremierDate" => dir == "des" ? query.OrderByDescending(m => m.PremierDate) : query.OrderBy(m => m.PremierDate),
            "Duration" => dir == "des" ? query.OrderByDescending(m => m.DurationMinutes) : query.OrderBy(m => m.DurationMinutes),
            _ => dir == "des" ? query.OrderByDescending(m => m.Title) : query.OrderBy(m => m.Title),
        };

        int pageSize = 10;
        int totalCount = query.Count();
        var movieList = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var vm = new MovieListVM
        {
            Movies = movieList,
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Sort = sort,
            Dir = dir,
            SearchName = name
        };

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView("_MovieTable", vm);
        }

        return View(vm);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Staff")]
    public IActionResult UpsertMovie(int? id)
        {
            if (id == null || id == 0)
            {
                return View(new MovieViewModel());
            }

            var movie = db.Movies.FirstOrDefault(m => m.MovieId == id);
            if (movie == null)
            {
                TempData["Error"] = "Movie not found.";
                return RedirectToAction("Movies");
            }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Staff")]
    public IActionResult UpsertMovie(MovieViewModel vm)
    {

        ModelState.Remove("PosterUrl");
        ModelState.Remove("BannerUrl");

        if (vm.MovieId == 0)
        {
            if (vm.PosterFile == null)
                ModelState.AddModelError("PosterFile", "Poster image is required.");
            if (vm.BannerFile == null)
                ModelState.AddModelError("BannerFile", "Banner image is required.");
        }

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
        var changeLogLines = new List<string>();

        if (vm.MovieId == 0)
        {
  
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

            movie = db.Movies.FirstOrDefault(m => m.MovieId == vm.MovieId);
            if (movie == null)
                return NotFound();

            if (vm.PosterFile != null)
            {
                if (!string.IsNullOrEmpty(movie.PosterUrl))
                    hp.DeletePhoto(movie.PosterUrl, "uploads");

                movie.PosterUrl = hp.SavePhotoNoResize(vm.PosterFile, "uploads");
                changeLogLines.Add("Poster updated");
            }

            if (vm.BannerFile != null)
            {
                if (!string.IsNullOrEmpty(movie.BannerUrl))
                    hp.DeletePhoto(movie.BannerUrl, "uploads");

                movie.BannerUrl = hp.SavePhotoNoResize(vm.BannerFile, "uploads");
                changeLogLines.Add("Banner updated");
            }
        }


        if (movie.Title != vm.Title)
            changeLogLines.Add($"Title: {movie.Title} → {vm.Title}");
        movie.Title = vm.Title;

        if (movie.Description != vm.Description)
            changeLogLines.Add($"Description: {movie.Description} → {vm.Description}");
        movie.Description = vm.Description;

        if (movie.Genre != vm.Genre)
            changeLogLines.Add($"Genre: {movie.Genre} → {vm.Genre}");
        movie.Genre = vm.Genre;

        if (movie.DurationMinutes != vm.DurationMinutes)
            changeLogLines.Add($"DurationMinutes: {movie.DurationMinutes} → {vm.DurationMinutes}");
        movie.DurationMinutes = vm.DurationMinutes;

        if (movie.Rating != vm.Rating)
            changeLogLines.Add($"Rating: {movie.Rating} → {vm.Rating}");
        movie.Rating = vm.Rating;

        if (movie.Director != vm.Director)
            changeLogLines.Add($"Director: {movie.Director} → {vm.Director}");
        movie.Director = vm.Director;

        if (movie.Writer != vm.Writer)
            changeLogLines.Add($"Writer: {movie.Writer} → {vm.Writer}");
        movie.Writer = vm.Writer;

        if (movie.PremierDate != vm.PremierDate)
            changeLogLines.Add($"PremierDate: {movie.PremierDate} → {vm.PremierDate}");
        movie.PremierDate = vm.PremierDate.Value;

        if (movie.TrailerUrl != vm.TrailerUrl)
            changeLogLines.Add($"TrailerUrl: {movie.TrailerUrl} → {vm.TrailerUrl}");
        movie.TrailerUrl = vm.TrailerUrl;

        db.SaveChanges();


        string changeLog = changeLogLines.Count > 0
            ? string.Join("\n", changeLogLines)
            : "No changes made";

        if (vm.MovieId == 0)
        {
            hp.LogAction("Movie", $"Create Movie Name = {movie.Title}");
        }
        else
        {
            hp.LogAction("Movie", $"Edit Movie Name = {movie.Title}\nChanges:\n{changeLog}");
            TempData["Info"] = "Movie updated successfully!";
        }

        return RedirectToAction("Movies");
    }


    [Authorize(Roles = "Admin,Staff")]
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
    [Authorize(Roles = "Admin,Staff")]
    public IActionResult DeleteConfirmed(int movieId)
    {
        var movie = db.Movies
            .Include(m => m.ShowTimes)
            .FirstOrDefault(m => m.MovieId == movieId);

        if (movie == null)
        {
            return NotFound();
        }

        if (!string.IsNullOrEmpty(movie.PosterUrl))
        {
            var posterPath = Path.Combine(_env.WebRootPath, "uploads", movie.PosterUrl);
            if (System.IO.File.Exists(posterPath))
            {
                System.IO.File.Delete(posterPath);
            }
        }

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
        hp.LogAction("Movie", $"Delete Movie Name ={movie.Title}");

        TempData["Info"] = "Movie deleted successfully.";
        return RedirectToAction("Movies");
    }

    public IActionResult Index(string name, int page = 1)
    {
        int pageSize = 8;

        var query = db.Movies.AsQueryable();

        if (!string.IsNullOrEmpty(name))
        {
            name = name.Trim();
            query = query.Where(m => m.Title.Contains(name) || m.Genre.Contains(name));
        }

        var totalMovies = query.Count();
        var movies = query.OrderBy(m => m.PremierDate)
                          .Skip((page - 1) * pageSize)
                          .Take(pageSize)
                          .ToList();

        var vm = new MovieListVM
        {
            Movies = movies,
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = totalMovies,
            SearchName = name 
        };

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView("_UserMovieGrid", vm);
        }

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
