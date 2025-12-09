using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace AssignmentC_.Models;

public class MovieViewModel
{
    public int MovieId { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [MaxLength(100)]
    public string Title { get; set; }

    [Required(ErrorMessage = "Description is required.")]
    public string Description { get; set; }

    [Required(ErrorMessage = "Genre is required.")]
    [MaxLength(50)]
    public string Genre { get; set; }

    [Required(ErrorMessage = "Duration is required.")]
    [Range(1, 500, ErrorMessage = "Duration must be between 1 and 500 minutes.")]
    public int DurationMinutes { get; set; }

    [Required(ErrorMessage = "Rating is required.")]
    [MaxLength(10)]
    public string Rating { get; set; }

    [Required(ErrorMessage = "Director is required.")]
    [MaxLength(100)]
    public string Director { get; set; }

    [Required(ErrorMessage = "Writer is required.")]
    [MaxLength(100)]
    public string Writer { get; set; }

    [Required(ErrorMessage = "Premier Date is required.")]
    [DataType(DataType.Date)]
    public DateTime PremierDate { get; set; }

    // Existing URLs (used when editing a movie)
    public string PosterUrl { get; set; }
    public string BannerUrl { get; set; }

    // IFormFile properties handle the actual file upload from the HTML form.
    // We don't mark them as [Required] here because for an Edit operation,
    // they are optional if the user is keeping the old image.
    public IFormFile PosterFile { get; set; }
    public IFormFile BannerFile { get; set; }

    [Required(ErrorMessage = "Trailer URL is required.")]
    [Url(ErrorMessage = "Must be a valid URL.")]
    [MaxLength(255)]
    public string TrailerUrl { get; set; }
}