using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace AssignmentC_.Models;

public class DB(DbContextOptions options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Order> Orders { get; set; }
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

public class Order
{

}

public class FoodAndBeverage
{

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