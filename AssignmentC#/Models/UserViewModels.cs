using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
namespace AssignmentC_.Models;

#nullable disable warnings

public class LoginVM
{
    // The Email property is used as the username for login.
    [Required(ErrorMessage = "Email address is required.")]
    [StringLength(100)]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; }

    // The Password property handles the secret input.
    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 5, ErrorMessage = "Password must be at least 5 characters long.")]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    // This property tracks the "Remember Me" checkbox state.
    [Display(Name = "Remember Me")]
    public bool RememberMe { get; set; }
}

public class RegisterVM
{
    // 1. Photo Property 
    [Required(ErrorMessage = "Please upload a profile photo.")]
    [Display(Name = "Profile Photo")]
    public IFormFile Photo { get; set; }

    // Corresponds to the 'Name' property in your User entity
    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    [Display(Name = "Full Name")]
    public string Name { get; set; }

    // Email is used for login and identification
    [Required(ErrorMessage = "Email is required.")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]

    // New: Regular expression to enforce a valid email format ending with @gmail.com
    [RegularExpression(@"^[\w-\.]+@gmail\.com$",
                       ErrorMessage = "Must be a valid email address ending with @gmail.com.")]
    public string Email { get; set; }

    // Password fields
    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be more than 6 number.")] // REMOVED ErrorMode
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [Required(ErrorMessage = "Password confirmation is required.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; }

    // Corresponds to the 'Gender' property in your User entity
    [Required(ErrorMessage = "Gender is required.")]
    [StringLength(1, ErrorMessage = "Gender must be a single character (e.g., M or F).")] // REMOVED ErrorMode
    public string Gender { get; set; }

    // Corresponds to the 'Phone' property in your User entity
    [Required(ErrorMessage = "Phone number is required.")]
    [Phone(ErrorMessage = "Invalid phone number format.")]
    [StringLength(11, ErrorMessage = "Phone number cannot exceed 11 digits.")] // REMOVED ErrorMode
    [Display(Name = "Phone Number")]
    public string Phone { get; set; }
}