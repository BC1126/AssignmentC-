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

    // 1. Enforce minimum 8 character length
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]

    [DataType(DataType.Password)]

    // 2. Enforce complexity requirements (Uppercase, Lowercase, Digit, Special Character)
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$",
        ErrorMessage = "Password must contain: at least one uppercase letter, one lowercase letter, one digit, one special character, and at least 8 chracter long."
    )]
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
    // Set StringLength to match the max allowed digits (11)
    [StringLength(11, ErrorMessage = "Phone number must be between 10 and 11 digits.")]
    [Display(Name = "Phone Number")]

    // ------------------------------------------------------------------------------------------
    // FINAL MODIFIED PART: REGULAR EXPRESSION FOR 10 or 11 DIGITS (e.g., 0123456789 or 01234567890)
    // ------------------------------------------------------------------------------------------
    [RegularExpression(@"^01\d{8,9}$",
        ErrorMessage = "Must be a valid Malaysian phone number, starting with 01 and 10 or 11 digits total (e.g., 0123456789).")]
    public string Phone { get; set; }
}

public class EditProfileVM
{
    // Must be a string to match User.UserId
    [Required]
    [MaxLength(5)]
    public string Id { get; set; }

    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100)]
    public string Name { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [StringLength(100)]
    // Using your desired Regex validation
    [RegularExpression(@"^[\w-\.]+@gmail\.com$",
                       ErrorMessage = "Email must be a valid address ending with @gmail.com.")]
    public string Email { get; set; }

    [Phone(ErrorMessage = "Invalid phone number format.")]
    [StringLength(11)] // Adjusted to 11 to match User.Phone MaxLength
    public string Phone { get; set; }

    [StringLength(1)]
    public string Gender { get; set; }

    // Read-only property for display/post-back integrity
    public string Role { get; set; }
    // Hidden field to store the current photo URL for display/reference
    public string CurrentPhotoUrl { get; set; }

    // New property for photo upload (IFormFile is used for incoming files)
    [Display(Name = "Change Profile Photo")]
    public IFormFile NewPhoto { get; set; }
}

public class ChangePasswordVM
{
    [StringLength(100, MinimumLength = 5)]
    [DataType(DataType.Password)]
    [DisplayName("Current Password")]
    public string Current { get; set; }

    [StringLength(100, MinimumLength = 5)]
    [DataType(DataType.Password)]
    [DisplayName("New Password")]
    public string New { get; set; }

    [StringLength(100, MinimumLength = 5)]
    [Compare("New")]
    [DataType(DataType.Password)]
    [DisplayName("Confirm Password")]
    public string Confirm { get; set; }
}