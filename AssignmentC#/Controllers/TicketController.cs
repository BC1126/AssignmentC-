using AssignmentC_.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AssignmentC_.Controllers;

public class TicketController(DB db) : Controller
{
    private const int TIME_LIMIT = 8 * 60;

    public IActionResult Checkout()
    {
        
            if (HttpContext.Session.GetString("StartTime") == null)
            {
                HttpContext.Session.SetString("StartTime",DateTime.Now.ToString("O"));
            }

            DateTime startTime = DateTime.Parse(HttpContext.Session.GetString("StartTime"));

            int elapsed = (int)(DateTime.Now - startTime).TotalSeconds;
            int remaining = TIME_LIMIT - elapsed;

            var timer = new TimerViewModel
            {
                Expired = remaining <= 0,
                Minutes = Math.Max(remaining, 0) / 60,
                Seconds = Math.Max(remaining, 0) % 60
            };

            var vm = new CheckoutViewModel
            {
                Timer = timer
            };

        if (Request.IsAjax())
        {
            return PartialView("_Timer", timer);
        }

        return View(vm);
    }

    [HttpGet]
    public IActionResult Purchase()
    {
        /*
        if (TempData["BookingData"] != null)
        {
            var json = TempData["BookingData"] as string;

            if (string.IsNullOrEmpty(json))
            {
                TempData["Error"] = "Please select your seats first";
                return RedirectToAction("SelectTicket", "Booking");
            }

            var bookingData = JsonSerializer.Deserialize<BookingSessionData>(json);
            TempData.Keep("BookingData");

            var showtime = db.ShowTimes
                             .Include(s => s.Movie)
                             .Include(s => s.Hall)
                             .ThenInclude(h => h.Outlet)
                             .FirstOrDefault(s => s.ShowTimeId == bookingData.ShowTimeId);

            if (showtime != null)
            {
                List<string> seating = bookingData.SelectedSeatIdentifiers;

                var seats = db.Seats.Where(s => seating.Contains(s.SeatIdentifier));

                var bookingDatas = new PurchaseVM
                {
                    ShowTimeId = showtime.ShowTimeId,
                    MovieTitle = showtime.Movie.Title,
                    MovieDuration = showtime.Movie.DurationMinutes,
                    StartTime = showtime.StartTime,
                    HallName = showtime.Hall.Name,
                    OutletName = showtime.Hall.Outlet.Name,

                    TicketPrice = showtime.TicketPrice,
                    TicketQuantity = seating.Count,
                    TicketSubtotal = showtime.TicketPrice * seating.Count,

                    SelectedSeatIdentifiers = seats.Select(s => s.SeatIdentifier).ToList(),
                };

                return View(bookingDatas);
            }
            else
            {
                return RedirectToAction("Index");
            }

        }
        else
        {
            return RedirectToAction("Index");
        }
        */

        return View();
    }

    public IActionResult Receipt()
    {
        return View();
    }
}
