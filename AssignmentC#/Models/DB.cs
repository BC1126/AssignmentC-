using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssignmentC_.Models;

#nullable disable warnings

public class DB(DbContextOptions options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Admin> Admins { get; set; }
    public DbSet<Staff> Staffs { get; set; }
    public DbSet<Member> Members { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderLine> OrderLines { get; set; }
    public DbSet<Movie> Movies { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<BookingSeat> BookingSeats { get; set; }
    public DbSet<Hall> Halls { get; set; }
    public DbSet<Seat> Seats { get; set; }
    public DbSet<ShowTime> ShowTimes { get; set; }
    public DbSet<Outlet> Outlets { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Promotion> Promotions { get; set; }
    public DbSet<Memberpoints> Memberpoints { get; set; }
    public DbSet<Voucher> Vouchers { get; set; }
    public DbSet<VoucherAssignment> VoucherAssignments { get; set; }
    public DbSet<VoucherCondition> VoucherConditions { get; set; }
    public DbSet<SeatLock> SeatLocks { get; set; }
    public DbSet<ActionLog> ActionLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // FIX 1: Prevent Hall deletion from immediately deleting ShowTimes
        // This stops one path of the cycle.
        modelBuilder.Entity<ShowTime>()
            .HasOne(st => st.Hall)
            .WithMany(h => h.ShowTimes)
            .HasForeignKey(st => st.HallId)
            .OnDelete(DeleteBehavior.Restrict);

        // FIX 2: Prevent Seat deletion from immediately deleting BookingSeats
        // This stops the other path of the cycle.
        modelBuilder.Entity<BookingSeat>()
            .HasOne(bs => bs.Seat)
            .WithMany()
            .HasForeignKey(bs => bs.SeatId)
            .OnDelete(DeleteBehavior.Restrict);

        //Prevent Delete the Product also deleting the OrderLine Item
        modelBuilder.Entity<OrderLine>()
           .HasOne(ol => ol.Product)
           .WithMany()
           .HasForeignKey(ol => ol.ProductId)
           .OnDelete(DeleteBehavior.SetNull);

        // Must call the base method last
        base.OnModelCreating(modelBuilder);
    }
}

public class User
{
    [Key, MaxLength(5)]
    public string UserId {  get; set; }
    [MaxLength(100)]
    public string Name { get; set; }
    [MaxLength(1)]
    public string Gender { get; set; }
    [MaxLength(100)]
    public string Email { get; set; }
    [MaxLength(100)]
    public string PasswordHash { get; set; }
    [MaxLength(11)]
    public string Phone { get; set; }
    public string Role => GetType().Name;
    public bool IsEmailConfirmed { get; set; } = false;
}

public class Admin : User
{

}

public class Staff : User
{

}

public class Member : User
{
    [MaxLength(100)]
    public string PhotoURL { get; set; }
}

public class Product
{
    [Key, MaxLength(4)]
    public string Id { get; set; }
    [MaxLength(100)]
    public string Name { get; set; }
    [MaxLength(100)]
    public string Description { get; set; }
    [Precision(6, 2)]
    public decimal Price { get; set; }
    public int Stock { get; set; }
    [MaxLength(100)]
    public string Region { get; set; }
    [MaxLength(100)]
    public string Cinema { get; set; }
    [MaxLength(100)]
    public string Category { get; set; }
    [MaxLength(100)]
    public string PhotoURL { get; set; }

    // Navigation Properties
    public List<OrderLine> Lines { get; set; } = [];
}

public class Order
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public bool Paid { get; set; }
    [MaxLength(100)]
    public string Region { get; set; }
    [MaxLength(100)]
    public string Cinema { get; set; }
    [MaxLength(100)]
    public DateOnly CollectDate { get; set; }
    [MaxLength(100)]
    public bool Claim { get; set; }

    // Foreign Keys
    public string MemberEmail { get; set; }

    // Navigation Properties
    public Member Member { get; set; }
    public List<OrderLine> OrderLines { get; set; } = [];
}

public class OrderLine
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Precision(6, 2)]
    public decimal Price { get; set; }
    public int Quantity { get; set; }

    // Foreign Keys
    public int OrderId { get; set; }
    public string? ProductId { get; set; }  // make nullable, in case product deleted

    // Navigation Properties
    public Order Order { get; set; }
    public Product? Product { get; set; }   // nullable

    // Snapshot info
    public string ProductName { get; set; } = "";
    public string ProductPhotoURL { get; set; } = "";
}

public class Payment
{
    [Key]
    public int PaymentId { get; set; }
    public decimal amount { get; set; }
    public string status { get; set; }
    public DateOnly date {  get; set; }

    //FK
    public Booking Booking { get; set; }
    public User User { get; set; }
    public Order Order { get; set; }
    public List<Promotion> Promotions { get; set; }
}

public class Promotion
{
    [Key]
    public int PromotionId { get; set; }
    public List<Payment> Payments { get; set; }
}

public class Memberpoints : Promotion
{
    public int points { get; set; }

    public VoucherCondition? VoucherCondition { get; set; }
    public VoucherAssignment? VoucherAssignment { get; set; }
}

public class Voucher : Promotion
{
    [MaxLength(50)]
    public string VoucherCode { get; set; }

    [MaxLength(20)]
    public string VoucherType { get; set; }

    public decimal DiscountValue { get; set; }

    public string EligibilityMode { get; set; }

    [MaxLength(10)]
    public string status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime CreatedTime { get; set; }
    public decimal MinSpend { get; set; }
}

public class VoucherUsage
{
    public int VoucherUsageId { get; set; }
    public decimal DiscounAmount { get; set; }
    public DateTime UsedTime { get; set; }

    // FK
    public Promotion Promotion { get; set; }
    public User User { get; set; }
}

public class VoucherCondition
{
    [Key]
    public int ConditionId { get; set; }

    [MaxLength(100)]
    public string ConditionType { get; set; }

    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    
    public bool? IsFirstPurchase { get; set; }
    public List<int> BirthMonth { get; set; } = new List<int>();

    public Promotion Promotion { get; set; } = null;
}

public class VoucherAssignment
{
    [Key]
    public int AssignmentId { get; set; }

    //FK
    public Promotion promotion { get; set; } = null;
    public User user { get; set; }
}


public class Booking
{
    [Key]
    public int BookingId { get; set; }
    public string MemberId { get; set; }
    public int ShowTimeId { get; set; }
    
    // Booking info
    public int TicketQuantity { get; set; }
    [Column(TypeName = "decimal(18, 2)")]
    public decimal TotalPrice { get; set; }
    public DateTime BookingDate { get; set; } = DateTime.Now;

    // Navigation: Link to Movie, Hall, Member, and Showtime
    public ShowTime ShowTime { get; set; }
    public Member Member { get; set; }
    public ICollection<BookingSeat> BookingSeats { get; set; } = new List<BookingSeat>();

    internal ShowTime Select(Func<object, Booking> value)
    {
        throw new NotImplementedException();
    }
}

public class BookingSeat
{
    [Key]
    public int BookingSeatId { get; set; }

    public int BookingId { get; set; }
    public int SeatId { get; set; }

    public Booking Booking { get; set; }
    public Seat Seat { get; set; }
}

public class Movie{
    [Key]
    public int MovieId { get; set; }
    [MaxLength(100)]
    public string Title { get; set; }
    [Column(TypeName = "nvarchar(MAX)")]
    public string Description { get; set; }
    [MaxLength(50)]
    public string Genre { get; set; }
    public int DurationMinutes { get; set; }
    [MaxLength(10)]
    public string Rating { get; set; }
    [MaxLength(100)]
    public string Director { get; set; }
    [MaxLength(100)]
    public string Writer { get; set; }
    public DateTime PremierDate { get; set; }
    [MaxLength(255)]
    public string? PosterUrl { get; set; }
    [MaxLength(255)]
    public string? BannerUrl { get; set; }
    [MaxLength(255)]
    public string TrailerUrl { get; set; }
    public ICollection<ShowTime> ShowTimes { get; set; } = new List<ShowTime>();
}

public class ShowTime
{
    [Key]
    public int ShowTimeId { get; set; }

    [Required]
    public int MovieId { get; set; }

    [Required]
    public int HallId { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    [Range(0, 1000)]
    public decimal TicketPrice { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    [ForeignKey(nameof(MovieId))]
    public Movie Movie { get; set; }
    [ForeignKey(nameof(HallId))]
    public Hall Hall { get; set; }
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    [NotMapped]
    public DateTime EndTime => StartTime.AddMinutes(Movie?.DurationMinutes ?? 0);
}

public class Hall
{
    [Key]
    public int HallId { get; set; }
    public int OutletId { get; set; }
    [MaxLength(50)]
    public string Name { get; set; }
    public int Capacity { get; set; }

    [MaxLength(20)]
    public string HallType { get; set; } = "Standard";

    public bool IsActive { get; set; } = true;

    public Outlet Outlet { get; set; }
    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
    public ICollection<ShowTime> ShowTimes { get; set; } = new List<ShowTime>();
}

public class Seat
{
    [Key]
    public int SeatId { get; set; }
    public int HallId { get; set; }
    [MaxLength(10)]
    public string SeatIdentifier { get; set; }
    public bool IsPremium { get; set; }
    public bool IsWheelchair { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public Hall Hall { get; set; }
}

public class Outlet
{
    [Key]
    public int OutletId { get; set; }
    [MaxLength(50)]
    public string City { get; set; } 
    [MaxLength(100)]
    public string Name { get; set; } 

    public ICollection<Hall> Halls { get; set; } = new List<Hall>();
}
public class SeatLock
{
    public int SeatLockId { get; set; }
    public int ShowTimeId { get; set; }
    public int SeatId { get; set; }
    public string SessionId { get; set; } 
    public DateTime LockedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsExpired => DateTime.Now >= ExpiresAt;

    // Navigation properties
    public ShowTime ShowTime { get; set; }
    public Seat Seat { get; set; }
}
public class ActionLog
{
    [Key]
    public int Id { get; set; }

    [MaxLength(100)]
    public string UserEmail { get; set; }

    [MaxLength(100)]
    public string UserName { get; set; }

    [MaxLength(50)]
    public string UserRole { get; set; }  

    [MaxLength(100)]
    public string Entity { get; set; }

    [Column(TypeName = "nvarchar(MAX)")]
    public string Action { get; set; }

    public DateTime CreatedAt { get; set; }
}
