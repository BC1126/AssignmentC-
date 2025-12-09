using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AssignmentC_.Models;

#nullable disable warnings

public class ProductInsertVM
{
    [StringLength(4)]
    [RegularExpression(@"P\d{3}", ErrorMessage = "Invalid {0} format.")]
    [Remote("CheckId", "Product", ErrorMessage = "Duplicated {0}.")]
    public string Id { get; set; }

    [Required(ErrorMessage = "Product Name is required.")]
    [RegularExpression(@"^[A-Za-z0-9\s]+$")]
    [StringLength(100)]
    public string Name { get; set; }

    [Required(ErrorMessage = "Product Description is required.")]
    [StringLength(255)]
    public string Description { get; set; }

    [Range(0.01, 9999.99)]
    [RegularExpression(@"\d+(\.\d{1,2})?", ErrorMessage = "Invalid {0} format.")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Stock is required.")]
    [Range(0, 200)]
    public int Stock {  get; set; }

    [Required(ErrorMessage = "Region is required.")]
    [StringLength(100)]
    public string Region { get; set; }

    [Required(ErrorMessage = "Cinema is required.")]
    [StringLength(100)]
    public string Cinema { get; set; }

    [Required(ErrorMessage = "Category is required.")]
    [StringLength(100)]
    public string Category { get; set; }

    // Other properties
    public IFormFile Photo { get; set; }
}

public class ProductUpdateVM
{
    public string Id { get; set; }

    [Required(ErrorMessage = "Product Name is required.")]
    [RegularExpression(@"^[A-Za-z0-9\s]+$")]
    [StringLength(100)]
    public string Name { get; set; }

    [Required(ErrorMessage = "Product Description is required.")]
    [StringLength(255)]
    public string Description { get; set; }

    [Required(ErrorMessage = "Stock is required.")]
    [Range(0, 200)]
    public int Stock { get; set; }

    [Range(0.01, 9999.99)]
    [RegularExpression(@"\d+(\.\d{1,2})?", ErrorMessage = "Invalid {0} format.")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Region is required.")]
    [StringLength(100)]
    public string Region { get; set; }

    [Required(ErrorMessage = "Cinema is required.")]
    [StringLength(100)]
    public string Cinema { get; set; }

    [Required(ErrorMessage = "Category is required.")]
    [StringLength(100)]
    public string Category { get; set; }

    // Other properties
    public string? PhotoURL { get; set; }
    public IFormFile? Photo { get; set; }
}
