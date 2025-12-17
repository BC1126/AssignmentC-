using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssignmentC_.Models;

namespace AssignmentC_.Controllers;

public class BookingController : Controller
{
    private readonly DB db;

    public BookingController(DB db)
    {
        this.db = db;
    }

    // GET: /Booking/SelectTicket?showtimeId=123
    [HttpGet]
    public IActionResult SelectTicket(int showtimeId)
    {
        Console.WriteLine($"SelectTicket called with showtimeId: {showtimeId}");

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
            Console.WriteLine($"❌ Showtime {showtimeId} not found");
            TempData["Error"] = "Showtime not found";
            return RedirectToAction("Index", "Movie");
        }

        Console.WriteLine($"✅ Found showtime: {showtime.Movie.Title}");

        // Get all booked seat IDs for this showtime
        var bookedSeatIds = showtime.Bookings
            .SelectMany(b => b.BookingSeats)
            .Select(bs => bs.SeatId)
            .ToHashSet();

        Console.WriteLine($"Found {bookedSeatIds.Count} booked seats");

        // Create ViewModel
        var vm = new SelectTicketViewModel
        {
            ShowTimeId = showtime.ShowTimeId,
            MovieTitle = showtime.Movie.Title,
            MoviePosterUrl = showtime.Movie.PosterUrl,
            StartTime = showtime.StartTime,
            HallName = showtime.Hall.Name,
            OutletName = showtime.Hall.Outlet.Name,
            TicketPrice = showtime.TicketPrice,
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
                    Row = new string(s.SeatIdentifier.TakeWhile(char.IsLetter).ToArray()),
                    Column = int.Parse(new string(s.SeatIdentifier.SkipWhile(char.IsLetter).ToArray()))
                })
                .ToList()
        };

        Console.WriteLine($"Created ViewModel with {vm.Seats.Count} seats");

        return View(vm);
    }

    // POST: /Booking/SelectTicket
    // User submits selected seats, validates them, then proceeds to Food & Beverage page
    [HttpPost]
    public IActionResult SelectTicket(int showTimeId, List<int> seatIds)
    {
        Console.WriteLine($"========== POST SelectTicket ==========");
        Console.WriteLine($"ShowTimeId: {showTimeId}");
        Console.WriteLine($"SeatIds: {string.Join(", ", seatIds)}");

        // Validate input
        if (showTimeId <= 0 || seatIds == null || !seatIds.Any())
        {
            TempData["Error"] = "Please select at least one seat";
            return RedirectToAction("SelectTicket", new { showtimeId = showTimeId });
        }

        // Verify showtime exists
        var showtime = db.ShowTimes
            .Include(st => st.Movie)
            .Include(st => st.Hall)
                .ThenInclude(h => h.Outlet)
            .FirstOrDefault(st => st.ShowTimeId == showTimeId);

        if (showtime == null)
        {
            TempData["Error"] = "Showtime not found";
            return RedirectToAction("Index", "Movie");
        }

        // Get seat details
        var seats = db.Seats
            .Where(s => seatIds.Contains(s.SeatId))
            .ToList();

        if (seats.Count != seatIds.Count)
        {
            TempData["Error"] = "Some seats are invalid";
            return RedirectToAction("SelectTicket", new { showtimeId = showTimeId });
        }

        // Check if seats are already booked
        var bookedSeatIds = db.BookingSeats
            .Where(bs => bs.Booking.ShowTimeId == showTimeId)
            .Select(bs => bs.SeatId)
            .ToHashSet();

        var alreadyBooked = seatIds.Intersect(bookedSeatIds).ToList();
        if (alreadyBooked.Any())
        {
            var bookedIdentifiers = seats
                .Where(s => alreadyBooked.Contains(s.SeatId))
                .Select(s => s.SeatIdentifier);
            TempData["Error"] = $"Seats {string.Join(", ", bookedIdentifiers)} are already booked";
            return RedirectToAction("SelectTicket", new { showtimeId = showTimeId });
        }

        // ==========================================
        // Store booking data in TempData for Food & Beverage page
        // ==========================================
        var bookingData = new BookingSessionData
        {
            ShowTimeId = showtime.ShowTimeId,
            MovieTitle = showtime.Movie.Title,
            StartTime = showtime.StartTime,
            HallName = showtime.Hall.Name,
            OutletName = showtime.Hall.Outlet.Name,
            TicketPrice = showtime.TicketPrice,
            SelectedSeatIds = seatIds,
            SelectedSeatIdentifiers = seats.Select(s => s.SeatIdentifier).ToList(),
            TicketQuantity = seats.Count,
            TicketSubtotal = showtime.TicketPrice * seats.Count
        };

        var json = System.Text.Json.JsonSerializer.Serialize(bookingData);
        TempData["BookingData"] = json;

        return RedirectToAction("Index", "Cart");
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

        // Get showtimes for the next 7 days
        var startDate = DateTime.Now.Date;
        var endDate = startDate.AddDays(7);

        var showtimes = movie.ShowTimes
            .Where(st => st.IsActive && st.StartTime >= startDate && st.StartTime < endDate)
            .OrderBy(st => st.StartTime)
            .ToList();

        // Group by outlet
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