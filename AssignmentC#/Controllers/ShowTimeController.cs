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
            vm.Halls = db.Halls
                .Where(h => h.OutletId == outletId)
                .ToList();
        }

        if (hallId.HasValue)
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

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Staff")]
    public IActionResult CreateShowTime(ShowTimeManageVM vm)
    {
        var movie = db.Movies.Find(vm.MovieId);
        if (movie == null)
            return NotFound();

        var newStart = vm.Date.Date.Add(vm.StartTime.TimeOfDay);
        var newEnd = newStart.AddMinutes(movie.DurationMinutes);

        // 🔥 CONFLICT CHECK
        var conflict = db.ShowTimes
            .Include(st => st.Movie)
            .Any(st =>
                st.HallId == vm.HallId &&
                st.IsActive &&
                newStart < st.StartTime.AddMinutes(st.Movie.DurationMinutes) &&
                newEnd > st.StartTime
            );

        if (conflict)
        {
            TempData["Info"] = "Time conflict detected with existing showtime.";
            return RedirectToAction("Manage",
                new { vm.OutletId, vm.HallId, date = vm.Date });
        }

        // ✅ Save
        var newShowTime = new ShowTime
        {
            MovieId = vm.MovieId,
            HallId = vm.HallId,
            StartTime = newStart,
            TicketPrice = vm.TicketPrice,
            IsActive = true
        };

        db.ShowTimes.Add(newShowTime);
        db.SaveChanges();

        // ===========================
        // 🔹 LOG ACTION
        // ===========================
        // Use your injected Helper (hp)
        hp.LogAction("ShowTime", $"Created showtime for movie {movie.Title} at hall {vm.HallId}");

        TempData["Info"] = "Showtime added successfully!";
        return RedirectToAction("Manage", new { vm.OutletId, vm.HallId, date = vm.Date.ToString("yyyy-MM-dd") });
    }


    // GET: ShowTimeManage
    [Authorize(Roles = "Admin,Staff")]
    public IActionResult ShowTimeManage(int? outletId, int? hallId, DateTime? date)
    {
        var vm = new ShowTimeManageVM
        {
            Date = date ?? DateTime.Today,
            Outlets = db.Outlets.ToList(),
            Halls = outletId.HasValue ? db.Halls.Where(h => h.OutletId == outletId.Value).ToList() : new List<Hall>(),
            OutletId = outletId ?? 0,
            HallId = hallId ?? 0,
        };

        // Fetch all active showtimes initially (no filters)
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

if (date.HasValue)
    query = query.Where(st => st.StartTime.Date == date.Value.Date);

vm.ExistingShowTimes = query
    .OrderBy(st => st.StartTime)
    .ToList();


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
    public IActionResult FilterShowTimes(int? outletId, int? hallId, DateTime? date)
    {
        var query = db.ShowTimes
            .Include(st => st.Movie)
            .Include(st => st.Hall)
            .ThenInclude(h=>h.Outlet)
            .Where(st => st.IsActive)
            .AsQueryable();

        if (outletId.HasValue)
            query = query.Where(st => st.Hall.OutletId == outletId.Value);

        if (hallId.HasValue && hallId > 0)
            query = query.Where(st => st.HallId == hallId.Value);

        if (date.HasValue)
            query = query.Where(st => st.StartTime.Date == date.Value.Date);

        var result = query.OrderBy(st => st.StartTime).ToList();

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

        var newStart = vm.Date.Date.Add(vm.StartTime.TimeOfDay);
        var newEnd = newStart.AddMinutes(db.Movies.Find(vm.MovieId)?.DurationMinutes ?? 0);

        // Conflict check (excluding current showtime)
        var conflict = db.ShowTimes
            .Include(s => s.Movie)
            .Any(s =>
                s.ShowTimeId != vm.ShowTimeId &&
                s.HallId == vm.HallId &&
                s.IsActive &&
                newStart < s.StartTime.AddMinutes(s.Movie.DurationMinutes) &&
                newEnd > s.StartTime
            );

        if (conflict)
        {
            TempData["Error"] = "Time conflict detected with existing showtime.";
            return RedirectToAction("ShowTimeManage");
        }
        // -------------------------------
        // Track changes with new line formatting
        // -------------------------------
        var changeLogLines = new List<string>();

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

}
