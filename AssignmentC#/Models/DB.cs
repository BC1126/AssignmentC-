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
    public string UserId {  get; set; }
    [MaxLength(100)]
    public string Name { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string Phone { get; set; }
    public string Role => GetType().Name;

}

public class Admin : User
{

}

public class Member : User
{

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