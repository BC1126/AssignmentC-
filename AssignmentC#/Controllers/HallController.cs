using AssignmentC_;
using AssignmentC_.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
namespace AssignmentC_.Controllers;

public class HallController(DB db, Helper hp) : Controller
{
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

        // Calculate capacity based on rows and seats per row
        int capacity = vm.Rows!.Value * vm.SeatsPerRow!.Value;

        // Create the Hall
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

        var hall = db.Halls.Include(h => h.Seats).FirstOrDefault(h => h.HallId == vm.HallId);

        if (hall == null)
        {
            TempData["Error"] = "Hall not found";
            return RedirectToAction("ManageHalls");
        }

        bool dimensionsChanged = (vm.Rows.HasValue && vm.SeatsPerRow.HasValue) &&
                                 (hall.Capacity != (vm.Rows.Value * vm.SeatsPerRow.Value));

        if (dimensionsChanged)
        {
            // 1. Check if there are active bookings or showtimes
            var hasShowtimes = db.ShowTimes.Any(st => st.HallId == hall.HallId);
            if (hasShowtimes)
            {
                TempData["Error"] = "Cannot change seat capacity/layout for a hall that has active showtimes. Delete showtimes first.";
                return RedirectToAction("EditHall", new { id = hall.HallId });
            }

            // 2. Remove old seats
            db.Seats.RemoveRange(hall.Seats);

            // 3. Update Capacity
            hall.Capacity = vm.Rows.Value * vm.SeatsPerRow.Value;

            // 4. Generate new seats
            for (int r = 0; r < vm.Rows.Value; r++)
            {
                for (int c = 1; c <= vm.SeatsPerRow.Value; c++)
                {
                    db.Seats.Add(new Seat
                    {
                        HallId = hall.HallId,
                        SeatIdentifier = $"{(char)('A' + r)}{c}",
                        IsPremium = false,
                        IsActive = true
                    });
                }
            }
        }

        // Update general hall info
        hall.Name = vm.Name;
        hall.OutletId = vm.OutletId;
        hall.HallType = vm.HallType;
        hall.IsActive = vm.IsActive;

        db.SaveChanges();

        TempData["Info"] = dimensionsChanged
            ? $"Hall layout updated to {hall.Capacity} seats."
            : "Hall information updated successfully.";

        return RedirectToAction("ManageHalls");
    }

    [HttpPost]
    public IActionResult ToggleHallStatus(int id)
    {
        var hall = db.Halls.Find(id);

        if (hall == null)
            return Json(new { success = false, message = "Hall not found" });

        hall.IsActive = !hall.IsActive;
        db.SaveChanges();

        return Json(new { success = true, isActive = hall.IsActive });
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
            TempData["Error"] = "Hall not found";
            return RedirectToAction("ManageHalls");
        }

        // Check if hall has any showtimes
        if (hall.ShowTimes.Any())
        {
            TempData["Error"] = "Cannot delete hall with existing showtimes. Disable it instead.";
            return RedirectToAction("ManageHalls");
        }

        // Delete all seats first
        db.Seats.RemoveRange(hall.Seats);

        // Delete the hall
        db.Halls.Remove(hall);
        db.SaveChanges();

        TempData["Info"] = $"Hall '{hall.Name}' deleted successfully.";
        return RedirectToAction("ManageHalls");
    }
}
