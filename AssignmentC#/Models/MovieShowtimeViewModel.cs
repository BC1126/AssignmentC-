using AssignmentC_.Models;

namespace AssignmentC_.Models;

public class MovieShowtimeViewModel
{
    public Movie Movie { get; set; }
    public List<OutletShowtimes> GroupedShowtimes { get; set; } = new();
}

public class OutletShowtimes
{
    public int OutletId { get; set; }
    public string OutletName { get; set; }
    public string City { get; set; }
    public List<ShowtimeInfo> Showtimes { get; set; } = new();
}

public class ShowtimeInfo
{
    public int ShowTimeId { get; set; }
    public DateTime StartTime { get; set; }
    public decimal TicketPrice { get; set; }
    public string HallName { get; set; }
    public string HallType { get; set; }
    public int AvailableSeats { get; set; }
}