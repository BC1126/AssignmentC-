using System.ComponentModel.DataAnnotations;

namespace AssignmentC_.Models
{
    public class TicketViewModel
    {

    }

    public class PurchaseVM
    {
        public int ShowTimeId { get; set; }
        public string MovieTitle { get; set; }
        public int MovieDuration { get; set; }
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
        public List<string> SelectedSeatIdentifiers { get; set; } = new();
    }

    public class TimerViewModel
    {
        public int Minutes { get; set; }
        public int Seconds { get; set; }
        public bool Expired { get; set; }
    }


    public class CheckoutViewModel
    {
        public TimerViewModel Timer { get; set; }
    }
}
