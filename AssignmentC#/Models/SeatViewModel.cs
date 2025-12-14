namespace AssignmentC_.Models
{
    public class SeatViewModel
    {
        public int SeatId { get; set; }
        public string SeatIdentifier { get; set; }
        public bool IsPremium { get; set; }
        public bool IsWheelchair { get; set; }
        public bool IsActive { get; set; } = true;

        public char Row => SeatIdentifier[0];
        public int Column => int.Parse(SeatIdentifier.Substring(1));
    }
}