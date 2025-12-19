using AssignmentC_;
using AssignmentC_.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
namespace AssignmentC_.Controllers;

public class HallController(DB db, Helper hp) : Controller
{
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public IActionResult ManageHalls()
    {
        Console.WriteLine("DB = " + db.Database.GetDbConnection().ConnectionString);

        var halls = db.Halls
            .Include(h => h.Outlet)
            .Include(h => h.Seats)
            .Select(h => new HallViewModel
            {
                HallId = h.HallId,
                Name = h.Name,
                OutletId = h.OutletId,
                OutletName = h.Outlet.Name,
                Capacity = h.Capacity,
                HallType = h.HallType,
                IsActive = h.IsActive,
                TotalSeats = h.Seats.Count,
                PremiumSeats = h.Seats.Count(s => s.IsPremium)
            })
            .ToList();

        return View(halls);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public IActionResult AddHall()
    {
        var vm = new HallViewModel
        {
            OutletList = db.Outlets
                .Select(o => new SelectListItem
                {
                    Value = o.OutletId.ToString(),
                    Text = $"{o.Name} - {o.City}"
                })
                .ToList(),
            IsActive = true
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddHall(HallViewModel vm)
    {
        Console.WriteLine("🔥 POST AddHall HIT 🔥");
        foreach (var entry in ModelState)
        {
            foreach (var error in entry.Value.Errors)
            {
                Console.WriteLine($"{entry.Key}: {error.ErrorMessage}");
            }
        }
        if (!ModelState.IsValid)
        {
            foreach (var entry in ModelState)
            {
                foreach (var error in entry.Value.Errors)
                {
                    Console.WriteLine($"{entry.Key}: {error.ErrorMessage}");
                }
            }

            vm.OutletList = db.Outlets
                .Select(o => new SelectListItem
                {
                    Value = o.OutletId.ToString(),
                    Text = $"{o.Name} - {o.City}"
                })
                .ToList();

            return View(vm);
        }

        int capacity = vm.Rows!.Value * vm.SeatsPerRow!.Value;

        var hall = new Hall
        {
            Name = vm.Name,
            OutletId = vm.OutletId,
            Capacity = capacity,
            HallType = vm.HallType,
            IsActive = vm.IsActive
        };

        db.Halls.Add(hall);
        db.SaveChanges();

        // Generate seats based on rows and seats per row
        for (int row = 0; row < vm.Rows; row++)
        {
            for (int col = 1; col <= vm.SeatsPerRow; col++)
            {
                db.Seats.Add(new Seat
                {
                    HallId = hall.HallId,
                    SeatIdentifier = $"{(char)('A' + row)}{col}",
                    IsPremium = false
                });
            }
        }

        db.SaveChanges();

        TempData["Info"] = $"Hall '{hall.Name}' created successfully with {capacity} seats.";

        return RedirectToAction("EditHallSeats", new { id = hall.HallId });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public IActionResult EditHallSeats(int id)
    {
        var hall = db.Halls
            .Include(h => h.Outlet)
            .Include(h => h.Seats)
            .FirstOrDefault(h => h.HallId == id);

        if (hall == null)
            return NotFound();

        var vm = new HallViewModel
        {
            HallId = hall.HallId,
            Name = hall.Name,
            OutletName = hall.Outlet.Name,
            Capacity = hall.Capacity,
            Seats = hall.Seats
                .OrderBy(s => s.SeatIdentifier)
                .Select(s => new SeatViewModel
                {
                    SeatId = s.SeatId,
                    SeatIdentifier = s.SeatIdentifier,
                    IsPremium = s.IsPremium,
                    IsWheelchair = s.IsWheelchair,
                    IsActive = s.IsActive,
                    // Extract Row and Column from SeatIdentifier (e.g., "A5" -> Row="A", Column=5)
                    Row = s.SeatIdentifier.Substring(0, 1),
                    Column = int.Parse(s.SeatIdentifier.Substring(1))
                }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    public IActionResult UpdateSeat([FromBody] SeatUpdateRequest request)
    {
        var seat = db.Seats.Find(request.SeatId);
        if (seat == null)
            return NotFound();

        seat.IsPremium = request.IsPremium;
        seat.IsWheelchair = request.IsWheelchair;
        seat.IsActive = request.IsActive;

        db.SaveChanges();

        return Json(new { success = true });
    }

    [HttpPost]
    public IActionResult SaveAllSeats([FromBody] List<SeatUpdateRequest> seats)
    {
        // Add logging and null checks
        Console.WriteLine("SaveAllSeats called");

        if (seats == null)
        {
            Console.WriteLine("❌ Seats parameter is NULL");
            return Json(new { success = false, message = "No seat data received" });
        }

        Console.WriteLine($"Received {seats.Count} seats to update");

        try
        {
            foreach (var seatData in seats)
            {
                if (seatData == null)
                {
                    Console.WriteLine("⚠️ Null seatData in list, skipping");
                    continue;
                }

                Console.WriteLine($"Updating seat ID: {seatData.SeatId}");

                var seat = db.Seats.Find(seatData.SeatId);
                if (seat != null)
                {
                    seat.IsPremium = seatData.IsPremium;
                    seat.IsWheelchair = seatData.IsWheelchair;
                    seat.IsActive = seatData.IsActive;
                }
                else
                {
                    Console.WriteLine($"⚠️ Seat with ID {seatData.SeatId} not found");
                }
            }

            db.SaveChanges();
            Console.WriteLine("✅ Seats saved successfully");

            TempData["Info"] = "Seat layout saved successfully!";
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error saving seats: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return Json(new { success = false, message = ex.Message });
        }
    }
    public class SeatUpdateRequest
    {
        public int SeatId { get; set; }
        public bool IsPremium { get; set; }
        public bool IsWheelchair { get; set; }
        public bool IsActive { get; set; }
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public IActionResult EditHall(int id)
    {
        var hall = db.Halls
            .Include(h => h.Outlet)
            .FirstOrDefault(h => h.HallId == id);

        if (hall == null)
        {
            TempData["Error"] = "Hall not found";
            return RedirectToAction("ManageHalls");
        }

        var vm = new HallViewModel
        {
            HallId = hall.HallId,
            Name = hall.Name,
            OutletId = hall.OutletId,
            HallType = hall.HallType,
            IsActive = hall.IsActive,
            Capacity = hall.Capacity,
            OutletList = db.Outlets
                .Select(o => new SelectListItem
                {
                    Value = o.OutletId.ToString(),
                    Text = $"{o.Name} - {o.City}"
                })
                .ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditHall(HallViewModel vm)
    {
        ModelState.Remove("Rows");
        ModelState.Remove("SeatsPerRow");

        if (!ModelState.IsValid)
        {
            vm.OutletList = db.Outlets
                .Select(o => new SelectListItem
                {
                    Value = o.OutletId.ToString(),
                    Text = $"{o.Name} - {o.City}"
                }).ToList();
            return View(vm);
        }

        var hall = db.Halls.Find(vm.HallId);
        if (hall == null)
        {
            TempData["Error"] = "Hall not found";
            return RedirectToAction("ManageHalls");
        }

        // UPDATE ONLY GENERAL INFO
        hall.Name = vm.Name;
        hall.OutletId = vm.OutletId;
        hall.HallType = vm.HallType;
        hall.IsActive = vm.IsActive;

        db.SaveChanges();

        TempData["Info"] = "Hall information updated successfully.";
        return RedirectToAction("ManageHalls");
    }

    [HttpPost]
    public IActionResult ToggleHallStatus(int id)
    {
        var hall = db.Halls.Find(id);

        if (hall == null)
            return Json(new { success = false, message = "Hall not found" });
        if (hall.IsActive)
        {
            bool hasActiveShowtimes = db.ShowTimes.Any(st => st.HallId == id && st.StartTime >= DateTime.Today);
            if (hasActiveShowtimes)
            {
                return Json(new
                {
                    success = false,
                    message = "Cannot disable a hall with active showtimes today. Please reschedule movies first."
                });
            }
        }

        // Flip the status
        hall.IsActive = !hall.IsActive;
        db.SaveChanges();

        return Json(new
        {
            success = true,
            isActive = hall.IsActive,
            message = $"Hall is now {(hall.IsActive ? "Active" : "Inactive")}"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteHall(int id)
    {
        var hall = db.Halls
            .Include(h => h.Seats)
            .Include(h => h.ShowTimes)
            .FirstOrDefault(h => h.HallId == id);

        if (hall == null)
        {
            TempData["Error"] = "Hall not found.";
            return RedirectToAction("ManageHalls");
        }


        if (hall.ShowTimes.Any(st => st.StartTime > DateTime.Now))
        {
            TempData["Error"] = $"Cannot delete '{hall.Name}' because it is linked to movie showtimes. Try disabling it instead.";
            return RedirectToAction("ManageHalls");
        }

        try
        {
            if (hall.Seats.Any())
            {
                db.Seats.RemoveRange(hall.Seats);
            }

            db.Halls.Remove(hall);

            db.SaveChanges();

            TempData["Info"] = $"Hall '{hall.Name}' and its seats were successfully deleted.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = "An error occurred while deleting the hall: " + ex.Message;
        }

        return RedirectToAction("ManageHalls");
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RegenerateSeats(int hallId, int rows, int seatsPerRow)
    {
        Console.WriteLine("===========================================");
        Console.WriteLine($"[REGENERATE] 🔥 METHOD CALLED 🔥");
        Console.WriteLine($"[REGENERATE] HallId: {hallId}");
        Console.WriteLine($"[REGENERATE] Rows: {rows}");
        Console.WriteLine($"[REGENERATE] SeatsPerRow: {seatsPerRow}");
        Console.WriteLine("===========================================");

        // Validation
        if (rows < 1 || rows > 26)
        {
            TempData["Error"] = "Rows must be between 1 and 26";
            return RedirectToAction("EditHallSeats", new { id = hallId });
        }

        if (seatsPerRow < 1 || seatsPerRow > 50)
        {
            TempData["Error"] = "Seats per row must be between 1 and 50";
            return RedirectToAction("EditHallSeats", new { id = hallId });
        }

        var hall = db.Halls
            .Include(h => h.Seats)
            .Include(h => h.ShowTimes)
            .FirstOrDefault(h => h.HallId == hallId);

        if (hall == null)
        {
            TempData["Error"] = "Hall not found";
            return RedirectToAction("ManageHalls");
        }

        // Safety check: Don't allow regeneration if there are active showtimes
        var hasActiveShowtimes = hall.ShowTimes.Any(st => st.StartTime > DateTime.Now);
        if (hasActiveShowtimes)
        {
            TempData["Error"] = "Cannot regenerate seats: This hall has upcoming showtimes. Please delete or reschedule them first.";
            return RedirectToAction("EditHallSeats", new { id = hallId });
        }

        // Safety check: Don't allow if there are any bookings
        var hasBookings = db.Bookings.Any(b => b.ShowTime.HallId == hallId);
        if (hasBookings)
        {
            TempData["Error"] = "Cannot regenerate seats: This hall has existing bookings.";
            return RedirectToAction("EditHallSeats", new { id = hallId });
        }

        try
        {
            var oldCapacity = hall.Capacity;
            var newCapacity = rows * seatsPerRow;

            Console.WriteLine($"[REGENERATE] Old capacity: {oldCapacity}, New capacity: {newCapacity}");
            Console.WriteLine($"[REGENERATE] Deleting {hall.Seats.Count} existing seats...");

            // Delete all existing seats
            db.Seats.RemoveRange(hall.Seats);
            db.SaveChanges(); // Save deletion first

            // Update hall capacity
            hall.Capacity = newCapacity;

            // Generate new seats
            Console.WriteLine($"[REGENERATE] Creating {newCapacity} new seats...");
            var newSeats = new List<Seat>();

            for (int row = 0; row < rows; row++)
            {
                char rowLetter = (char)('A' + row);
                for (int col = 1; col <= seatsPerRow; col++)
                {
                    newSeats.Add(new Seat
                    {
                        HallId = hall.HallId,
                        SeatIdentifier = $"{rowLetter}{col}",
                        IsPremium = false,
                        IsWheelchair = false,
                        IsActive = true
                    });
                }
            }

            db.Seats.AddRange(newSeats);
            db.SaveChanges();

            Console.WriteLine($"[REGENERATE] ✅ Success! Created {newSeats.Count} seats");

            TempData["Info"] = $"Seat layout regenerated successfully! Changed from {oldCapacity} to {newCapacity} seats. All seats are now standard and active.";
            return RedirectToAction("EditHallSeats", new { id = hallId });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[REGENERATE] ❌ Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");

            TempData["Error"] = $"Error regenerating seats: {ex.Message}";
            return RedirectToAction("EditHallSeats", new { id = hallId });
        }
    }

    private bool CanRegenerateSeats(int hallId, out string errorMessage)
    {
        errorMessage = null;

        var hall = db.Halls
            .Include(h => h.ShowTimes)
            .FirstOrDefault(h => h.HallId == hallId);

        if (hall == null)
        {
            errorMessage = "Hall not found";
            return false;
        }

        // Check for future showtimes
        var hasFutureShowtimes = hall.ShowTimes.Any(st => st.StartTime > DateTime.Now);
        if (hasFutureShowtimes)
        {
            errorMessage = "This hall has upcoming showtimes";
            return false;
        }

        // Check for any bookings
        var hasBookings = db.Bookings.Any(b => b.ShowTime.HallId == hallId);
        if (hasBookings)
        {
            errorMessage = "This hall has existing bookings";
            return false;
        }

        return true;
    }
}