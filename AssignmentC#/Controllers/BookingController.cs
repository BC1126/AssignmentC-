using System.Text.Json;
using AssignmentC_.Hubs;
using AssignmentC_.Models;
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

        // 1. Get booked seats
        var bookedSeatIds = showtime.Bookings
            .SelectMany(b => b.BookingSeats)
            .Select(bs => bs.SeatId)
            .ToHashSet();

        // 2. Clean up expired locks
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
        if (request.ShowTimeId <= 0)
        {
            return BadRequest(new { success = false, message = "Invalid showtime" });
        }

        var showtime = db.ShowTimes.Find(request.ShowTimeId);
        if (showtime == null)
        {
            return NotFound(new { success = false, message = "Showtime not found" });
        }

        // Calculate pricing regardless of validation
        decimal ticketPrice = showtime.TicketPrice;
        decimal childrenPrice = ticketPrice * 0.8m; // 20% discount
        decimal seniorPrice = ticketPrice * 0.85m; // 15% discount

        decimal subtotal = (request.ChildrenCount * childrenPrice) +
                          (request.AdultCount * ticketPrice) +
                          (request.SeniorCount * seniorPrice);

        // Validate ticket counts
        int totalTickets = request.ChildrenCount + request.AdultCount + request.SeniorCount;
        bool isValid = totalTickets == request.SelectedSeatsCount && request.SelectedSeatsCount > 0;

        return Ok(new
        {
            success = true,
            isValid = isValid,
            message = isValid ? "Valid" : "Ticket type count must match the number of selected seats",
            childrenCount = request.ChildrenCount,
            adultCount = request.AdultCount,
            seniorCount = request.SeniorCount,
            totalTickets,
            selectedSeats = request.SelectedSeatsCount,
            subtotal = subtotal.ToString("F2"),
            ticketPrice = ticketPrice.ToString("F2"),
            childrenPrice = childrenPrice.ToString("F2"),
            seniorPrice = seniorPrice.ToString("F2")
        });
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

        if (submission.ShowTimeId <= 0 || !uniqueSeatIds.Any())
        {
            TempData["Error"] = "Please select at least one seat";
            return RedirectToAction("SelectTicket", new { showtimeId = submission.ShowTimeId });
        }

        int totalTickets = submission.ChildrenCount + submission.AdultCount + submission.SeniorCount;
        if (totalTickets != uniqueSeatIds.Count)
        {
            TempData["Error"] = "Ticket type count must match the number of selected seats";
            return RedirectToAction("SelectTicket", new { showtimeId = submission.ShowTimeId });
        }

        var showtime = db.ShowTimes
            .Include(st => st.Movie)
            .Include(st => st.Hall).ThenInclude(h => h.Outlet)
            .FirstOrDefault(st => st.ShowTimeId == submission.ShowTimeId);

        if (showtime == null)
        {
            TempData["Error"] = "Showtime not found";
            return RedirectToAction("Index", "Movie");
        }



        var seats = db.Seats
            .Where(s => uniqueSeatIds.Contains(s.SeatId) && s.HallId == showtime.HallId)
            .ToList();

        if (seats.Count != uniqueSeatIds.Count)
        {
            TempData["Error"] = "Some seats are invalid";
            return RedirectToAction("SelectTicket", new { showtimeId = submission.ShowTimeId });
        }

        var bookedSeatIds = db.BookingSeats
            .Where(bs => bs.Booking.ShowTimeId == submission.ShowTimeId)
            .Select(bs => bs.SeatId)
            .ToHashSet();


        var alreadyBooked = uniqueSeatIds.Intersect(bookedSeatIds).ToList();
        if (alreadyBooked.Any())
        {
            // FIX: Query the 'seats' list (objects), not 'uniqueSeatIds' (integers)
            var bookedIdentifiers = seats
                .Where(s => alreadyBooked.Contains(s.SeatId))
                .Select(s => s.SeatIdentifier);

            TempData["Error"] = $"Seats {string.Join(", ", bookedIdentifiers)} are already booked";
            return RedirectToAction("SelectTicket", new { showtimeId = submission.ShowTimeId });
        }

        // Pricing
        decimal ticketPrice = showtime.TicketPrice;
        decimal childrenPrice = ticketPrice * 0.8m;
        decimal seniorPrice = ticketPrice * 0.85m;
        decimal subtotal = (submission.ChildrenCount * childrenPrice) +
                          (submission.AdultCount * ticketPrice) +
                          (submission.SeniorCount * seniorPrice);


        var bookingData = new BookingSessionData
        {
            ShowTimeId = showtime.ShowTimeId,
            MovieTitle = showtime.Movie.Title,
            StartTime = showtime.StartTime,
            HallName = showtime.Hall.Name,
            OutletName = showtime.Hall.Outlet.Name,
            TicketPrice = ticketPrice,
            ChildrenCount = submission.ChildrenCount,
            AdultCount = submission.AdultCount,
            SeniorCount = submission.SeniorCount,
            TicketQuantity = seats.Count,
            SelectedSeatIds = uniqueSeatIds, 
            SelectedSeatIdentifiers = seats
                .Select(s => s.SeatIdentifier)
                .Distinct()
                .OrderBy(s => s)
                .ToList(),
            TicketSubtotal = subtotal
        };

        TempData["BookingData"] = JsonSerializer.Serialize(bookingData);
        return RedirectToAction("Purchase", "Ticket");

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

        // 2. NOW CHECK IF ANYONE *ELSE* HAS IT
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

        // Find the lock owned by the current user
        var myLock = await db.SeatLocks
            .Where(s => vm.SeatIds.Contains(s.SeatId)
                   && s.ShowTimeId == vm.ShowTimeId
                   && s.SessionId == sessionId)
            .ToListAsync();

        if (myLock.Any())
        {
            db.SeatLocks.RemoveRange(myLock);
            await db.SaveChangesAsync();

            // THIS IS THE KEY: Tell everyone else to unlock this seat NOW
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
        var endDate = startDate.AddDays(7);

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
                Showtimes = g.Select(st => {
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
}

// Request models for AJAX endpoints
public class TicketCalculationRequest
{
    public int ShowTimeId { get; set; }
    public int ChildrenCount { get; set; }
    public int AdultCount { get; set; }
    public int SeniorCount { get; set; }
    public int SelectedSeatsCount { get; set; }
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
}
public class SeatLockRequest
{
    public int ShowTimeId { get; set; }
    public List<int> SeatIds { get; set; }
}