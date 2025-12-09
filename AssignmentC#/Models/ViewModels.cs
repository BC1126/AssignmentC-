using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AssignmentC_.Models;

using System.ComponentModel.DataAnnotations;

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
    [DataType(DataType.Password)] // Ensures input is masked in the UI
    public string Password { get; set; }

    // This property tracks the "Remember Me" checkbox state.
    [Display(Name = "Remember Me")] // Used for display labels in the View
    public bool RememberMe { get; set; }
}

public class RegisterVM
{
    // --- Email Field ---
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [StringLength(100)]
    public string Email { get; set; }

    // --- Password Field ---
    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    // --- Password Confirmation Field ---
    [Required(ErrorMessage = "Confirm Password is required.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")] // Used for display labels in the View
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; }

    // --- Optional Name Fields ---
    [StringLength(50)]
    public string FirstName { get; set; }

    [StringLength(50)]
    public string LastName { get; set; }
}
