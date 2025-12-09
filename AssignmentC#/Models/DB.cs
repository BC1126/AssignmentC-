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
    public string Desc {  get; set; }
    [Precision(6, 2)]
    public decimal Price { get; set; }
    public int stock {  get; set; }
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
    public DateOnly CollectDate {  get; set; }

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

}

public class Booking
{

}

public class Review
{

}

public class Movie
{

}

public class ShowTime
{

}

public class Hall
{

}

public class Seat
{

}