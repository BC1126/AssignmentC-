using System.ComponentModel;
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
        public string MoviewUrl { get; set; }
        public string MovieRating { get; set; }
        public int MovieDuration { get; set; }
        public DateTime StartTime { get; set; }
        public string HallName { get; set; }
        public string OutletCity { get; set; }
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

    public class VoucherViewModel
    {
        public int PromotionId { get; set; }

        [DisplayName("Voucher Code")]
        public string VoucherCode { get; set; }

        [DisplayName("Discount Type")]
        public string DiscountType { get; set; }

        [DisplayName("Discount Value")]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335" , ErrorMessage = "Please enter a valid number")]
        // 79228162514264337593543950335 = max value of decimal
        public decimal DiscountValue { get; set; }

        [DisplayName("Eligibility Mode")]
        public string EligibilityMode { get; set; }

        [DisplayName("Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate {  get; set; }

        [DisplayName("End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

       
        [DisplayName("Minimum Spend Amount")]
        [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Please enter a valid number")]
        public decimal MinSpend { get; set; }
        
        // Condition
        public int? MaxAge { get; set; }
        public int? MinAge { get; set; }
        
        public bool? IsFirstPurchase { get; set; }
        public List<int> BirthMonth { get; set; } = new List<int>();
        // Assigned User use
        public int? AssignedUserId { get; set; }
    }
}
