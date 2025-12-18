namespace AssignmentC_.Models;

public class MemberDetailsVM
{
    // Basic Information
    public string Name { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string Gender { get; set; }

    // Profile Image
    public string? PhotoPath { get; set; }

    // Account Status
    public DateTime JoinDate { get; set; }
    public bool IsEmailConfirmed { get; set; }

    // Metadata (Optional but helpful for Staff)
    public string Role { get; set; } = "Member";
}