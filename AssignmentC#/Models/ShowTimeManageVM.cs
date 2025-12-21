using System.ComponentModel.DataAnnotations;

namespace AssignmentC_.Models
{
    public class ShowTimeManageVM
    {
        public int ShowTimeId { get; set; }

        [Required(ErrorMessage = "Please select an outlet.")]
        public int OutletId { get; set; }

        [Required(ErrorMessage = "Please select a hall.")]
        public int HallId { get; set; }

        [Required(ErrorMessage = "Please select a date.")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        // Dropdowns
        public List<Outlet> Outlets { get; set; }
        public List<Hall> Halls { get; set; }
        public List<Movie> Movies { get; set; }

        // Existing showtimes
        public List<ShowTime> ExistingShowTimes { get; set; }

        // New showtime input
        [Required(ErrorMessage = "Please select a movie.")]
        public int MovieId { get; set; }

        [Required(ErrorMessage = "Please enter a start time.")]
        [DataType(DataType.Time)]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "Please enter a ticket price.")]
        [Range(0.00, 1000.00, ErrorMessage = "Ticket price must be between 0 and 1000.")]
        public decimal TicketPrice { get; set; }

        [Range(0, 30, ErrorMessage = "You can repeat for up to 30 days.")]
        public int RepeatDays { get; set; } = 0;

    }
}
