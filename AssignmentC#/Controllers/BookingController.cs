using System.Security.Claims;
using System.Text.Json;
using AssignmentC_.Hubs;
using AssignmentC_.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AssignmentC_.Controllers;

public class BookingController : Controller
{
    private readonly DB db;
    private readonly IHubContext<SeatHub> hub;

    public BookingController(DB db, IHubContext<SeatHub> hub)
    {
        this.db = db;
        this.hub = hub;
    }

    [HttpGet]
    public IActionResult SelectTicket(int showtimeId)
    {
        if (string.IsNullOrEmpty(HttpContext.Session.GetString("SessionStarted")))
        {
            HttpContext.Session.SetString("SessionStarted", DateTime.Now.ToString());
        }

        var sessionId = HttpContext.Session.Id;
        var showtime = db.ShowTimes
            .Include(st => st.Movie)
            .Include(st => st.Hall)
                .ThenInclude(h => h.Outlet)
            .Include(st => st.Hall)
                .ThenInclude(h => h.Seats)
            .Include(st => st.Bookings)
                .ThenInclude(b => b.BookingSeats)
            .FirstOrDefault(st => st.ShowTimeId == showtimeId);

        if (showtime == null)
        {
            TempData["Error"] = "Showtime not found";
            return RedirectToAction("Index", "Movie");
        }

        var now = DateTime.UtcNow;

        var bookedSeatIds = showtime.Bookings
            .SelectMany(b => b.BookingSeats)
            .Select(bs => bs.SeatId)
            .ToHashSet();

        var expiredLocks = db.SeatLocks
            .Where(sl => sl.ShowTimeId == showtimeId && sl.ExpiresAt < now)
            .ToList();
        db.SeatLocks.RemoveRange(expiredLocks);
        db.SaveChanges();

        // 3. Get seats locked by OTHER people (Grey)
        var lockedByOthersIds = db.SeatLocks
            .Where(sl => sl.ShowTimeId == showtimeId
                      && sl.ExpiresAt > now
                      && sl.SessionId != sessionId)
            .Select(sl => sl.SeatId)
            .ToHashSet();

        var myLockedSeatIds = db.SeatLocks
        .Where(sl => sl.ShowTimeId == showtimeId
                  && sl.ExpiresAt > now
                  && sl.SessionId == sessionId)
        .Select(sl => sl.SeatId)
        .ToHashSet();

        var vm = new SelectTicketViewModel
        {
            ShowTimeId = showtime.ShowTimeId,
            MovieId = showtime.MovieId,
            MovieTitle = showtime.Movie.Title,
            MoviePosterUrl = showtime.Movie.PosterUrl,
            StartTime = showtime.StartTime,
            HallName = showtime.Hall.Name,
            OutletName = showtime.Hall.Outlet.Name,
            TicketPrice = showtime.TicketPrice,
            ChildrenPrice = showtime.TicketPrice * 0.8m,
            SeniorPrice = showtime.TicketPrice * 0.85m,
            OkuPrice = showtime.TicketPrice * 0.9m,
            SessionId = sessionId,
            LockDurationMinutes = 5,
            Seats = showtime.Hall.Seats
                .Where(s => s.IsActive)
                .OrderBy(s => s.SeatIdentifier)
                .Select(s => new SeatSelectionViewModel
                {
                    SeatId = s.SeatId,
                    SeatIdentifier = s.SeatIdentifier,
                    IsPremium = s.IsPremium,
                    IsWheelchair = s.IsWheelchair,
                    IsOccupied = bookedSeatIds.Contains(s.SeatId),
                    IsLocked = lockedByOthersIds.Contains(s.SeatId),
                    IsSelected = myLockedSeatIds.Contains(s.SeatId),
                    Row = new string(s.SeatIdentifier.TakeWhile(char.IsLetter).ToArray()),
                    Column = int.Parse(new string(s.SeatIdentifier.SkipWhile(char.IsLetter).ToArray()))
                })
                .ToList()
        };

        return View(vm);
    }

    [HttpPost]
    public IActionResult CalculateTicketPrice([FromBody] TicketCalculationRequest request)
    {
        var showtime = db.ShowTimes.Include(st => st.Hall).ThenInclude(h => h.Seats)
                                 .FirstOrDefault(st => st.ShowTimeId == request.ShowTimeId);

        if (showtime == null || request.SelectedSeatIds == null) return BadRequest();

        var selectedSeats = showtime.Hall.Seats
            .Where(s => request.SelectedSeatIds.Contains(s.SeatId)).ToList();

        decimal total = 0;
        int remChild = request.ChildrenCount;
        int remAdult = request.AdultCount;
        int remSenior = request.SeniorCount;
        int remOku = request.OkuCount;

        foreach (var seat in selectedSeats)
        {
            decimal seatPrice = showtime.TicketPrice;

            if (remChild > 0) { seatPrice *= 0.80m; remChild--; }
            else if (remSenior > 0) { seatPrice *= 0.85m; remSenior--; }
            else if (remOku > 0) { seatPrice *= 0.90m; remOku--; }
            else { remAdult--; }

            if (seat.IsPremium) { seatPrice *= 1.20m; }

            total += seatPrice;
        }

        return Ok(new { success = true, subtotal = total.ToString("F2"), isValid = true });
    }

    // AJAX: Validate seat availability in real-time
    [HttpPost]
    public IActionResult ValidateSeats([FromBody] SeatValidationRequest request)
    {
        if (request.ShowTimeId <= 0 || request.SeatIds == null || !request.SeatIds.Any())
        {
            return BadRequest(new { success = false, message = "Invalid request" });
        }

        // Check if seats are still available
        var bookedSeatIds = db.BookingSeats
            .Where(bs => bs.Booking.ShowTimeId == request.ShowTimeId)
            .Select(bs => bs.SeatId)
            .ToHashSet();

        var conflictingSeats = request.SeatIds.Intersect(bookedSeatIds).ToList();

        if (conflictingSeats.Any())
        {
            var seatIdentifiers = db.Seats
                .Where(s => conflictingSeats.Contains(s.SeatId))
                .Select(s => s.SeatIdentifier)
                .ToList();

            return Ok(new
            {
                success = false,
                isValid = false,
                message = $"Seats {string.Join(", ", seatIdentifiers)} are no longer available",
                conflictingSeats = conflictingSeats
            });
        }

        return Ok(new
        {
            success = true,
            isValid = true,
            message = "All seats are available"
        });
    }

    [HttpPost]
    public IActionResult SelectTicket([FromForm] TicketSelectionSubmission submission)
    {
        var uniqueSeatIds = submission.SeatIds?.Distinct().ToList() ?? new List<int>();
        var email = User.Identity!.Name!;
        var user = db.Users.FirstOrDefault(u => u.Email == email);

        if (submission.ShowTimeId <= 0 || !uniqueSeatIds.Any())
        {
            TempData["Error"] = "Please select at least one seat";
            return RedirectToAction("SelectTicket", new { showtimeId = submission.ShowTimeId });
        }
        

        if (user == null)
        {
            return RedirectToAction("Login", "User");
        }

        var showtime = db.ShowTimes
            .Include(st => st.Movie)
            .Include(st => st.Hall).ThenInclude(h => h.Outlet)
            .Include(st => st.Hall).ThenInclude(h => h.Seats)
            .FirstOrDefault(st => st.ShowTimeId == submission.ShowTimeId);

        if (showtime == null)
        {
            TempData["Error"] = "Showtime not found";
            return RedirectToAction("Index", "Movie");
        }

        var seats = showtime.Hall.Seats
            .Where(s => uniqueSeatIds.Contains(s.SeatId))
            .ToList();

        if (seats.Count != uniqueSeatIds.Count)
        {
            TempData["Error"] = "Some selected seats are invalid for this hall.";
            return RedirectToAction("SelectTicket", new { showtimeId = submission.ShowTimeId });
        }

        decimal subtotal = 0;
        int tempChildren = submission.ChildrenCount;
        int tempSenior = submission.SeniorCount;
        int tempOku = submission.OkuCount;
        var sessionId = HttpContext.Session.Id;
        var currentLock = db.SeatLocks
            .Where(s => s.SessionId == sessionId && s.ShowTimeId == submission.ShowTimeId)
            .OrderByDescending(s => s.ExpiresAt)
            .FirstOrDefault();
        foreach (var seat in seats)
        {
            decimal price = showtime.TicketPrice;

            if (tempChildren > 0) { price *= 0.80m; tempChildren--; }
            else if (tempSenior > 0) { price *= 0.85m; tempSenior--; }
            else if (tempOku > 0) { price *= 0.90m; tempOku--; }

            if (seat.IsPremium) { price *= 1.20m; }

            subtotal += price;
        }

        HttpContext.Session.SetString("SelectedRegion", showtime.Hall.Outlet.City);
        HttpContext.Session.SetString("SelectedCinema", showtime.Hall.Outlet.Name);
        HttpContext.Session.SetString("CollectDate", DateOnly.FromDateTime(showtime.StartTime).ToString());
        if (currentLock != null)
        {
            HttpContext.Session.SetString("LockExpiry", currentLock.ExpiresAt.ToString("O"));
        }
        var bookingData = new BookingSessionData
        {
            ShowTimeId = showtime.ShowTimeId,
            MemberId = user.UserId,
            MovieTitle = showtime.Movie.Title,
            StartTime = showtime.StartTime,
            HallName = showtime.Hall.Name,
            OutletName = showtime.Hall.Outlet.Name,
            TicketPrice = showtime.TicketPrice,

            ChildrenCount = submission.ChildrenCount,
            AdultCount = submission.AdultCount,
            SeniorCount = submission.SeniorCount,
            OkuCount = submission.OkuCount,

            TicketQuantity = uniqueSeatIds.Count,
            SelectedSeatIds = uniqueSeatIds,
            SelectedSeatIdentifiers = seats
                .OrderBy(s => s.SeatIdentifier)
                .Select(s => s.SeatIdentifier)
                .ToList(),
            TicketSubtotal = subtotal
        };

        var jsonData = JsonSerializer.Serialize(bookingData);
        HttpContext.Session.SetString("BookingData", jsonData);

        TempData["BookingData"] = jsonData;

        return RedirectToAction("UserIndex", "Product");
    }

    [HttpPost]
    public async Task<IActionResult> LockSeats([FromBody] SeatLockRequest vm)
    {
        var sessionId = HttpContext.Session.Id;
        var now = DateTime.UtcNow;
        var expiry = now.AddMinutes(5);
        var allExpired = db.SeatLocks.Where(s => s.ExpiresAt < now);
        db.SeatLocks.RemoveRange(allExpired);
        await db.SaveChangesAsync();

        var myExisting = db.SeatLocks.Where(s =>
            vm.SeatIds.Contains(s.SeatId) &&
            s.ShowTimeId == vm.ShowTimeId &&
            s.SessionId == sessionId);

        db.SeatLocks.RemoveRange(myExisting);
        await db.SaveChangesAsync();

        var heldByOthers = await db.SeatLocks.AnyAsync(s =>
            vm.SeatIds.Contains(s.SeatId) &&
            s.ShowTimeId == vm.ShowTimeId &&
            s.SessionId != sessionId &&
            s.ExpiresAt > now);

        if (heldByOthers)
        {
            return Ok(new { success = false, message = "This seat is being held by another user." });
        }

        foreach (var seatId in vm.SeatIds)
        {
            db.SeatLocks.Add(new SeatLock
            {
                ShowTimeId = vm.ShowTimeId,
                SeatId = seatId,
                SessionId = sessionId,
                LockedAt = now,
                ExpiresAt = expiry
            });
        }

        await db.SaveChangesAsync();
        await hub.Clients.All.SendAsync("SeatStatusChanged", vm.ShowTimeId, vm.SeatIds, "locked");
        return Ok(new { success = true, expiresAt = expiry.ToString("o") });
    }


    [HttpPost]
    public async Task<IActionResult> ReleaseSeats([FromBody] SeatLockRequest vm)
    {
        var sessionId = HttpContext.Session.Id;
        var myLock = await db.SeatLocks
            .Where(s => vm.SeatIds.Contains(s.SeatId)
                   && s.ShowTimeId == vm.ShowTimeId
                   && s.SessionId == sessionId)
            .ToListAsync();

        if (myLock.Any())
        {
            db.SeatLocks.RemoveRange(myLock);
            await db.SaveChangesAsync();

            await hub.Clients.All.SendAsync("SeatStatusChanged", vm.ShowTimeId, vm.SeatIds, "available");
        }

        return Ok(new { success = true });
    }


    [HttpGet]
    public IActionResult selectShowtime(int movieId)
    {
        var movie = db.Movies
            .Include(m => m.ShowTimes)
                .ThenInclude(st => st.Hall)
                    .ThenInclude(h => h.Outlet)
            .Include(m => m.ShowTimes)
                .ThenInclude(st => st.Hall)
                    .ThenInclude(h => h.Seats)
            .Include(m => m.ShowTimes)
                .ThenInclude(st => st.Bookings)
                    .ThenInclude(b => b.BookingSeats)
            .FirstOrDefault(m => m.MovieId == movieId);

        if (movie == null)
        {
            TempData["Error"] = "Movie not found";
            return RedirectToAction("Index", "Movie");
        }

        var startDate = DateTime.Now.Date;
        var endDate = startDate.AddDays(31);

        var showtimes = movie.ShowTimes
            .Where(st => st.IsActive && st.StartTime >= startDate && st.StartTime < endDate)
            .OrderBy(st => st.StartTime)
            .ToList();

        var groupedShowtimes = showtimes
            .GroupBy(st => new { st.Hall.OutletId, st.Hall.Outlet.Name, st.Hall.Outlet.City })
            .Select(g => new OutletShowtimes
            {
                OutletId = g.Key.OutletId,
                OutletName = g.Key.Name,
                City = g.Key.City,
                Showtimes = g.Select(st =>
                {
                    var bookedSeats = st.Bookings
                        .SelectMany(b => b.BookingSeats)
                        .Select(bs => bs.SeatId)
                        .Distinct()
                        .Count();

                    var totalSeats = st.Hall.Seats.Count(s => s.IsActive);

                    return new ShowtimeInfo
                    {
                        ShowTimeId = st.ShowTimeId,
                        StartTime = st.StartTime,
                        TicketPrice = st.TicketPrice,
                        HallName = st.Hall.Name,
                        HallType = st.Hall.HallType,
                        AvailableSeats = totalSeats - bookedSeats
                    };
                }).ToList()
            })
            .ToList();

        var viewModel = new MovieShowtimeViewModel
        {
            Movie = movie,
            GroupedShowtimes = groupedShowtimes
        };

        return View(viewModel);
    }

    [Authorize (Roles = "Admin")]
    public IActionResult BookingManagement(string search)
    {
        var bookings = db.Bookings
            .Include(b => b.Member)             
            .Include(b => b.BookingSeats)      
                .ThenInclude(bs => bs.Seat)    
            .Include(b => b.ShowTime)           
                .ThenInclude(s => s.Movie)     
            .OrderByDescending(b => b.BookingDate)
            .ToList();

        if (!string.IsNullOrEmpty(search))
        {
            bookings = bookings.Where(b =>
                b.ShowTime.Movie.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                b.Member.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return View(bookings);
    }

    [HttpGet]
    public IActionResult GetAvailability(int movieId, string date)
    {
        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        var targetDate = DateTime.Parse(date).Date;

        var availability = db.ShowTimes
            .Include(st => st.Hall)
                .ThenInclude(h => h.Seats)
            .Include(st => st.Bookings)
                .ThenInclude(b => b.BookingSeats)
            .Where(st => st.MovieId == movieId && st.StartTime.Date == targetDate && st.IsActive)
            .Select(st => new
            {
                ShowTimeId = st.ShowTimeId,
                TotalSeats = st.Hall.Seats.Count(s => s.IsActive),
                BookedSeats = st.Bookings.SelectMany(b => b.BookingSeats).Select(bs => bs.SeatId).Distinct().Count(),
                AvailableSeats = st.Hall.Seats.Count(s => s.IsActive) -
                               st.Bookings.SelectMany(b => b.BookingSeats).Select(bs => bs.SeatId).Distinct().Count()
            })
            .Select(st => new
            {
                st.ShowTimeId,
                st.AvailableSeats,
                st.TotalSeats,
                st.BookedSeats,
                IsSoldOut = st.AvailableSeats <= 0
            })
            .ToList();

        if (isAjax)
        {
            return Json(availability);
        }

        return Ok(availability);
    }

    [HttpGet]
    public JsonResult GetShowtimeAvailabilityAjax(int movieId, string date)
    {
        var targetDate = DateTime.Parse(date).Date;

        var availability = db.ShowTimes
            .Include(st => st.Hall)
                .ThenInclude(h => h.Seats)
            .Include(st => st.Bookings)
                .ThenInclude(b => b.BookingSeats)
            .Where(st => st.MovieId == movieId && st.StartTime.Date == targetDate && st.IsActive)
            .Select(st => new
            {
                ShowTimeId = st.ShowTimeId,
                TotalSeats = st.Hall.Seats.Count(s => s.IsActive),
                BookedSeats = st.Bookings.SelectMany(b => b.BookingSeats).Select(bs => bs.SeatId).Distinct().Count(),
                AvailableSeats = st.Hall.Seats.Count(s => s.IsActive) -
                               st.Bookings.SelectMany(b => b.BookingSeats).Select(bs => bs.SeatId).Distinct().Count()
            })
            .Select(st => new
            {
                st.ShowTimeId,
                st.AvailableSeats,
                st.TotalSeats,
                st.BookedSeats,
                IsSoldOut = st.AvailableSeats <= 0,
                PercentageFull = st.TotalSeats > 0 ? (int)((double)st.BookedSeats / st.TotalSeats * 100) : 0
            })
            .ToList();

        return Json(availability);
    }
}
    public class TicketCalculationRequest
{
    public int ShowTimeId { get; set; }
    public int ChildrenCount { get; set; }
    public int AdultCount { get; set; }
    public int SeniorCount { get; set; }
    public int OkuCount { get; set; }
    public int SelectedSeatsCount { get; set; }
    public List<int> SelectedSeatIds { get; set; } = new();
}

public class SeatValidationRequest
{
    public int ShowTimeId { get; set; }
    public List<int> SeatIds { get; set; }
}

public class TicketSelectionSubmission
{
    public int ShowTimeId { get; set; }
    public List<int> SeatIds { get; set; } = new();
    public int ChildrenCount { get; set; }
    public int AdultCount { get; set; }
    public int SeniorCount { get; set; }
    public int OkuCount { get; set; }
}
public class SeatLockRequest
{
    public int ShowTimeId { get; set; }
    public List<int> SeatIds { get; set; }
}