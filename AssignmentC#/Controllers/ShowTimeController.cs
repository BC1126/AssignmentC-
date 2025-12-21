using AssignmentC_.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssignmentC_.Controllers;
public class ShowTimeController : Controller
{
    private readonly DB db;
    private readonly Helper hp;

    public ShowTimeController(DB db, Helper hp)
    {
        this.db = db;
        this.hp = hp;
    }

    [Authorize(Roles = "Admin,Staff")]
    [Authorize(Roles = "Admin,Staff")]
    public IActionResult Manage(int? outletId, int? hallId, DateTime? date)
    {
        var vm = new ShowTimeManageVM
        {
            Date = date ?? DateTime.Today,
            Outlets = db.Outlets.ToList(),
            Movies = db.Movies
             .Where(m => date == null || m.PremierDate.Date <= date.Value.Date)
             .OrderBy(m => m.Title)
             .ToList()
        };

        if (outletId.HasValue)
        {
            vm.OutletId = outletId.Value;

            // ✅ FIX 1: Only load ACTIVE halls into the dropdown list
            vm.Halls = db.Halls
                .Where(h => h.OutletId == outletId && h.IsActive)
                .ToList();
        }

        if (hallId.HasValue)
        {
            // Optional: Double check if the selected hallId is actually active. 
            // If not, we ignore it to prevent showing data for inactive halls.
            var selectedHall = db.Halls.FirstOrDefault(h => h.HallId == hallId.Value && h.IsActive);

            if (selectedHall != null)
            {
                vm.HallId = hallId.Value;
                vm.ExistingShowTimes = db.ShowTimes
                    .Include(st => st.Movie)
                    .Where(st =>
                        st.HallId == hallId &&
                        st.StartTime.Date == vm.Date.Date &&
                        st.IsActive)
                    .OrderBy(st => st.StartTime)
                    .ToList();
            }
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Staff")]
    public IActionResult CreateShowTime(ShowTimeManageVM vm)
    {
        // 1. Basic Validation
        var movie = db.Movies.Find(vm.MovieId);
        if (movie == null) return NotFound();

        var targetHall = db.Halls.FirstOrDefault(h => h.HallId == vm.HallId && h.IsActive);
        if (targetHall == null)
        {
            TempData["Error"] = "Hall invalid.";
            return RedirectToAction("Manage", new { vm.OutletId, date = vm.Date });
        }

        int successCount = 0;
        int failCount = 0;
        int cleaningBuffer = 15; // Keep your smart scheduling rule!

        // 2. LOOP: Iterate from 0 (Today) up to RepeatDays
        for (int i = 0; i <= vm.RepeatDays; i++)
        {
            // Calculate date and time for THIS iteration
            DateTime currentDay = vm.Date.Date.AddDays(i);
            DateTime start = currentDay.Add(vm.StartTime.TimeOfDay);
            DateTime end = start.AddMinutes(movie.DurationMinutes);

            // 3. Conflict Check for THIS specific day
            bool conflict = db.ShowTimes
                .Include(st => st.Movie)
                .Any(st =>
                    st.HallId == vm.HallId &&
                    st.IsActive &&
                    start < st.StartTime.AddMinutes(st.Movie.DurationMinutes + cleaningBuffer) &&
                    end.AddMinutes(cleaningBuffer) > st.StartTime
                );

            if (conflict)
            {
                // If conflict, SKIP this day and count failure
                failCount++;
                continue;
            }

            // 4. Create and Add (Queue it)
            var newShowTime = new ShowTime
            {
                MovieId = vm.MovieId,
                HallId = vm.HallId,
                StartTime = start,
                TicketPrice = vm.TicketPrice,
                IsActive = true
            };

            db.ShowTimes.Add(newShowTime);
            hp.LogAction("ShowTime", $"Created showtime for {movie.Title} on {start:yyyy-MM-dd}");
            successCount++;
        }

        // 5. Save all changes at once
        db.SaveChanges();

        // 6. Smart Feedback Message
        if (failCount == 0)
        {
            TempData["Info"] = $"Success! Added showtimes for {successCount} days.";
        }
        else if (successCount > 0)
        {
            TempData["Info"] = $"Partial Success. Added {successCount} showtimes. Skipped {failCount} days due to conflicts.";
        }
        else
        {
            TempData["Error"] = "Failed. All selected days had conflicts.";
        }

        return RedirectToAction("Manage", new { vm.OutletId, vm.HallId, date = vm.Date.ToString("yyyy-MM-dd") });
    }


    // GET: ShowTimeManage
    [Authorize(Roles = "Admin,Staff")]
    public IActionResult ShowTimeManage(int? outletId, int? hallId, int? movieId, DateTime? date, string sort = "Date", string dir = "asc", int page = 1)
    {
        var selectedDate = date ?? DateTime.Today;
        int pageSize = 15;

        var vm = new ShowTimeManageVM
        {
            Date = selectedDate,
            Outlets = db.Outlets.ToList(),
            Movies = db.Movies.OrderBy(m => m.Title).ToList(),
            Halls = outletId.HasValue ? db.Halls.Where(h => h.OutletId == outletId.Value).ToList() : new List<Hall>(),
            OutletId = outletId ?? 0,
            HallId = hallId ?? 0
        };

        var query = db.ShowTimes
            .Include(st => st.Movie)
            .Include(st => st.Hall)
            .ThenInclude(h => h.Outlet)
            .Where(st => st.IsActive)
            .AsQueryable();

        // --- Filters ---
        if (outletId.HasValue) query = query.Where(st => st.Hall.OutletId == outletId.Value);
        if (hallId.HasValue && hallId > 0) query = query.Where(st => st.HallId == hallId.Value);
        if (movieId.HasValue && movieId > 0) query = query.Where(st => st.MovieId == movieId.Value);
        query = query.Where(st => st.StartTime.Date == selectedDate.Date);

        // --- 🔥 Sorting Logic (Same pattern as Movie) ---
        query = sort switch
        {
            "Movie" => dir == "des" ? query.OrderByDescending(st => st.Movie.Title) : query.OrderBy(st => st.Movie.Title),
            "Outlet" => dir == "des" ? query.OrderByDescending(st => st.Hall.Outlet.Name) : query.OrderBy(st => st.Hall.Outlet.Name),
            "Hall" => dir == "des" ? query.OrderByDescending(st => st.Hall.Name) : query.OrderBy(st => st.Hall.Name),
            "Price" => dir == "des" ? query.OrderByDescending(st => st.TicketPrice) : query.OrderBy(st => st.TicketPrice),
            // Default: Sort by Date/Time
            _ => dir == "des" ? query.OrderByDescending(st => st.StartTime) : query.OrderBy(st => st.StartTime)
        };

        // --- Pagination ---
        int totalItems = query.Count();
        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        vm.ExistingShowTimes = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // --- ViewBags for View ---
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.SelectedMovieId = movieId;

        // 🔥 Pass Sort Data
        ViewBag.Sort = sort;
        ViewBag.Dir = dir;

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Staff")]
    public IActionResult DeleteShowTime(int id)
    {
        var st = db.ShowTimes.FirstOrDefault(s => s.ShowTimeId == id);
        if (st == null)
            return Json(new { success = false, message = "Showtime not found." });

        db.ShowTimes.Remove(st);
        db.SaveChanges();
        hp.LogAction("ShowTime", $"Delete ShowTimeId={id}");

        return Json(new { success = true, message = "Showtime deleted successfully!" });
    }



    [Authorize(Roles = "Admin,Staff")]
    public IActionResult FilterShowTimes(int? outletId, int? hallId, int? movieId, DateTime? date, string sort = "Date", string dir = "asc", int page = 1)
    {
        int pageSize = 15;

        var query = db.ShowTimes
            .Include(st => st.Movie)
            .Include(st => st.Hall)
            .ThenInclude(h => h.Outlet)
            .Where(st => st.IsActive)
            .AsQueryable();

        // --- Filters ---
        if (outletId.HasValue) query = query.Where(st => st.Hall.OutletId == outletId.Value);
        if (hallId.HasValue && hallId > 0) query = query.Where(st => st.HallId == hallId.Value);
        if (movieId.HasValue && movieId > 0) query = query.Where(st => st.MovieId == movieId.Value);
        if (date.HasValue) query = query.Where(st => st.StartTime.Date == date.Value.Date);

        // --- 🔥 Sorting Logic ---
        query = sort switch
        {
            "Movie" => dir == "des" ? query.OrderByDescending(st => st.Movie.Title) : query.OrderBy(st => st.Movie.Title),
            "Outlet" => dir == "des" ? query.OrderByDescending(st => st.Hall.Outlet.Name) : query.OrderBy(st => st.Hall.Outlet.Name),
            "Hall" => dir == "des" ? query.OrderByDescending(st => st.Hall.Name) : query.OrderBy(st => st.Hall.Name),
            "Price" => dir == "des" ? query.OrderByDescending(st => st.TicketPrice) : query.OrderBy(st => st.TicketPrice),
            _ => dir == "des" ? query.OrderByDescending(st => st.StartTime) : query.OrderBy(st => st.StartTime)
        };

        // --- Pagination ---
        int totalItems = query.Count();
        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var result = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;

        // 🔥 Pass Sort Data
        ViewBag.Sort = sort;
        ViewBag.Dir = dir;

        return PartialView("_ShowTimeTable", result);
    }



    [Authorize(Roles = "Admin,Staff")]
    public IActionResult GetHalls(int outletId)
    {
        var halls = db.Halls
            .Where(h => h.OutletId == outletId)
            .Select(h => new {
                h.HallId,
                h.Name
            })
            .ToList();

        return Json(halls);
    }

    // GET: ShowTime/Edit/5
    [Authorize(Roles = "Admin,Staff")]
    public IActionResult EditShowTime(int id)
    {
        var st = db.ShowTimes
            .Include(s => s.Movie)
            .Include(s => s.Hall)
            .FirstOrDefault(s => s.ShowTimeId == id);

        if (st == null)
            return NotFound();

        var vm = new ShowTimeManageVM
        {
            ShowTimeId = st.ShowTimeId,
            MovieId = st.MovieId,
            HallId = st.HallId,
            OutletId = st.Hall.OutletId,
            Date = st.StartTime.Date,
            StartTime = st.StartTime,
            TicketPrice = st.TicketPrice,
            Outlets = db.Outlets.ToList(),
            Halls = db.Halls.Where(h => h.OutletId == st.Hall.OutletId).ToList(),
            Movies = db.Movies.OrderBy(m => m.Title).ToList()
        };

        return View(vm);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Staff")]
    public IActionResult EditShowTime(ShowTimeManageVM vm)
    {
        var st = db.ShowTimes
            .Include(s => s.Movie)
            .Include(s => s.Hall)
            .FirstOrDefault(s => s.ShowTimeId == vm.ShowTimeId);

        if (st == null)
            return NotFound();

        // 1. Get the duration of the movie being assigned (it might be a new movie or the same one)
        var movieDuration = db.Movies.Find(vm.MovieId)?.DurationMinutes ?? 0;

        var newStart = vm.Date.Date.Add(vm.StartTime.TimeOfDay);
        var newEnd = newStart.AddMinutes(movieDuration);

        // ==========================================
        // 🔥 CONFLICT CHECK (With Cleaning Gap)
        // ==========================================
        int cleaningBuffer = 15; // 15 minutes for cleaning

        var conflict = db.ShowTimes
            .Include(s => s.Movie)
            .Any(s =>
                s.ShowTimeId != vm.ShowTimeId && // Exclude the showtime currently being edited
                s.HallId == vm.HallId &&
                s.IsActive &&

                // 1. The NEW movie must not start while an OLD movie (plus cleaning) is running
                newStart < s.StartTime.AddMinutes(s.Movie.DurationMinutes + cleaningBuffer) &&

                // 2. The NEW movie (plus cleaning) must not run into an OLD movie's start time
                newEnd.AddMinutes(cleaningBuffer) > s.StartTime
            );

        if (conflict)
        {
            ModelState.Clear();
            ModelState.AddModelError("",
                "Time conflict detected! Ensure there is at least a 15-minute cleaning gap.");

            vm.Outlets = db.Outlets.ToList();
            vm.Halls = db.Halls
                .Where(h => h.OutletId == vm.OutletId)
                .ToList();
            vm.Movies = db.Movies.ToList();

            return View(vm);
        }



        // -------------------------------
        // Track changes with new line formatting
        // -------------------------------
        var changeLogLines = new List<string>();

        // Note: We compare st.MovieId (old) with vm.MovieId (new)
        if (st.MovieId != vm.MovieId)
            changeLogLines.Add($"Movie: {st.Movie?.Title} → {db.Movies.Find(vm.MovieId)?.Title}");

        if (st.HallId != vm.HallId)
            changeLogLines.Add($"Hall: {st.Hall?.Name} → {db.Halls.Find(vm.HallId)?.Name}");

        if (st.StartTime != newStart)
            changeLogLines.Add($"StartTime: {st.StartTime} → {newStart}");

        if (st.TicketPrice != vm.TicketPrice)
            changeLogLines.Add($"TicketPrice: {st.TicketPrice} → {vm.TicketPrice}");

        // -------------------------------
        // Update fields
        // -------------------------------
        st.MovieId = vm.MovieId;
        st.HallId = vm.HallId;
        st.StartTime = newStart;
        st.TicketPrice = vm.TicketPrice;

        db.SaveChanges();

        // -------------------------------
        // Log changes
        // -------------------------------
        string changeLog = changeLogLines.Count > 0
            ? string.Join("\n", changeLogLines)
            : "No changes made";

        hp.LogAction("ShowTime", $"Edit ShowTimeId={vm.ShowTimeId}\nChanges:\n{changeLog}");

        TempData["Info"] = "Showtime updated successfully!";
        return RedirectToAction("ShowTimeManage");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Staff")]
    public IActionResult BatchDelete(
    List<int> selectedIds,
    int? outletId,
    int? hallId,
    DateTime? date)
    {
        if (selectedIds == null || !selectedIds.Any())
        {
            TempData["Error"] = "No showtimes selected.";
            return RedirectToAction("ShowTimeManage");
        }

        var showtimes = db.ShowTimes
            .Where(st => selectedIds.Contains(st.ShowTimeId))
            .ToList();

        if (!showtimes.Any())
        {
            TempData["Error"] = "Selected showtimes not found.";
            return RedirectToAction("ShowTimeManage");
        }

        db.ShowTimes.RemoveRange(showtimes);
        db.SaveChanges();

        foreach (var st in showtimes) hp.LogAction("ShowTime", $"BatchDelete ShowTimeId={st.ShowTimeId}");

        TempData["Info"] = $"{showtimes.Count} showtime(s) deleted successfully.";

        return RedirectToAction("ShowTimeManage", new
        {
            outletId,
            hallId,
            date = date?.ToString("yyyy-MM-dd")
        });
    }

    [Authorize(Roles = "Admin,Staff")]
    public IActionResult ExportShowTimes(int? outletId, int? hallId, int? movieId, DateTime? date)
    {
        // ---------------------------------------------------------
        // 1. QUERY & FILTERING
        // ---------------------------------------------------------
        var query = db.ShowTimes
            .Include(st => st.Movie)
            .Include(st => st.Hall)
            .ThenInclude(h => h.Outlet)
            .Where(st => st.IsActive)
            .AsQueryable();

        if (outletId.HasValue)
            query = query.Where(st => st.Hall.OutletId == outletId.Value);

        if (hallId.HasValue && hallId > 0)
            query = query.Where(st => st.HallId == hallId.Value);

        if (movieId.HasValue && movieId > 0)
            query = query.Where(st => st.MovieId == movieId.Value);

        if (date.HasValue)
            query = query.Where(st => st.StartTime.Date == date.Value.Date);

        // ---------------------------------------------------------
        // 2. FETCH & SORT (In Memory)
        // ---------------------------------------------------------
        // We fetch first, then sort by Hall + Time so we can compare adjacent rows
        var rawData = query.ToList();

        var data = rawData
            .OrderBy(st => st.Hall.Outlet.Name)
            .ThenBy(st => st.Hall.Name)
            .ThenBy(st => st.StartTime)
            .ToList();

        // ---------------------------------------------------------
        // 3. BUILD CSV CONTENT
        // ---------------------------------------------------------
        var sb = new System.Text.StringBuilder();

        // Header Row
        sb.AppendLine("Date,Outlet,Hall,Movie Just Ended,Cleaning Starts (End Time),Next Show Starts,Gap/Deadline");

        for (int i = 0; i < data.Count; i++)
        {
            var item = data[i];

            // --- CALCULATION LOGIC ---
            string nextShowTime = "End of Day";
            string gap = "Until Closing"; // Default for last movie

            // Look ahead to the next record
            if (i + 1 < data.Count)
            {
                var nextItem = data[i + 1];

                // Check if the next record is in the SAME Hall on the SAME Day
                if (nextItem.HallId == item.HallId && nextItem.StartTime.Date == item.StartTime.Date)
                {
                    nextShowTime = nextItem.StartTime.ToString("hh:mm tt");

                    // Calculate minutes between Current End and Next Start
                    double minutes = (nextItem.StartTime - item.EndTime).TotalMinutes;

                    if (minutes < 0)
                    {
                        gap = $"OVERRUN! ({minutes:0} mins)"; // Negative gap means schedule clash
                    }
                    else
                    {
                        gap = $"{minutes:0} mins";
                    }
                }
            }

            // Escape quotes in movie title for CSV safety
            string movieTitle = $"\"{item.Movie.Title.Replace("\"", "\"\"")}\"";

            // --- APPEND ROW ---
            sb.AppendLine($"{item.StartTime:dd/MM/yyyy}," +
                          $"{item.Hall.Outlet.Name}," +
                          $"{item.Hall.Name}," +
                          $"{movieTitle}," +
                          $"{item.EndTime:hh:mm tt}," +   // When cleaning begins
                          $"{nextShowTime}," +            // When cleaning must finish
                          $"{gap}");
        }

        // ---------------------------------------------------------
        // 4. RETURN FILE
        // ---------------------------------------------------------
        string fileName = $"Cleaning_Schedule_{DateTime.Now:yyyyMMdd_HHmm}.csv";
        return File(System.Text.Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", fileName);
    }
}
