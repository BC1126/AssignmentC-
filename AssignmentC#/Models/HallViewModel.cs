using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace AssignmentC_.Models
{
    public class HallViewModel
    {
        public int HallId { get; set; }

        [Required(ErrorMessage = "Hall name is required")]
        [MaxLength(50)]
        public string Name { get; set; }

        public int Capacity { get; set; }

        [Required(ErrorMessage = "Please select an outlet")]
        public int OutletId { get; set; }

        public string? OutletName { get; set; }
        public List<SelectListItem> OutletList { get; set; } = new();

        [Required(ErrorMessage = "Number of rows is required")]
        [Range(1, 50, ErrorMessage = "Rows must be between 1 and 50")]
        public int Rows { get; set; }

        [Required(ErrorMessage = "Seats per row is required")]
        [Range(1, 50, ErrorMessage = "Seats per row must be between 1 and 50")]
        public int SeatsPerRow { get; set; }

        public decimal StandardPrice { get; set; }
        public decimal VipPrice { get; set; }

        [Required(ErrorMessage = "Hall type is required")]
        public string HallType { get; set; }

        public bool IsActive { get; set; } = true;

        // For displaying seats
        public int TotalSeats { get; set; }
        public int PremiumSeats { get; set; }
        public List<SeatViewModel> Seats { get; set; } = new();

    }
}