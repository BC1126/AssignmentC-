using Microsoft.AspNetCore.Mvc;

using System.ComponentModel.DataAnnotations;

namespace AssignmentC_.Models;

#nullable disable warnings
public class ProductReportVM
{
    public string ProductId { get; set; }
    public string Name { get; set; }
    public string Cinema { get; set; }
    public decimal Price { get; set; }
    public int TotalQuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class SalesChartVM
{
    public string Label { get; set; }
    public decimal TotalSales { get; set; }
}

public class MovieTicketReportVM
{
    public int MovieId { get; set; }
    public string Title { get; set; }
    public int TotalTicketsSold { get; set; }
    public decimal TotalRevenue { get; set; }
}


