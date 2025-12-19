namespace AssignmentC_.Models;

/// <summary>
/// ViewModel for the SelectTicket page - displays movie showtime and available seats
/// </summary>
public class SelectTicketViewModel
{
    public int ShowTimeId { get; set; }
    public int MovieId { get; set; }
    public string MovieTitle { get; set; }
    public string MoviePosterUrl { get; set; }
    public DateTime StartTime { get; set; }
    public string HallName { get; set; }
    public string OutletName { get; set; }
    public decimal TicketPrice { get; set; }
    public decimal ChildrenPrice { get; set; } // 20% discount
    public decimal SeniorPrice { get; set; } // 15% discount
    public decimal OkuPrice { get; set; }
    public string SessionId { get; set; } // For seat locking
    public int LockDurationMinutes { get; set; } // How long seats are locked
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
    public bool IsLocked { get; set; } // Temporarily locked by another session
    public bool IsSelected { get; set; }
    public string Row { get; set; } // e.g., "A", "B"
    public int Column { get; set; } // e.g., 1, 2, 3
}

public class BookingSessionData
{
    public int ShowTimeId { get; set; }
    public string MemberId { get; set; }
    public int TicketQuantity { get; set; }
    public decimal TicketSubtotal { get; set; }
    public List<int> SelectedSeatIds { get; set; } = new List<int>();

    public string MovieTitle { get; set; }
    public DateTime StartTime { get; set; }
    public string HallName { get; set; }
    public string OutletName { get; set; }
    public List<string> SelectedSeatIdentifiers { get; set; } = new List<string>();

    public int ChildrenCount { get; set; }
    public int AdultCount { get; set; }
    public int SeniorCount { get; set; }
    public int OkuCount { get; set; }
    public decimal TicketPrice { get; set; } // Base price
}