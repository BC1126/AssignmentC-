using AssignmentC_.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssignmentC_.Controllers;
public class ShowTimeController : Controller
{
    private readonly DB db;
    public ShowTimeController(DB db)
    {
        this.db = db;
    }

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
        db.ShowTimes.Add(new ShowTime
        {
            MovieId = vm.MovieId,
            HallId = vm.HallId,
            StartTime = newStart,
            TicketPrice = vm.TicketPrice,
            IsActive = true
        });


        db.SaveChanges();

        TempData["Info"] = "Showtime added successfully!";
        return RedirectToAction("Manage", new { vm.OutletId, vm.HallId, date = vm.Date.ToString("yyyy-MM-dd") });
    }

    // GET: ShowTimeManage
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
        vm.ExistingShowTimes = db.ShowTimes
            .Include(st => st.Movie)
            .Include(st => st.Hall)
            .Where(st => st.IsActive)
            .OrderBy(st => st.StartTime)
            .ToList();

        return View(vm);
    }



    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteShowTime(int id)
    {
        var st = db.ShowTimes
            .Include(s => s.Hall)
            .FirstOrDefault(s => s.ShowTimeId == id);
        if (st == null)
        {
            TempData["Error"] = "Showtime not found!";
            return RedirectToAction("ShowTimeManage");
        }

        db.ShowTimes.Remove(st);
        db.SaveChanges();

        TempData["Info"] = "Showtime deleted successfully!";
        return RedirectToAction("ShowTimeManage",
            new { outletId = st.Hall.OutletId, hallId = st.HallId, date = st.StartTime.ToString("yyyy-MM-dd") });
    }


    public IActionResult FilterShowTimes(int? outletId, int? hallId, DateTime? date)
    {
        var query = db.ShowTimes
            .Include(st => st.Movie)
            .Include(st => st.Hall)
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

        // Update fields
        st.MovieId = vm.MovieId;
        st.HallId = vm.HallId;
        st.StartTime = newStart;
        st.TicketPrice = vm.TicketPrice;

        db.SaveChanges();

        TempData["Info"] = "Showtime updated successfully!";
        return RedirectToAction("ShowTimeManage");
    }

}
