using AssignmentC_.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace AssignmentC_.Controllers;

public class TicketController(DB db, Helper hp) : Controller
{
    private const int TIME_LIMIT = 8 * 60;

    public IActionResult Checkout()
    {
        // Get PurchaseVM from session
        var session = HttpContext.Session.GetString("PurchaseVM");
        var expiryStr = HttpContext.Session.GetString("LockExpiry");
        if (session == null)
        {
            return RedirectToAction("", "Home");
        }

        var purchaseVM = JsonSerializer.Deserialize<PurchaseVM>(session);

        if (string.IsNullOrEmpty(expiryStr))
        {
            return RedirectToAction("SelectTicket", "Booking");
        }

        // 3. Calculate EXACT remaining time based on the lock in the database
        DateTime expiryTime = DateTime.Parse(expiryStr, null, System.Globalization.DateTimeStyles.RoundtripKind);
        int remainingSeconds = (int)(expiryTime - DateTime.UtcNow).TotalSeconds;

        // 4. Create the timer model
        var timer = new TimerViewModel
        {
            Expired = remainingSeconds <= 0,
            Minutes = Math.Max(remainingSeconds, 0) / 60,
            Seconds = Math.Max(remainingSeconds, 0) % 60
        };

        if (Request.IsAjax())
        {
            return PartialView("_Timer", timer);
        }

        ViewBag.Timer = new CheckoutViewModel { Timer = timer };
        ViewBag.Vouchers = db.Vouchers.ToList();

        return View(purchaseVM);
    }

    [HttpPost]
    public IActionResult Checkout(Payment pm)
    {
        var voucherCode = HttpContext.Session.GetString("AppliedVoucherCode");

        var v = db.Promotions.OfType<Voucher>().FirstOrDefault(v => v.VoucherCode == voucherCode);
        List<Promotion> promo = new List<Promotion>();
        promo.Add(v);

        // Get Cart in Order
        var cart = hp.GetCart();
        if (!cart.Any())
        {
            TempData["Error"] = "Your cart is empty.";
            return RedirectToAction("ShoppingCart");
        }

        var region = HttpContext.Session.GetString("SelectedRegion");
        var cinema = HttpContext.Session.GetString("SelectedCinema");
        var collectDateStr = HttpContext.Session.GetString("CollectDate");

        if (region == null || cinema == null || collectDateStr == null)
        {
            TempData["Error"] = "Please select region, cinema, and collect date first.";
            return RedirectToAction("UserSelectRegion");
        }

        var order = new Order
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            Paid = true,
            Region = region,
            Cinema = cinema,
            CollectDate = DateOnly.Parse(collectDateStr),
            Claim = false,
            MemberEmail = User.Identity!.Name!
        };

        db.Orders.Add(order);

        foreach (var (productId, quantity) in cart)
        {
            var p = db.Products.Find(productId);
            if (p == null) continue;

            // Stock validation
            if (quantity > p.Stock)
            {
                TempData["Error"] = $"Not enough stock for {p.Name}. Available: {p.Stock}";
                return RedirectToAction("ShoppingCart","Product");
            }

            // Reduce stock
            p.Stock -= quantity;

            // Add order line
            order.OrderLines.Add(new OrderLine
            {
                ProductId = productId,
                ProductName = p.Name,
                ProductPhotoURL = p.PhotoURL,
                Price = p.Price,
                Quantity = quantity
            });

        }

        db.SaveChanges();
        hp.SetCart();

        // Order End
        // -------------------------------------------------

        // Get User

        var user = db.Users.FirstOrDefault(v => v.Email == User.Identity!.Name!);
        if (user == null)
        {
            return RedirectToAction("Login", "User");
        }


        // Get Booking
        var sessionJson = HttpContext.Session.GetString("BookingData");
        var data = JsonSerializer.Deserialize<BookingSessionData>(sessionJson);

        if (data == null)
        {
            return RedirectToAction("Index", "Movie");
        }

        var finalBooking = new Booking
        {
            MemberId = data.MemberId,
            ShowTimeId = data.ShowTimeId,
            TicketQuantity = data.TicketQuantity,
            TotalPrice = data.TicketSubtotal,
            BookingDate = DateTime.Now,
            BookingSeats = data.SelectedSeatIds.Select(sid => new BookingSeat
            {
                SeatId = sid
            }).ToList()
        };

        db.Bookings.Add(finalBooking);

        var a = HttpContext.Session.GetString("TotalAmount");
        decimal amount = decimal.Parse(a);

        var payment = new Payment
        {
            amount = amount,
            status = "Paid",
            date = DateOnly.FromDateTime(DateTime.Today),

            Booking = finalBooking,
            Promotions = promo,
            Order = order,
            User = user,
        };

        db.Payments.Add(payment);
        db.SaveChanges();

        return RedirectToAction("Receipt", payment); ;
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
                //Get Cart
                var cart = hp.GetCart();
                decimal subtotal = 0;

                List<string> OrderNames = new List<string>();
                List<int> OrderQuantitys = new List<int>();
                List<decimal> OrderPrice = new List<decimal>();

                foreach (var (productId, quantity) in cart)
                {

                    var p = db.Products.Find(productId);
                    if (p == null) continue;

                    // Stock validation
                    if (quantity > p.Stock)
                    {
                        TempData["Error"] = $"Not enough stock for {p.Name}. Available: {p.Stock}";
                        return RedirectToAction("ShoppingCart","Product");
                    }

                    OrderNames.Add(p.Name);
                    OrderQuantitys.Add(quantity);
                    OrderPrice.Add(p.Price);

                    // Reduce stock
                    p.Stock -= quantity;

                    // Add order line
                
                    subtotal += (p.Price * quantity);
                }

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

                        OrderName = OrderNames,
                        OrderQuantity = OrderQuantitys,
                        OrderPrice = OrderPrice,
                        OrderSubtotal = subtotal,

                        TicketPrice = showtime.TicketPrice,
                        TicketQuantity = seating.Count,
                        TicketSubtotal = showtime.TicketPrice * seating.Count,

                        SelectedSeatIdentifiers = seating.ToList(),
                        total = subtotal + (showtime.TicketPrice * seating.Count),
                };

                var tempDate = JsonSerializer.Serialize(bd);
                HttpContext.Session.SetString("PurchaseVM", tempDate);

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

    [HttpPost]
    public IActionResult OrderSummary(decimal subtotal, decimal ticketSubtotal, int quantity, decimal addOn)
    {
        var voucherCode = HttpContext.Session.GetString("AppliedVoucherCode");

        var vc = db.Vouchers.FirstOrDefault(v => v.VoucherCode == voucherCode);
        decimal dv = 0;
        decimal total = 0;

        if(vc == null)
        {
            total = subtotal;
        }
        else
        {
            if (string.Equals(vc.VoucherType.Trim(), "percentage", StringComparison.OrdinalIgnoreCase))
            {
                decimal d = vc.DiscountValue / 100;
                dv = subtotal * d;
                total = subtotal - dv;
            }
            else
            {
                dv = vc.DiscountValue;
                total = subtotal - dv;
            }
        }

        var vm = new PurchaseVM
        {
            TicketSubtotal = ticketSubtotal,
            TicketQuantity = quantity,
            OrderSubtotal = addOn,
            DiscountAmount = dv,
            TotalAmount = total,
        };

        HttpContext.Session.SetString("TotalAmount", total.ToString("0.00"));

        return PartialView("_DiscountSummary", vm);
    }
    public IActionResult Receipt()
    {
        return View();
    }

    public IActionResult MyTicket()
    {
        var email = User.Identity!.Name!;
        var user = db.Users.FirstOrDefault(u => u.Email == email);

        if (user == null)
        {
            return RedirectToAction("Login", "User");
        }

        var vm = db.Payments
                  .Include(p => p.User)
                  .Include(p => p.Booking)
                  .ThenInclude(b => b.ShowTime)
                  .ThenInclude(p => p.Movie)
                  .Include(p => p.Order)
                  .Where(p => p.User.UserId == user.UserId)
                  .Select(p => new PaymentVM
                  {
                      PaymentId = p.PaymentId,
                      Amount = p.amount,
                      Status = p.status,
                      Date = p.date,

                      Booking = p.Booking,
                      User = p.User,
                      Order = p.Order,
                      ShowTime = p.Booking.ShowTime,
                      Movie = p.Booking.ShowTime.Movie,
                  })
                  .ToList();

        return View(vm);
    }

    public IActionResult TicketDetail(int? id)
    {
        if(id == null)
        {
            return RedirectToAction("Login", "User");
        }

        var payment = db.Payments
                        .Include(p => p.User)
                        .Include(p => p.Booking)
                        .ThenInclude(p => p.ShowTime)
                        .ThenInclude(p => p.Hall)
                        .ThenInclude(p => p.Outlet)

                        .Include(p => p.Booking)
                        .ThenInclude(p => p.ShowTime)
                        .ThenInclude(p => p.Movie)

                        .Include(p => p.Order)
                        .FirstOrDefault(p => p.PaymentId == id);

        

        if (payment == null)
        {
            return RedirectToAction("Index", "Home");
        }

        var b = payment.Booking.BookingId;
        var s = db.BookingSeats
                     .Include(p => p.Booking)
                     .Include(p => p.Seat)
                     .Where(p => p.BookingId == b)
                     .Select(p => p.Seat.SeatIdentifier)
                     .ToList();

        ViewBag.Seats = s;

        return View(payment);
    }

    
}
