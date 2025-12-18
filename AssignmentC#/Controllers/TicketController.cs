using AssignmentC_.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace AssignmentC_.Controllers;

public class TicketController(DB db) : Controller
{
    private const int TIME_LIMIT = 8 * 60;

    public IActionResult Checkout()
    {

        // Timer
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

        // Get all vouchers
        var vouchers = db.Vouchers.ToList();

        ViewBag.Vouchers = vouchers;


        

        return View(vm);
    }

    public IActionResult Purchase()
    {
        // Get Booking Data
        var json = TempData.Peek("BookingData") as string;

        if (json == null)
        {
            return RedirectToAction("SelectTicket", "Booking");
        }


        var bookingData = JsonSerializer.Deserialize<BookingSessionData>(json);
        
         var showtime = db.ShowTimes
                          .Include(s => s.Movie)
                          .Include(s => s.Hall)
                          .ThenInclude(h => h.Outlet)
                          .FirstOrDefault(s => s.ShowTimeId == bookingData.ShowTimeId);

            if (showtime != null)
            {
                List<string> seating = bookingData.SelectedSeatIdentifiers;

                var seats = db.Seats.Where(s => seating.Contains(s.SeatIdentifier));

                var bd = new PurchaseVM
                {
                    ShowTimeId = showtime.ShowTimeId,
                    MovieTitle = showtime.Movie.Title,
                    MoviewUrl = showtime.Movie.PosterUrl,
                    MovieDuration = showtime.Movie.DurationMinutes,
                    MovieRating = showtime.Movie.Rating,
                    StartTime = showtime.StartTime,
                    HallName = showtime.Hall.Name,
                    OutletCity = showtime.Hall.Outlet.City,
                    OutletName = showtime.Hall.Outlet.Name,

                    TicketPrice = showtime.TicketPrice,
                    TicketQuantity = seating.Count,
                    TicketSubtotal = showtime.TicketPrice * seating.Count,

                    SelectedSeatIdentifiers = seating.ToList(),
                };

                return View(bd);
            }
            else
            {
                return RedirectToAction("Index", "Movie");
            }
    }

    public IActionResult Voucher()
    {
        var vm = new VoucherViewModel
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(1),
        };
        return View(vm);
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
            MinSpend = model.MinSpend,
            CreatedTime = DateTime.Today
        };

        // Check voucher code exist
        bool exist = db.Promotions.OfType<Voucher>().Any(p => p.VoucherCode == voucher.VoucherCode);
        if (exist)
        {
            ModelState.AddModelError("VoucherCode", "Cannot have two or more Voucher with same code" );
        }

        // Check Voucher Type
        var type = voucher.VoucherType?.Trim();

        if (!string.Equals(type, "percentage", StringComparison.OrdinalIgnoreCase) && !string.Equals(type, "fixed", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("DiscountType", "The Voucher Type can only be Percentage or Fixed " + voucher.VoucherType);
        }
        else if (string.Equals(type, "percentage", StringComparison.OrdinalIgnoreCase))
        {
            if (voucher.DiscountValue > 100)
            {
                ModelState.AddModelError("DiscountValue", "Percentage discount must not exceed 100\"");
            }
        }

        // Check Start Date
        if (voucher.StartDate.Date < DateTime.Today)
        {
            ModelState.AddModelError("StartDate", "Date must start today or later");
        }

        // Check End Date
        if (voucher.EndDate < voucher.StartDate || voucher.EndDate > voucher.StartDate.AddDays(365))
        {
            ModelState.AddModelError("EndDate", "The maximum of end date is 365 days only");
        }

        // Check Eligibility Mode
        var mode = voucher.EligibilityMode.Trim();
        if (mode != "open" && mode != "condition" && mode != "assigned" && mode != "both")
        {
            ModelState.AddModelError("EligibilityMode", "Invalid Eligibility Mode");
        }

        // No Error
        if (ModelState.IsValid)
        {
            int promoId = voucher.PromotionId;
            var promo = db.Promotions.Find(promoId);

            // Condition
            if (model.EligibilityMode == "condition" || model.EligibilityMode == "both")
            {

                var condition = new VoucherCondition
                {
                    ConditionType = "CUSTOM",
                    MinAge = model.MinAge,
                    MaxAge = model.MaxAge,
                    IsFirstPurchase = model.IsFirstPurchase,
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

            db.Vouchers.Add(voucher);
            db.SaveChanges();
        }
        return View();
    }

    public IActionResult EditVoucher(int? id)
    {
        var p = db.Vouchers.Find(id);

        if(p == null)
        {
            return RedirectToAction("VoucherList");
        }

        var vm = new VoucherViewModel
        {
            PromotionId = p.PromotionId,
            VoucherCode = p.VoucherCode,
            DiscountType = p.VoucherType,
            DiscountValue = p.DiscountValue,
            EligibilityMode = p.EligibilityMode,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            MinSpend = p.MinSpend,
        };

        return View(vm);
    }

    [HttpPost]
    public IActionResult EditVoucher(VoucherViewModel model)
    {
        var v = db.Promotions.OfType<Voucher>().FirstOrDefault(v => v.PromotionId == model.PromotionId);

        if (v == null) 
        {
            return RedirectToAction("VoucherList");
        }

        // Check voucher code exist
        bool exist = db.Promotions.OfType<Voucher>().Any(p => p.VoucherCode == model.VoucherCode && p.PromotionId != model.PromotionId);
        if (exist)
        {
            return RedirectToAction("VoucherList");
        }

        // Check Voucher Type
        var type = model.DiscountType?.Trim();

        if (!string.Equals(type, "percentage", StringComparison.OrdinalIgnoreCase) && !string.Equals(type, "fixed", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("DiscountType", "The Voucher Type can only be Percentage or Fixed ");
        }
        else if (string.Equals(type, "percentage", StringComparison.OrdinalIgnoreCase))
        {
            if (model.DiscountValue > 100)
            {
                ModelState.AddModelError("DiscountValue", "Percentage discount must not exceed 100\"");
            }
        }

        // Check Start Date
        if (model.StartDate.Date < DateTime.Today)
        {
            ModelState.AddModelError("StartDate", "Date must start today or later");
        }

        // Check End Date
        if (model.EndDate < model.StartDate || model.EndDate > model.StartDate.AddDays(365))
        {
            ModelState.AddModelError("EndDate", "The maximum of end date is 365 days only");
        }

        // Check Eligibility Mode
        var mode = model.EligibilityMode.Trim();
        if (mode != "open" && mode != "condition" && mode != "assigned" && mode != "both")
        {
            ModelState.AddModelError("EligibilityMode", "Invalid Eligibility Mode");
        }

        // With Error
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        v.VoucherCode = model.VoucherCode;
        v.VoucherType = model.DiscountType;
        v.DiscountValue = model.DiscountValue;
        v.EligibilityMode = model.EligibilityMode;
        v.StartDate = model.StartDate;
        v.EndDate = model.EndDate;
        v.MinSpend = model.MinSpend;
        
        db.SaveChanges();
        return RedirectToAction("VoucherList");
    }

    public IActionResult DeleteVoucher(int? id)
    {
        var v = db.Promotions.OfType<Voucher>().FirstOrDefault(v => v.PromotionId == id);

        if (v != null)
        {
            db.Promotions.Remove(v);
            db.SaveChanges();
        }

        return Redirect(Request.Headers.Referer.ToString());
    }

    public IActionResult VoucherList(string? search, string? type, string? status)
    {
        search = search?.Trim() ?? "";

        var v = db.Promotions
                  .OfType<Voucher>()
                  .Where(v => v.VoucherCode.Contains(search));

        if(status != null)
        {
            switch (status)
            {
                case "active":
                    v = db.Promotions
                          .OfType<Voucher>().Where(v => v.StartDate > DateTime.Today);
                break;
            
                case "expired":
                    v = db.Promotions
                          .OfType<Voucher>().Where(v => v.EndDate < DateTime.Today);
                break;
            }
        }

        if (type != null)
        {
            v = db.Promotions
                  .OfType<Voucher>()
                  .Where(v => v.VoucherType == type);
        }

        var vouchers = v.Select(v => new VoucherViewModel
                        {
                            PromotionId = v.PromotionId,
                            VoucherCode = v.VoucherCode,
                            DiscountType = v.VoucherType,
                            DiscountValue = v.DiscountValue,
                            StartDate = v.StartDate,
                            EndDate = v.EndDate,
                        })
                        .ToList();

        

        if (Request.IsAjax())
        {
            return PartialView("_VoucherList", vouchers);
        }

        return View(vouchers);
    }

    [HttpPost]
    public IActionResult ApplyVoucher(string? SelectedVoucher)
    {
        if(SelectedVoucher == null)
        {
            HttpContext.Session.Remove("AppliedVoucherCode");
            return PartialView("_AppliedVoucher");
        }

        var voucher = db.Promotions
                        .OfType<Voucher>()
                        .FirstOrDefault(v => v.VoucherCode == SelectedVoucher); // May check date here

        if(voucher == null)
        {
            HttpContext.Session.Remove("AppliedVoucherCode");
            return PartialView("_AppliedVoucher");
        }
        else
        {
            HttpContext.Session.SetString("AppliedVoucherCode", voucher.VoucherCode);
            return PartialView("_AppliedVoucher", voucher);
        }
    }

    public IActionResult Receipt()
    {
        return View();
    }
}
