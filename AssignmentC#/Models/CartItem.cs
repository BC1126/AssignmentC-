namespace AssignmentC_.Models;

public class CartItem
{
    public Guid CartItemId { get; set; } = Guid.NewGuid();

    // Movie & Showtime info
    public string MovieTitle { get; set; }
    public int ShowTimeId { get; set; }
    public string ShowTimeDisplay { get; set; } // "Dec 15, 2025 - 7:00 PM"
    public string HallName { get; set; } // "Hall 1 - Mid Valley"

    // Seat info
    public List<int> SeatIds { get; set; } = new(); // [101, 102, 103]
    public List<string> Seats { get; set; } = new(); // ["A1", "A2", "A3"]

    // Pricing
    public decimal Price { get; set; } // Price per seat
    public decimal TotalPrice => Seats.Count * Price;
}