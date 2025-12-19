namespace AssignmentC_.Modelsl;

public class SeatLock
{
    public int SeatLockId { get; set; }
    public int SeatId { get; set; }
    public int ShowTimeId { get; set; }
    public string SessionId { get; set; }
    public DateTime LockedAt { get; set; }     
    public DateTime ExpiresAt { get; set; }     

    // Navigation properties
    public Seat Seat { get; set; }
    public ShowTime ShowTime { get; set; }
}

