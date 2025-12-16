// ViewModels/AdminEditUserVM.cs

using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

public class AdminEditUserVM
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

    [Required(ErrorMessage = "Phone number is required.")]
    [StringLength(11, ErrorMessage = "Phone number must be between 10 and 11 digits.")]
    [RegularExpression(@"^01\d{8,9}$", ErrorMessage = "Must be a valid Malaysian phone number.")]
    public string Phone { get; set; }

    [StringLength(1)]
    public string Gender { get; set; }

    // Read-only property for display/post-back integrity
    public string Role { get; set; }
    // Hidden field to store the current photo URL for display/reference
    public string CurrentPhotoUrl { get; set; }

    // New property for photo upload (IFormFile is used for incoming files)
    [Display(Name = "Change Profile Photo")]
    public IFormFile? NewPhoto { get; set; }
}

public class AddUserVM
{
    // UserId is included but usually marked as readonly in the View 
    // because your helper generates it automatically.
    public string? UserId { get; set; }

    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    [Display(Name = "Full Name")]
    public string Name { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
    [RegularExpression(@"^[\w-\.]+@gmail\.com$", ErrorMessage = "Must be a valid @gmail.com address.")]
    public string Email { get; set; }

    public string Role { get; set; }

    [Required(ErrorMessage = "Gender is required.")]
    [StringLength(1, ErrorMessage = "Gender must be M or F.")]
    public string Gender { get; set; }

    [Required(ErrorMessage = "Phone number is required.")]
    [StringLength(11, ErrorMessage = "Phone number must be between 10 and 11 digits.")]
    [RegularExpression(@"^01\d{8,9}$", ErrorMessage = "Must be a valid Malaysian phone number.")]
    public string Phone { get; set; }
}