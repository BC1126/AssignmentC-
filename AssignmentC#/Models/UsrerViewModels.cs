 using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

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
    [DataType(DataType.Password)] // Ensures input is masked in the UI
    public string Password { get; set; }

    // This property tracks the "Remember Me" checkbox state.
    [Display(Name = "Remember Me")] // Used for display labels in the View
    public bool RememberMe { get; set; }
}

public class RegisterVM
{
    // Corresponds to the 'Name' property in your User entity
    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    [Display(Name = "Full Name")]
    public string Name { get; set; }

    // Email is used for login and identification
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
    public string Email { get; set; }

    // Password fields
    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [Required(ErrorMessage = "Password confirmation is required.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; }

    // Corresponds to the 'Gender' property in your User entity
    [Required(ErrorMessage = "Gender is required.")]
    [StringLength(1, ErrorMessage = "Gender must be a single character (e.g., M, F, or O).")]
    public string Gender { get; set; }

    // Corresponds to the 'Phone' property in your User entity
    // Note: The [Required] attribute is generally used here if the field is mandatory. 
    // If optional, remove [Required]. I'll keep it for completeness based on your previous input pattern.
    [Required(ErrorMessage = "Phone number is required.")]
    [Phone(ErrorMessage = "Invalid phone number format.")]
    [StringLength(11, ErrorMessage = "Phone number cannot exceed 11 digits.")]
    [Display(Name = "Phone Number")]
    public string Phone { get; set; }

    // Checkbox for terms and conditions
    [Required(ErrorMessage = "You must agree to the terms and conditions.")]
    [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to the terms and conditions.")]
    [Display(Name = "Agree to Terms")]
    public bool AgreeToTerms { get; set; }
}