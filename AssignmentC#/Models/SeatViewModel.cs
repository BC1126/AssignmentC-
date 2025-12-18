namespace AssignmentC_.Models;

public class SeatViewModel
{
    public int SeatId { get; set; }
    public string SeatIdentifier { get; set; }
    public bool IsPremium { get; set; }
    public bool IsWheelchair { get; set; }
    public bool IsActive { get; set; }


    public string Row { get; set; }
    public int Column { get; set; }
}