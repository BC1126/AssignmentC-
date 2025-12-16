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
    public IFormFile? NewPhoto { get; set; }
}