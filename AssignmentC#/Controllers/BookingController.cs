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
                    // Parse Row and Column from SeatIdentifier (e.g., "A1" -> Row="A", Column=1)
                    Row = new string(s.SeatIdentifier.TakeWhile(char.IsLetter).ToArray()),
                    Column = int.Parse(new string(s.SeatIdentifier.SkipWhile(char.IsLetter).ToArray()))
                })
                .ToList()
        };

        Console.WriteLine($"Created ViewModel with {vm.Seats.Count} seats");

        // Debug: Print first few seats
        foreach (var seat in vm.Seats.Take(5))
        {
            Console.WriteLine($"Seat: {seat.SeatIdentifier}, ID: {seat.SeatId}, Row: {seat.Row}, Col: {seat.Column}");
        }

        return View(vm);
    }

    // POST: /Booking/AddToCart
    [HttpPost]
    public IActionResult AddToCart([FromBody] AddToCartRequest req)
    {
        try
        {
            Console.WriteLine("========== AddToCart Called ==========");
            Console.WriteLine($"Request received: {req != null}");

            if (req == null)
            {
                Console.WriteLine("❌ Request object is NULL");
                return Json(new { success = false, message = "Invalid request - no data received" });
            }

            Console.WriteLine($"ShowTimeId: {req.ShowTimeId}");
            Console.WriteLine($"SeatIds: {req.SeatIds?.Count ?? 0}");

            if (req.SeatIds != null && req.SeatIds.Any())
            {
                Console.WriteLine($"Seat IDs: {string.Join(", ", req.SeatIds)}");
            }

            if (req.ShowTimeId <= 0)
            {
                Console.WriteLine("❌ Invalid ShowTimeId");
                return Json(new { success = false, message = "Invalid showtime" });
            }

            if (req.SeatIds == null || !req.SeatIds.Any())
            {
                Console.WriteLine("❌ No seats selected");
                return Json(new { success = false, message = "Please select at least one seat" });
            }

            // Verify showtime exists
            var showtime = db.ShowTimes
                .Include(st => st.Movie)
                .Include(st => st.Hall)
                    .ThenInclude(h => h.Outlet)
                .FirstOrDefault(st => st.ShowTimeId == req.ShowTimeId);

            if (showtime == null)
            {
                Console.WriteLine($"❌ Showtime {req.ShowTimeId} not found");
                return Json(new { success = false, message = "Showtime not found" });
            }

            Console.WriteLine($"✅ Found showtime: {showtime.Movie.Title}");

            // Get seat details
            var seats = db.Seats
                .Where(s => req.SeatIds.Contains(s.SeatId))
                .ToList();

            Console.WriteLine($"Found {seats.Count} seats in database");

            if (seats.Count != req.SeatIds.Count)
            {
                Console.WriteLine($"❌ Seat count mismatch. Expected: {req.SeatIds.Count}, Found: {seats.Count}");
                return Json(new { success = false, message = "Some seats are invalid" });
            }

            // Check if seats are already booked
            var bookedSeatIds = db.BookingSeats
                .Where(bs => bs.Booking.ShowTimeId == req.ShowTimeId)
                .Select(bs => bs.SeatId)
                .ToHashSet();

            Console.WriteLine($"Found {bookedSeatIds.Count} already booked seats for this showtime");

            var alreadyBooked = req.SeatIds.Intersect(bookedSeatIds).ToList();
            if (alreadyBooked.Any())
            {
                var bookedIdentifiers = seats
                    .Where(s => alreadyBooked.Contains(s.SeatId))
                    .Select(s => s.SeatIdentifier);
                Console.WriteLine($"❌ Seats already booked: {string.Join(", ", bookedIdentifiers)}");
                return Json(new
                {
                    success = false,
                    message = $"Seats {string.Join(", ", bookedIdentifiers)} are already booked"
                });
            }

            // Add to cart (session)
            var cart = GetCart();
            Console.WriteLine($"Current cart has {cart.Items.Count} items");

            // Check if seats are already in cart
            var existingItem = cart.Items.FirstOrDefault(i => i.ShowTimeId == req.ShowTimeId);
            if (existingItem != null)
            {
                Console.WriteLine($"Found existing cart item for this showtime");
                var duplicateSeats = existingItem.SeatIds.Intersect(req.SeatIds).ToList();
                if (duplicateSeats.Any())
                {
                    var dupIdentifiers = seats
                        .Where(s => duplicateSeats.Contains(s.SeatId))
                        .Select(s => s.SeatIdentifier);
                    Console.WriteLine($"❌ Duplicate seats in cart: {string.Join(", ", dupIdentifiers)}");
                    return Json(new
                    {
                        success = false,
                        message = $"Seats {string.Join(", ", dupIdentifiers)} are already in your cart"
                    });
                }
            }

            // Create cart item
            var cartItem = new CartItem
            {
                MovieTitle = showtime.Movie.Title,
                ShowTimeId = showtime.ShowTimeId,
                ShowTimeDisplay = showtime.StartTime.ToString("MMM dd, yyyy - h:mm tt"),
                HallName = $"{showtime.Hall.Name} - {showtime.Hall.Outlet.Name}",
                Price = showtime.TicketPrice,
                SeatIds = req.SeatIds,
                Seats = seats.Select(s => s.SeatIdentifier).ToList()
            };

            Console.WriteLine($"Created cart item: {cartItem.MovieTitle}, {cartItem.Seats.Count} seats");

            cart.Items.Add(cartItem);
            SaveCart(cart);

            Console.WriteLine($"✅ Cart saved. Total items: {cart.Items.Count}");

            return Json(new
            {
                success = true,
                message = $"Added {seats.Count} seat(s) to cart",
                cartCount = cart.Items.Sum(i => i.Seats.Count)
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌❌❌ EXCEPTION in AddToCart ❌❌❌");
            Console.WriteLine($"Message: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    private CartViewModel GetCart()
    {
        try
        {
            var json = HttpContext.Session.GetString("CART");
            Console.WriteLine($"Session JSON: {json ?? "NULL"}");

            if (string.IsNullOrEmpty(json))
            {
                Console.WriteLine("Creating new cart");
                return new CartViewModel();
            }

            var cart = System.Text.Json.JsonSerializer.Deserialize<CartViewModel>(json);
            Console.WriteLine($"Deserialized cart with {cart?.Items?.Count ?? 0} items");
            return cart ?? new CartViewModel();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in GetCart: {ex.Message}");
            return new CartViewModel();
        }
    }

    private void SaveCart(CartViewModel cart)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(cart);
            Console.WriteLine($"Saving cart JSON: {json}");
            HttpContext.Session.SetString("CART", json);
            Console.WriteLine("✅ Cart saved to session");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in SaveCart: {ex.Message}");
            throw;
        }
    }
}