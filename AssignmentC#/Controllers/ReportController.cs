using AssignmentC_;
using AssignmentC_.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using X.PagedList;
using X.PagedList.Extensions;
using X.PagedList.Mvc.Core;

namespace AssignmentC_;

public class ReportController(DB db, Helper hp) : Controller
{


    [Authorize(Roles = "Admin")]
    // Product report
    public IActionResult ProductReport(string search, string sort = "Name", string dir = "asc", int page = 1)
    {
        var products = db.Products
            .Select(p => new ProductReportVM
            {
                ProductId = p.Id, // use correct property name
                Name = p.Name,
                Cinema = p.Cinema,
                Price = p.Price,
                TotalQuantitySold = db.OrderLines.Where(ol => ol.ProductId == p.Id).Sum(ol => (int?)ol.Quantity) ?? 0,
                TotalRevenue = db.OrderLines.Where(ol => ol.ProductId == p.Id).Sum(ol => (decimal?)(ol.Quantity * ol.Price)) ?? 0
            }).AsQueryable();

        if (!string.IsNullOrEmpty(search))
            products = products.Where(p => p.Name.Contains(search) || p.Cinema.Contains(search));

        products = (sort, dir.ToLower()) switch
        {
            ("Name", "asc") => products.OrderBy(p => p.Name),
            ("Name", "desc") => products.OrderByDescending(p => p.Name),
            ("TotalQuantitySold", "asc") => products.OrderBy(p => p.TotalQuantitySold),
            ("TotalQuantitySold", "desc") => products.OrderByDescending(p => p.TotalQuantitySold),
            ("TotalRevenue", "asc") => products.OrderBy(p => p.TotalRevenue),
            ("TotalRevenue", "desc") => products.OrderByDescending(p => p.TotalRevenue),
            _ => products.OrderBy(p => p.Name)
        };

        var model = products.ToList().ToPagedList(page, 10);

        ViewBag.Search = search;
        ViewBag.Sort = sort;
        ViewBag.Dir = dir;

        if (Request.IsAjax())
        {
            return PartialView("_ProductReportTable", model);
        }

        return View("~/Views/Report/ProductReport.cshtml", model);
    }

    [Authorize(Roles = "Admin")]
    public IActionResult SalesChart(
    string viewBy = "day",
    DateTime? date1 = null,
    DateTime? date2 = null
)
    {
        ViewBag.ViewBy = viewBy;

        DateTime d1 = date1 ?? DateTime.Today.AddDays(-1);
        DateTime d2 = date2 ?? DateTime.Today;

        var q = db.OrderLines
            .Include(x => x.Order)
            .AsQueryable();

        decimal total1 = 0;
        decimal total2 = 0;
        string label1 = "";
        string label2 = "";

        if (viewBy == "day")
        {
            var dd1 = DateOnly.FromDateTime(d1);
            var dd2 = DateOnly.FromDateTime(d2);

            total1 = q.Where(x => x.Order.Date == dd1)
                      .Sum(x => (decimal?)(x.Price * x.Quantity)) ?? 0;

            total2 = q.Where(x => x.Order.Date == dd2)
                      .Sum(x => (decimal?)(x.Price * x.Quantity)) ?? 0;

            label1 = d1.ToString("yyyy-MM-dd");
            label2 = d2.ToString("yyyy-MM-dd");
        }
        else if (viewBy == "month")
        {
            total1 = q.Where(x =>
                        x.Order.Date.Year == d1.Year &&
                        x.Order.Date.Month == d1.Month)
                      .Sum(x => (decimal?)(x.Price * x.Quantity)) ?? 0;

            total2 = q.Where(x =>
                        x.Order.Date.Year == d2.Year &&
                        x.Order.Date.Month == d2.Month)
                      .Sum(x => (decimal?)(x.Price * x.Quantity)) ?? 0;

            label1 = d1.ToString("yyyy-MM");
            label2 = d2.ToString("yyyy-MM");
        }
        else // year
        {
            total1 = q.Where(x => x.Order.Date.Year == d1.Year)
                      .Sum(x => (decimal?)(x.Price * x.Quantity)) ?? 0;

            total2 = q.Where(x => x.Order.Date.Year == d2.Year)
                      .Sum(x => (decimal?)(x.Price * x.Quantity)) ?? 0;

            label1 = d1.Year.ToString();
            label2 = d2.Year.ToString();
        }

        ViewBag.Date1 = d1.ToString("yyyy-MM-dd");
        ViewBag.Date2 = d2.ToString("yyyy-MM-dd");

        ViewBag.ChartData = System.Text.Json.JsonSerializer.Serialize(new[]
        {
        new { Label = label1, TotalSales = total1 },
        new { Label = label2, TotalSales = total2 }
    });

        return View();
    }





    // Helper method to group sales
    private List<SalesChartVM> GroupSales(List<OrderLine> sales, string viewBy)
    {
        switch (viewBy.ToLower())
        {
            case "year":
                return sales
                    .GroupBy(ol => ol.Order.Date.Year)
                    .Select(g => new SalesChartVM
                    {
                        Label = g.Key.ToString(),
                        TotalSales = g.Sum(x => x.Price * x.Quantity)
                    })
                    .OrderBy(d => d.Label)
                    .ToList();

            case "month":
                return sales
                    .GroupBy(ol => new { ol.Order.Date.Year, ol.Order.Date.Month })
                    .Select(g => new SalesChartVM
                    {
                        Label = $"{g.Key.Year}-{g.Key.Month:00}",
                        TotalSales = g.Sum(x => x.Price * x.Quantity)
                    })
                    .OrderBy(d => d.Label)
                    .ToList();

            default: // day
                return sales
                    .GroupBy(ol => ol.Order.Date)
                    .Select(g => new SalesChartVM
                    {
                        Label = g.Key.ToString("yyyy-MM-dd"),
                        TotalSales = g.Sum(x => x.Price * x.Quantity)
                    })
                    .OrderBy(d => d.Label)
                    .ToList();
        }
    }

    [Authorize(Roles = "Admin")]
    public IActionResult MovieTicketReport(
    string search,
    string sort = "Title",
    string dir = "asc",
    int page = 1)
    {
        var query =
            from b in db.Bookings
            join st in db.ShowTimes on b.ShowTimeId equals st.ShowTimeId
            join m in db.Movies on st.MovieId equals m.MovieId
            group new { b, m } by new { m.MovieId, m.Title } into g
            select new MovieTicketReportVM
            {
                MovieId = g.Key.MovieId,
                Title = g.Key.Title,
                TotalTicketsSold = g.Sum(x => x.b.TicketQuantity),
                TotalRevenue = g.Sum(x => x.b.TotalPrice)
            };

        if (!string.IsNullOrEmpty(search))
            query = query.Where(x => x.Title.Contains(search));

        query = (sort, dir.ToLower()) switch
        {
            ("Title", "desc") => query.OrderByDescending(x => x.Title),
            ("TotalTicketsSold", "asc") => query.OrderBy(x => x.TotalTicketsSold),
            ("TotalTicketsSold", "desc") => query.OrderByDescending(x => x.TotalTicketsSold),
            ("TotalRevenue", "asc") => query.OrderBy(x => x.TotalRevenue),
            ("TotalRevenue", "desc") => query.OrderByDescending(x => x.TotalRevenue),
            _ => query.OrderBy(x => x.Title)
        };

        var model = query.ToList().ToPagedList(page, 10);

        ViewBag.Search = search;
        ViewBag.Sort = sort;
        ViewBag.Dir = dir;

        if (Request.IsAjax())
            return PartialView("_MovieTicketReportTable", model);

        return View("~/Views/Report/MovieTicketReport.cshtml", model);
    }

    [Authorize(Roles = "Admin")]
    public IActionResult MovieTicketSalesChart(
    string viewBy = "day",
    DateTime? date1 = null,
    DateTime? date2 = null
)
    {
        ViewBag.ViewBy = viewBy;

        DateTime d1 = date1 ?? DateTime.Today.AddDays(-1);
        DateTime d2 = date2 ?? DateTime.Today;

        var q = db.Bookings
            .Include(b => b.ShowTime)
            .ThenInclude(st => st.Movie)
            .AsQueryable();

        decimal total1 = 0;
        decimal total2 = 0;
        string label1 = "";
        string label2 = "";

        if (viewBy == "day")
        {
            var dd1 = DateOnly.FromDateTime(d1);
            var dd2 = DateOnly.FromDateTime(d2);

            total1 = q.Where(x => x.BookingDate.Date == d1.Date)
          .Sum(x => (decimal?)x.TotalPrice) ?? 0;

            total2 = q.Where(x => x.BookingDate.Date == d2.Date)
                      .Sum(x => (decimal?)x.TotalPrice) ?? 0;

            label1 = d1.ToString("yyyy-MM-dd");
            label2 = d2.ToString("yyyy-MM-dd");
        }
        else if (viewBy == "month")
        {
            total1 = q.Where(x =>
                        x.BookingDate.Year == d1.Year &&
                        x.BookingDate.Month == d1.Month)
                      .Sum(x => (decimal?)x.TotalPrice) ?? 0;

            total2 = q.Where(x =>
                        x.BookingDate.Year == d2.Year &&
                        x.BookingDate.Month == d2.Month)
                      .Sum(x => (decimal?)x.TotalPrice) ?? 0;

            label1 = d1.ToString("yyyy-MM");
            label2 = d2.ToString("yyyy-MM");
        }
        else // year
        {
            total1 = q.Where(x => x.BookingDate.Year == d1.Year)
                      .Sum(x => (decimal?)x.TotalPrice) ?? 0;

            total2 = q.Where(x => x.BookingDate.Year == d2.Year)
                      .Sum(x => (decimal?)x.TotalPrice) ?? 0;

            label1 = d1.Year.ToString();
            label2 = d2.Year.ToString();
        }

        ViewBag.Date1 = d1.ToString("yyyy-MM-dd");
        ViewBag.Date2 = d2.ToString("yyyy-MM-dd");

        ViewBag.ChartData = System.Text.Json.JsonSerializer.Serialize(new[]
        {
        new { Label = label1, TotalSales = total1 },
        new { Label = label2, TotalSales = total2 }
    });

        return View("~/Views/Report/MovieTicketSalesChart.cshtml");
    }


}



