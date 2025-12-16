using AssignmentC_.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AssignmentC_.Controllers;

public class TicketController(DB db) : Controller
{
    private const int TIME_LIMIT = 1 * 60;

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

    [HttpGet]
    public IActionResult Voucher()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Voucher(VoucherViewModel model)
    {
        var voucher = new Voucher
        {
            VoucherCode = model.VoucherCode,
            VoucherType = model.DiscountType,
            DiscountValue = model.DiscountValue,
            EligibilityMode = model.EligibilityMode,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            CreatedTime = DateTime.Today
        };

        db.Voucher.Add(voucher);
        db.SaveChanges();

        int promoId = voucher.PromotionId;
        var promo = db.Promotions.Find(promoId);

        // Condition
        if (model.EligibilityMode == "condition" || model.EligibilityMode == "both")
        {

            var condition = new VoucherCondition
            {
                promotion = promo,
                ConditionType = "CUSTOM",
                MinAge = model.MinAge,
                MaxAge = model.MaxAge,
                MinSpend = model.MinSpend,
                IsFirstPurchase  = model.IsFirstPurchase,
                BirthMonth = model.BirthMonth ?? new System.Collections.Generic.List<int>(),
            };

            db.VoucherConditions.Add(condition);
            db.SaveChanges();
        }
        // Assigned User
        else if (model.EligibilityMode == "assigned" || model.EligibilityMode == "both")
        {
            // Change user here
            var user = db.Users.Find(model.AssignedUserId);

            var assignment = new VoucherAssignment
            {
                promotion = promo,
                user = user,
            };

            db.VoucherAssignments.Add(assignment);
            db.SaveChanges();
        }

        ViewBag.Message = "Voucher Created Successfully";
        return View();
    }

    public IActionResult VoucherList()
    {
        var vouchers = db.Promotions
        .OfType<Voucher>()
        .Select(v => new VoucherViewModel
        {
            PromotionId = v.PromotionId,
            VoucherCode = v.VoucherCode,
            DiscountType = v.VoucherType,
            DiscountValue = v.DiscountValue,
            StartDate = v.StartDate,
            EndDate = v.EndDate,
        })
        .ToList();
        return View(vouchers);
    }

    public IActionResult Receipt()
    {
        return View();
    }
}
