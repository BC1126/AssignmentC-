namespace AssignmentC_.Models;

/// <summary>
/// ViewModel for the SelectTicket page - displays movie showtime and available seats
/// </summary>
public class SelectTicketViewModel
{
    public int ShowTimeId { get; set; }
    public string MovieTitle { get; set; }
    public string MoviePosterUrl { get; set; }
    public DateTime StartTime { get; set; }
    public string HallName { get; set; }
    public string OutletName { get; set; }
    public decimal TicketPrice { get; set; }
    public List<SeatSelectionViewModel> Seats { get; set; } = new();
}

/// <summary>
/// ViewModel for individual seat in the seat selection grid
/// </summary>
public class SeatSelectionViewModel
{
    public int SeatId { get; set; }
    public string SeatIdentifier { get; set; } // e.g., "A1", "B5"
    public bool IsPremium { get; set; }
    public bool IsWheelchair { get; set; }
    public bool IsOccupied { get; set; } // Already booked by someone else
    public string Row { get; set; } // e.g., "A", "B"
    public int Column { get; set; } // e.g., 1, 2, 3
}

public class BookingSessionData
{
    // Showtime Info
    public int ShowTimeId { get; set; }
    public string MovieTitle { get; set; }
    public DateTime StartTime { get; set; }
    public string HallName { get; set; }
    public string OutletName { get; set; }

    // Ticket Info
    public decimal TicketPrice { get; set; }
    public int TicketQuantity { get; set; }
    public decimal TicketSubtotal { get; set; }

    // Ticket Type Breakdown
    public int ChildrenCount { get; set; }
    public int AdultCount { get; set; }
    public int SeniorCount { get; set; }

    // Selected Seats
    public List<int> SelectedSeatIds { get; set; } = new();
    public List<string> SelectedSeatIdentifiers { get; set; } = new(); // e.g., ["A1", "A2", "A3"]

    // Food & Beverage (filled by F&B page)
    public decimal FoodBeverageTotal { get; set; } = 0;

    // Auto-calculated total
    public decimal GrandTotal => TicketSubtotal + FoodBeverageTotal;
}

