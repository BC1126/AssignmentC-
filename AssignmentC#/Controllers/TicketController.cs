using AssignmentC_.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace AssignmentC_.Controllers;

public class TicketController(DB db, Helper hp) : Controller
{
    [Authorize(Roles = "Member")]
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

        // Get all vouchers
        var today = DateTime.Today;
        var vouchers = db.Promotions
                         .OfType<Voucher>()
                         .Where(v => v.StartDate <= today && v.EndDate >= today
                         )
                         .ToList();
        ViewBag.Timer = new CheckoutViewModel { Timer = timer };
        ViewBag.Vouchers = vouchers;
        return View(purchaseVM);
    }

    [Authorize(Roles = "Member")]
    [HttpPost]
    public IActionResult Checkout(PurchaseVM vm)
    {
        ModelState.Clear();

        if (vm.PaymentMethod == null)
        {
            ModelState.AddModelError("", "Please select a payment method.");
        }

        if (vm.PaymentMethod == "Card")
        {
            // Check card number
            if (vm.CardNumber == null)
            { 
                ModelState.AddModelError("CardNumber", "Card number is required.");
            }
            else if (!Regex.IsMatch(vm.CardNumber, @"^\d{16}$"))
            {
                ModelState.AddModelError("CardNumber", "Please enter a valid Card Number");
            }
            else if (vm.CardNumber.Length != 16)
            {
                ModelState.AddModelError("CardNumber", "CardNumber must be exactly 16 characters.");
            }

            // Check Expiry Date
            
            if (vm.ExpiryDate == null)
            {
                ModelState.AddModelError("ExpiryDate", "Expiry date is required.");
            }
            else
            {
                var parts = vm.ExpiryDate.Split('-');
                int year = int.Parse(parts[0]);
                int month = int.Parse(parts[1]);

                var expiry = new DateTime(year, month, DateTime.DaysInMonth(year, month));

                if (expiry < DateTime.Today)
                {
                    ModelState.AddModelError("ExpiryDate", "Credit card has expired.");
                }
            }

            // Check CVV
            if (vm.CVV == null)
            {
                ModelState.AddModelError("CVV", "CVV is required.");
            }
            else if (!int.TryParse(vm.CVV, out int value))
            {
                ModelState.AddModelError("CVV", "Please enter a valid CVV");
            }
            else if (vm.CVV.Length != 3)
            {
                ModelState.AddModelError("CVV", "CardNumber must be exactly 3 characters.");
            }
        }
        else if (vm.PaymentMethod == "EWallet")
        {
            if (string.IsNullOrWhiteSpace(vm.PhoneNumber))
            {
                ModelState.AddModelError("PhoneNumber", "Phone number is required.");
            }
            else if (!Regex.IsMatch(vm.PhoneNumber, @"^60\d{9,10}$"))
            {
                ModelState.AddModelError("PhoneNumber", "Phone number must start with 60 and be 11 or 12 digits long.");
            }

            if (string.IsNullOrWhiteSpace(vm.Pin))
            {
                ModelState.AddModelError("Pin", "PIN is required.");
            }
            else if (!int.TryParse(vm.Pin, out int value))
            {
                ModelState.AddModelError("Pin", "Please enter a valid PIN number.");
            }
            else if (vm.Pin.Length != 6)
            {
                ModelState.AddModelError("Pin", "CardNumber must be exactly 6 characters.");
            }
        }

        if (!ModelState.IsValid)
        {
            return RedirectToAction("Index");
        }

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

        decimal orderSub = 0;
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

            orderSub += (p.Price * quantity);

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
        decimal amount = 0;
        if (a != null)
        {
           amount  = decimal.Parse(a);
        }
        else
        {
            amount = data.TicketSubtotal + orderSub;
        }


        if(v != null)
        {
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
            return RedirectToAction("Receipt", new { id = payment.PaymentId });
        }
        else
        {
            var payment = new Payment
            {
                amount = amount,
                status = "Paid",
                date = DateOnly.FromDateTime(DateTime.Today),

                Booking = finalBooking,
                Order = order,
                User = user,
            };
            db.Payments.Add(payment);
            db.SaveChanges();
            return RedirectToAction("Receipt", new { id = payment.PaymentId }); ;
        }
    }

    [Authorize(Roles = "Member")]
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

    [Authorize(Roles = "Admin")]
    public IActionResult Voucher()
    {
        var vm = new VoucherViewModel
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(1),
        };
        return View(vm);
    }

    [Authorize(Roles = "Admin")]
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

    [Authorize(Roles = "Admin")]
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

    [Authorize(Roles = "Admin")]
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

    [Authorize(Roles = "Admin")]
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

    [Authorize(Roles = "Admin")]
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
                case "schedule":
                    v = db.Promotions
                          .OfType<Voucher>().Where(v => v.StartDate > DateTime.Today);
                break;
            
                case "expired":
                    v = db.Promotions
                          .OfType<Voucher>().Where(v => v.EndDate < DateTime.Today);
                break;

                case "active":
                    v = db.Promotions
                          .OfType<Voucher>().Where(v => v.StartDate <= DateTime.Today && v.EndDate >= v.EndDate);
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

    [Authorize(Roles = "Member")]
    [HttpPost]
    public IActionResult ApplyVoucher(string SelectedVoucher)
    {
        if(SelectedVoucher == null)
        {
            HttpContext.Session.Remove("AppliedVoucherCode");
            HttpContext.Session.Remove("VoucherError");
            return PartialView("_AppliedVoucher");
        }

        var voucher = db.Promotions
                        .OfType<Voucher>()
                        .FirstOrDefault(v => v.VoucherCode == SelectedVoucher); // May check date here

        if(voucher == null)
        {
            HttpContext.Session.Remove("AppliedVoucherCode");
            HttpContext.Session.Remove("VoucherError");
            return PartialView("_AppliedVoucher");
        }
        else
        {
            var subtotalStr = HttpContext.Session.GetString("Subtotal");
            decimal subtotal = 0;
            if (subtotalStr != null)
            {
                subtotal = decimal.Parse(subtotalStr);
            }

            if (subtotal < voucher.MinSpend)
            {
                HttpContext.Session.Remove("AppliedVoucherCode");
                HttpContext.Session.SetString("VoucherError", $"Minimum spend RM {voucher.MinSpend:F2} required.");
                return PartialView("_AppliedVoucher");
            }

            var today = DateTime.Today;

            if (voucher.StartDate > today || voucher.EndDate < today)
            {
                HttpContext.Session.Remove("AppliedVoucherCode");
                HttpContext.Session.SetString("VoucherError", "This voucher is expired.");
                return PartialView("_AppliedVoucher");
            }

            HttpContext.Session.SetString("AppliedVoucherCode", voucher.VoucherCode);
            HttpContext.Session.Remove("VoucherError");
            return PartialView("_AppliedVoucher", voucher);
        }
    }

    [Authorize(Roles = "Member")]
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

        decimal sub = ticketSubtotal + addOn;
        HttpContext.Session.SetString("Subtotal", sub.ToString("0.00"));
        HttpContext.Session.SetString("TotalAmount", total.ToString("0.00"));

        return PartialView("_DiscountSummary", vm);
    }

    [Authorize(Roles = "Member")]
    public IActionResult Receipt(int id)
    {
        var email = User.Identity!.Name!;

        if (email == null)
        {
            return RedirectToAction("Index", "Home");
        }

        var vm = db.Payments
                   .Include(p => p.Promotions)
                   .Include(p => p.User)
                   .Include(p => p.Booking)
                   .ThenInclude(p => p.ShowTime)
                   .ThenInclude(p => p.Movie)

                   .Include(p => p.Booking)
                   .ThenInclude(p => p.ShowTime)
                   .ThenInclude(p => p.Hall)

                   .Include(p => p.Order)
                   .Where(p => p.User.Email == email && p.PaymentId == id)
                   .Select(p => new PaymentVM
                   {
                       PaymentId = p.PaymentId,
                       Amount = p.amount,
                       Status = p.status,
                       Date = p.date,

                       Promotions = p.Promotions.ToList(),
                       Booking = p.Booking,
                       User = p.User,
                       Order = p.Order,
                       ShowTime = p.Booking.ShowTime,
                       Movie = p.Booking.ShowTime.Movie,
                   })
                  .FirstOrDefault();

        if (vm == null)
        {
            return RedirectToAction("Index", "Home");
        }

        // Get Seat
        var s = db.BookingSeats
                     .Include(p => p.Booking)
                     .Include(p => p.Seat)
                     .Where(p => p.BookingId == vm.Booking.BookingId)
                     .Select(p => p.Seat.SeatIdentifier)
                     .ToList();

        ViewBag.Seats = s;

        // Get Add On
        var ol = db.OrderLines
                    .Include(o => o.Order)
                    .Where(o => o.OrderId == vm.Order.Id)
                    .ToList();

        decimal addOnSub = 0;

        if (ol != null)
        {
            foreach (var o in ol)
            {
                addOnSub += o.Price * o.Quantity; 
            }
        }

        ViewBag.AddOnSub = addOnSub;

        // Get Promo Use
        decimal subtotal = addOnSub + vm.Booking.TotalPrice;

        decimal dv = 0;

        foreach (var p in vm.Promotions ?? Enumerable.Empty<Promotion>())
        {
            if (p is Voucher v)
            {
                if (string.Equals(v.VoucherType.Trim(), "percentage", StringComparison.OrdinalIgnoreCase))
                {
                    decimal d = v.DiscountValue / 100;
                    dv = subtotal * d;
                }
                else
                {
                    dv = v.DiscountValue;
                }
            }
        }

        ViewBag.Sub = dv;

        return View(vm);
    }

    [Authorize(Roles = "Member")]
    public async Task<IActionResult> DownloadReceipt(int paymentId)
    {
        var payment = db.Payments
                   .Include(p => p.Promotions)
                   .Include(p => p.User)
                   .Include(p => p.Booking)
                   .ThenInclude(p => p.ShowTime)
                   .ThenInclude(p => p.Movie)

                   .Include(p => p.Booking)
                   .ThenInclude(p => p.ShowTime)
                   .ThenInclude(p => p.Hall)

                   .Include(p => p.Order)
                   .Where(p => p.PaymentId == paymentId)
                   .Select(p => new PaymentVM
                   {
                       PaymentId = p.PaymentId,
                       Amount = p.amount,
                       Status = p.status,
                       Date = p.date,

                       Promotions = p.Promotions.ToList(),
                       Booking = p.Booking,
                       User = p.User,
                       Order = p.Order,
                       ShowTime = p.Booking.ShowTime,
                       Movie = p.Booking.ShowTime.Movie,
                   })
                  .FirstOrDefault();

        if (payment == null)
        {
            return RedirectToAction("Index", "Home");
        }

        var document = new ReceiptDocument(db,payment);

        // 3. Generate PDF as byte array
        var pdfBytes = document.GeneratePdf();

        // 4. Return as FileResult
        return File(pdfBytes, "application/pdf", $"Receipt_{paymentId}.pdf");
    }

    [Authorize(Roles = "Member")]
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

    [Authorize(Roles = "Member")]
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

    [Authorize(Roles = "Admin")]
    public IActionResult PaymentList()
    {
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
                        .ToList();

        return View(payment);
    }

    [Authorize(Roles = "Admin")]
    public IActionResult DeletePayment(List<int> selectedID)
    {
        if (selectedID == null || !selectedID.Any())
        {
            TempData["Error"] = "No items selected";
            return RedirectToAction("PaymentList");
        }

        var items = db.Payments
                      .Where(p => selectedID.Contains(p.PaymentId))
                      .ToList();

        if (items.Any())
        {
            db.Payments.RemoveRange(items);
            db.SaveChanges();
        }

        TempData["Success"] = $"{items.Count} item(s) deleted.";
        return RedirectToAction("PaymentList");
    }

    [Authorize(Roles = "Member")]
    [HttpPost]
    public IActionResult Refund(int paymentId)
    {
        var payment = db.Payments
                        .Include(p => p.User)
                        .Include(p => p.Booking)
                        .FirstOrDefault(p => p.PaymentId == paymentId);

        if (payment == null)
        {
            return RedirectToAction("Index");
        }

        payment.status = "Refund";
        payment.date = DateOnly.FromDateTime(DateTime.Today);
        db.SaveChanges();


        // Send Email
        string body = $@"
        <h2>Your Ticket Has been successfully cancelled</h2>
        <p>Dear {payment.User.Name},</p>
        <p>Your refund has been issued. This make take 5 to 15 days to show in your bank account.</p><br>
        <p>Thank you for your patient</p>

        <ul>
            <li><strong>Payment ID:</strong> {payment.PaymentId}</li>
            <li><strong>Refund Amount:</strong> RM {payment.amount:F2}</li>
        </ul>

        <p>The amount will be returned to your original payment method.</p>
        <p>Thank you.</p>
    ";

        try
        {
            var mail = new System.Net.Mail.MailMessage
            {
                Subject = "Verify Your Email",
                Body = body,
                IsBodyHtml = true
            };
            mail.To.Add(User.Identity!.Name!);
            hp.SendEmail(mail);
        }
        catch (Exception ex)
        {
            TempData["Error"] = "User registered, but email failed: " + ex.Message;
        }

        // Set database
        //var booking = payment.Booking;
        //db.Bookings.Remove(booking);
        //db.SaveChanges();

        return RedirectToAction("MyTicket");
    }
}
