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
    public DbSet<Member> Members { get; set; }

    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderLine> OrderLines { get; set; }
    public DbSet<Movie>Movies { get; set; }
    public DbSet<Hall> Halls { get; set; }
    public DbSet<Seat> Seats { get; set; }
    public DbSet<ShowTime> ShowTimes { get; set; }
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


    // Navigation Properties
    public List<Payment> Payment { get; set; } = [];
}

public class Admin : User
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
    public string Desc { get; set; }
    [Precision(6, 2)]
    public decimal Price { get; set; }
    public int stock { get; set; }
    [MaxLength(100)]
    public string region { get; set; }
    [MaxLength(100)]
    public string cinema { get; set; }
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
    public string region { get; set; }
    [MaxLength(100)]
    public string cinema { get; set; }
    [MaxLength(100)]
    public DateOnly CollectDate { get; set; }

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
    public string ProductId { get; set; }

    // Navigation Properties
    public Order Order { get; set; }
    public Product Product { get; set; }
}

public class Payment
{
    [Key]
    public int PaymentId { get; set; }

    public int amount { get; set; }
    public bool status { get; set; }
    public DateOnly date {  get; set; }

    //FK
    public User User { get; set; }
    public Order Order { get; set; }
    public Promotion Promotion {  get; set; }
}

public class Promotion
{
    public int PromotionId { get; set; }
}

public class Memberpoints : Promotion
{
    public int points { get; set; }
}

public class Voucher : Promotion
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}


public class Booking
{

}

public class Review
{
    
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
    public string PosterUrl { get; set; }
    [MaxLength(255)]
    public string BannerUrl { get; set; }
    [MaxLength(255)]
    public string TrailerUrl { get; set; }
    public ICollection<ShowTime> ShowTimes { get; set; } = new List<ShowTime>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
public class ShowTime
{
    [Key]
    public int ShowTimeId { get; set; }
    public int MovieId { get; set; } // Foreign Key to Movie
    public int HallId { get; set; } // Foreign Key to Hall
    public DateTime StartTime { get; set; } // Date and time of the showing
    [Column(TypeName = "decimal(18, 2)")]
    public decimal TicketPrice { get; set; }

    // Navigation: Links to Movie, Hall, and Bookings
    public Movie Movie { get; set; }
    public Hall Hall { get; set; }
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}

public class Hall
{
    [Key]
    public int HallId { get; set; }
    [MaxLength(50)]
    public string Name { get; set; } // E.g., "Hall 1", "IMAX"
    public int Capacity { get; set; }

    // Navigation: A Hall has many Seats and ShowTimes
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
    public Hall Hall { get; set; }
}