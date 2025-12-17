// Add this method to your HallController or create a new SeedController

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssignmentC_.Models;

namespace AssignmentC_.Controllers;

public class SeedController : Controller
{
    private readonly DB db;

    public SeedController(DB db)
    {
        this.db = db;
    }

    [HttpGet]
    public IActionResult BookingTestData()
    {
        try
        {
            Console.WriteLine("🌱 Starting Booking Test Data Seed...");

            // 1. CREATE OUTLET
            var outlet = db.Outlets.FirstOrDefault();
            if (outlet == null)
            {
                outlet = new Outlet
                {
                    City = "Kuala Lumpur",
                    Name = "Mid Valley Megamall"
                };
                db.Outlets.Add(outlet);
                db.SaveChanges();
                Console.WriteLine($"✅ Created Outlet: {outlet.Name}");
            }

            // 2. CREATE HALL
            var hall = db.Halls.FirstOrDefault(h => h.Name == "Hall 1");
            if (hall == null)
            {
                hall = new Hall
                {
                    Name = "Hall 1",
                    OutletId = outlet.OutletId,
                    Capacity = 50,
                    HallType = "Standard",
                    IsActive = true
                };
                db.Halls.Add(hall);
                db.SaveChanges();
                Console.WriteLine($"✅ Created Hall: {hall.Name}");

                // CREATE SEATS (5 rows x 10 seats = 50 seats)
                for (int row = 0; row < 5; row++)
                {
                    for (int col = 1; col <= 10; col++)
                    {
                        var isPremium = row >= 3; // Last 2 rows are VIP
                        var isWheelchair = row == 0 && (col == 1 || col == 10); // First row corners

                        db.Seats.Add(new Seat
                        {
                            HallId = hall.HallId,
                            SeatIdentifier = $"{(char)('A' + row)}{col}",
                            IsPremium = isPremium,
                            IsWheelchair = isWheelchair,
                            IsActive = true
                        });
                    }
                }
                db.SaveChanges();
                Console.WriteLine($"✅ Created 50 seats for {hall.Name}");
            }

            // 3. CREATE MOVIES
            var movies = new List<Movie>();

            var movie1 = db.Movies.FirstOrDefault(m => m.Title == "Avengers: Endgame");
            if (movie1 == null)
            {
                movie1 = new Movie
                {
                    Title = "Avengers: Endgame",
                    Description = "After the devastating events of Avengers: Infinity War, the universe is in ruins. With the help of remaining allies, the Avengers assemble once more to reverse Thanos' actions and restore balance to the universe.",
                    Genre = "Action, Adventure, Sci-Fi",
                    DurationMinutes = 181,
                    Rating = "PG-13",
                    Director = "Anthony Russo, Joe Russo",
                    Writer = "Christopher Markus, Stephen McFeely",
                    PremierDate = new DateTime(2019, 4, 26),
                    PosterUrl = "/images/movies/avengers-endgame.jpg",
                    BannerUrl = "/images/movies/avengers-endgame-banner.jpg",
                    TrailerUrl = "https://www.youtube.com/watch?v=TcMBFSGVi1c"
                };
                db.Movies.Add(movie1);
                movies.Add(movie1);
            }

            var movie2 = db.Movies.FirstOrDefault(m => m.Title == "The Dark Knight");
            if (movie2 == null)
            {
                movie2 = new Movie
                {
                    Title = "The Dark Knight",
                    Description = "When the menace known as the Joker wreaks havoc and chaos on the people of Gotham, Batman must accept one of the greatest psychological and physical tests of his ability to fight injustice.",
                    Genre = "Action, Crime, Drama",
                    DurationMinutes = 152,
                    Rating = "PG-13",
                    Director = "Christopher Nolan",
                    Writer = "Jonathan Nolan, Christopher Nolan",
                    PremierDate = new DateTime(2008, 7, 18),
                    PosterUrl = "/images/movies/dark-knight.jpg",
                    BannerUrl = "/images/movies/dark-knight-banner.jpg",
                    TrailerUrl = "https://www.youtube.com/watch?v=EXeTwQWrcwY"
                };
                db.Movies.Add(movie2);
                movies.Add(movie2);
            }

            var movie3 = db.Movies.FirstOrDefault(m => m.Title == "Inception");
            if (movie3 == null)
            {
                movie3 = new Movie
                {
                    Title = "Inception",
                    Description = "A thief who steals corporate secrets through the use of dream-sharing technology is given the inverse task of planting an idea into the mind of a C.E.O.",
                    Genre = "Action, Sci-Fi, Thriller",
                    DurationMinutes = 148,
                    Rating = "PG-13",
                    Director = "Christopher Nolan",
                    Writer = "Christopher Nolan",
                    PremierDate = new DateTime(2010, 7, 16),
                    PosterUrl = "/images/movies/inception.jpg",
                    BannerUrl = "/images/movies/inception-banner.jpg",
                    TrailerUrl = "https://www.youtube.com/watch?v=YoHD9XEInc0"
                };
                db.Movies.Add(movie3);
                movies.Add(movie3);
            }

            db.SaveChanges();
            Console.WriteLine($"✅ Created {movies.Count} movies");

            // 4. CREATE SHOWTIMES
            var today = DateTime.Today;
            var showtimesCreated = 0;

            // Reload movies from DB to get their IDs
            movie1 = db.Movies.FirstOrDefault(m => m.Title == "Avengers: Endgame");
            movie2 = db.Movies.FirstOrDefault(m => m.Title == "The Dark Knight");
            movie3 = db.Movies.FirstOrDefault(m => m.Title == "Inception");

            // Create showtimes for next 7 days
            for (int day = 0; day < 7; day++)
            {
                var date = today.AddDays(day);

                // Avengers - 2 shows per day
                if (!db.ShowTimes.Any(st => st.MovieId == movie1.MovieId &&
                    st.StartTime.Date == date && st.StartTime.Hour == 14))
                {
                    db.ShowTimes.Add(new ShowTime
                    {
                        MovieId = movie1.MovieId,
                        HallId = hall.HallId,
                        StartTime = date.AddHours(14), // 2:00 PM
                        TicketPrice = 15.00m
                    });
                    showtimesCreated++;
                }

                if (!db.ShowTimes.Any(st => st.MovieId == movie1.MovieId &&
                    st.StartTime.Date == date && st.StartTime.Hour == 19))
                {
                    db.ShowTimes.Add(new ShowTime
                    {
                        MovieId = movie1.MovieId,
                        HallId = hall.HallId,
                        StartTime = date.AddHours(19), // 7:00 PM
                        TicketPrice = 18.00m
                    });
                    showtimesCreated++;
                }

                // Dark Knight - 1 show per day
                if (!db.ShowTimes.Any(st => st.MovieId == movie2.MovieId &&
                    st.StartTime.Date == date && st.StartTime.Hour == 20))
                {
                    db.ShowTimes.Add(new ShowTime
                    {
                        MovieId = movie2.MovieId,
                        HallId = hall.HallId,
                        StartTime = date.AddHours(20), // 8:00 PM
                        TicketPrice = 16.00m
                    });
                    showtimesCreated++;
                }

                // Inception - 2 shows per day
                if (!db.ShowTimes.Any(st => st.MovieId == movie3.MovieId &&
                    st.StartTime.Date == date && st.StartTime.Hour == 15))
                {
                    db.ShowTimes.Add(new ShowTime
                    {
                        MovieId = movie3.MovieId,
                        HallId = hall.HallId,
                        StartTime = date.AddHours(15).AddMinutes(30), // 3:30 PM
                        TicketPrice = 14.00m
                    });
                    showtimesCreated++;
                }

                if (!db.ShowTimes.Any(st => st.MovieId == movie3.MovieId &&
                    st.StartTime.Date == date && st.StartTime.Hour == 21))
                {
                    db.ShowTimes.Add(new ShowTime
                    {
                        MovieId = movie3.MovieId,
                        HallId = hall.HallId,
                        StartTime = date.AddHours(21).AddMinutes(30), // 9:30 PM
                        TicketPrice = 17.00m
                    });
                    showtimesCreated++;
                }
            }

            db.SaveChanges();
            Console.WriteLine($"✅ Created {showtimesCreated} showtimes");

            // 5. CREATE TEST BOOKING (to show occupied seats)
            var firstShowtime = db.ShowTimes
                .Include(st => st.Hall)
                    .ThenInclude(h => h.Seats)
                .FirstOrDefault();

            if (firstShowtime != null && !db.Bookings.Any(b => b.ShowTimeId == firstShowtime.ShowTimeId))
            {
                // Create a test member if needed
                var testMember = db.Members.FirstOrDefault(m => m.Email == "test@test.com");
                if (testMember == null)
                {
                    testMember = new Member
                    {
                        UserId = "M0001",
                        Name = "Test User",
                        Email = "test@test.com",
                        Gender = "M",
                        PasswordHash = "test123",
                        Phone = "0123456789"
                    };
                    db.Members.Add(testMember);
                    db.SaveChanges();
                }

                // Book some seats (A1, A2, A3)
                var seatsToBook = firstShowtime.Hall.Seats
                    .Where(s => s.SeatIdentifier == "A1" ||
                                s.SeatIdentifier == "A2" ||
                                s.SeatIdentifier == "A3")
                    .ToList();

                var booking = new Booking
                {
                    MemberId = testMember.UserId,
                    ShowTimeId = firstShowtime.ShowTimeId,
                    TicketQuantity = seatsToBook.Count,
                    TotalPrice = seatsToBook.Count * firstShowtime.TicketPrice,
                    BookingDate = DateTime.Now
                };
                db.Bookings.Add(booking);
                db.SaveChanges();

                foreach (var seat in seatsToBook)
                {
                    db.BookingSeats.Add(new BookingSeat
                    {
                        BookingId = booking.BookingId,
                        SeatId = seat.SeatId
                    });
                }
                db.SaveChanges();
                Console.WriteLine($"✅ Created test booking with 3 seats");
            }

            TempData["Info"] = $"✅ Test data created successfully! " +
                              $"Created {movies.Count} movies, {showtimesCreated} showtimes, " +
                              $"1 hall with 50 seats.";

            return RedirectToAction("TestBooking");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
            TempData["Error"] = $"Error creating test data: {ex.Message}";
            return Content($"Error: {ex.Message}<br><br>{ex.StackTrace}");
        }
    }

    [HttpGet]
    public IActionResult TestBooking()
    {
        var showtimes = db.ShowTimes
            .Include(st => st.Movie)
            .Include(st => st.Hall)
                .ThenInclude(h => h.Outlet)
            .OrderBy(st => st.StartTime)
            .Take(10)
            .Select(st => new
            {
                st.ShowTimeId,
                st.Movie.Title,
                StartTime = st.StartTime.ToString("MMM dd, yyyy - h:mm tt"),
                Hall = $"{st.Hall.Name} - {st.Hall.Outlet.Name}",
                Price = st.TicketPrice,
                BookingUrl = Url.Action("SelectTicket", "Booking", new { showtimeId = st.ShowTimeId })
            })
            .ToList();

        var html = "<h2>Test Booking Data</h2>";
        html += "<p><a href='/Seed/BookingTestData'>🌱 Re-seed Data</a></p>";
        html += "<table border='1' cellpadding='10'>";
        html += "<tr><th>ID</th><th>Movie</th><th>Time</th><th>Hall</th><th>Price</th><th>Action</th></tr>";

        foreach (var st in showtimes)
        {
            html += $"<tr>";
            html += $"<td>{st.ShowTimeId}</td>";
            html += $"<td><strong>{st.Title}</strong></td>";
            html += $"<td>{st.StartTime}</td>";
            html += $"<td>{st.Hall}</td>";
            html += $"<td>RM {st.Price:F2}</td>";
            html += $"<td><a href='{st.BookingUrl}' class='btn btn-primary'>Book Now</a></td>";
            html += $"</tr>";
        }

        html += "</table>";

        return Content(html, "text/html");
    }
    // Add this to your SeedController.cs

    [HttpGet]
    public IActionResult MovieShowtimeTestData()
    {
        try
        {
            Console.WriteLine("🌱 Starting Movie Showtime Test Data Seed...");

            // 1. CREATE OUTLETS
            Console.WriteLine("Creating outlets...");
            var outlet1 = GetOrCreateOutlet("Mid Valley Megamall", "Kuala Lumpur");
            var outlet2 = GetOrCreateOutlet("1 Utama Shopping Centre", "Petaling Jaya");
            var outlet3 = GetOrCreateOutlet("Pavilion KL", "Kuala Lumpur");
            Console.WriteLine($"✅ Outlets ready: {outlet1.Name}, {outlet2.Name}, {outlet3.Name}");

            // 2. CREATE HALLS
            Console.WriteLine("Creating halls...");
            var halls = new List<Hall>();

            // Mid Valley - 4 halls
            halls.Add(GetOrCreateHall(outlet1.OutletId, "Hall 1", "Standard"));
            halls.Add(GetOrCreateHall(outlet1.OutletId, "Hall 2", "Premium"));
            halls.Add(GetOrCreateHall(outlet1.OutletId, "Hall 3", "IMAX"));
            halls.Add(GetOrCreateHall(outlet1.OutletId, "Hall 4", "4DX"));

            // 1 Utama - 3 halls
            halls.Add(GetOrCreateHall(outlet2.OutletId, "Hall 1", "Standard"));
            halls.Add(GetOrCreateHall(outlet2.OutletId, "Hall 2", "Premium"));
            halls.Add(GetOrCreateHall(outlet2.OutletId, "Hall 3", "IMAX"));

            // Pavilion - 2 halls
            halls.Add(GetOrCreateHall(outlet3.OutletId, "Hall 1", "Standard"));
            halls.Add(GetOrCreateHall(outlet3.OutletId, "Hall 2", "Premium"));

            Console.WriteLine($"✅ Total halls: {halls.Count}");

            // 3. CREATE MOVIES
            Console.WriteLine("Creating movies...");
            var movies = new List<Movie>
        {
            GetOrCreateMovie("Avengers: Endgame", "Action, Adventure, Sci-Fi", 181, "P13",
                "After the devastating events of Avengers: Infinity War, the universe is in ruins.",
                "https://www.youtube.com/watch?v=TcMBFSGVi1c"),

            GetOrCreateMovie("The Dark Knight", "Action, Crime, Drama", 152, "P13",
                "When the menace known as the Joker wreaks havoc and chaos on the people of Gotham.",
                "https://www.youtube.com/watch?v=EXeTwQWrcwY"),

            GetOrCreateMovie("Inception", "Action, Sci-Fi, Thriller", 148, "P13",
                "A thief who steals corporate secrets through the use of dream-sharing technology.",
                "https://www.youtube.com/watch?v=YoHD9XEInc0"),

            GetOrCreateMovie("Interstellar", "Adventure, Drama, Sci-Fi", 169, "P13",
                "A team of explorers travel through a wormhole in space in an attempt to ensure humanity's survival.",
                "https://www.youtube.com/watch?v=zSWdZVtXT7E"),

            GetOrCreateMovie("Parasite", "Comedy, Drama, Thriller", 132, "18",
                "Greed and class discrimination threaten the newly formed symbiotic relationship.",
                "https://www.youtube.com/watch?v=5xH0HfJHsaY")
        };
            Console.WriteLine($"✅ Movies ready: {movies.Count}");

            // 4. CREATE SHOWTIMES
            Console.WriteLine("Creating showtimes...");
            var showtimesCreated = CreateShowtimes(movies, halls);
            Console.WriteLine($"✅ Showtimes created: {showtimesCreated}");

            // 5. CREATE TEST MEMBER
            Console.WriteLine("Creating test member...");
            var testMember = GetOrCreateTestMember();
            Console.WriteLine($"✅ Test member ready: {testMember.Name}");

            // 6. CREATE SAMPLE BOOKINGS
            Console.WriteLine("Creating sample bookings...");
            var bookingsCreated = CreateSampleBookings(testMember);
            Console.WriteLine($"✅ Sample bookings: {bookingsCreated}");

            TempData["Info"] = $"✅ Test data created successfully! " +
                              $"{movies.Count} movies, {halls.Count} halls, " +
                              $"{showtimesCreated} showtimes, {bookingsCreated} sample bookings.";

            return RedirectToAction("TestMovieList");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
            TempData["Error"] = $"Error: {ex.Message}. Inner: {ex.InnerException?.Message}";
            return Content($"Error: {ex.Message}<br><br>Inner: {ex.InnerException?.Message}<br><br>{ex.StackTrace}", "text/html");
        }
    }

    // Helper methods
    private Outlet GetOrCreateOutlet(string name, string city)
    {
        var outlet = db.Outlets.FirstOrDefault(o => o.Name == name);
        if (outlet == null)
        {
            outlet = new Outlet { City = city, Name = name };
            db.Outlets.Add(outlet);
            db.SaveChanges();
            Console.WriteLine($"  ✅ Created outlet: {name}");
        }
        else
        {
            Console.WriteLine($"  ⏭️ Outlet exists: {name}");
        }
        return outlet;
    }

    private Hall GetOrCreateHall(int outletId, string name, string type)
    {
        var hall = db.Halls.FirstOrDefault(h => h.Name == name && h.OutletId == outletId);
        if (hall == null)
        {
            hall = new Hall
            {
                Name = name,
                OutletId = outletId,
                Capacity = 50,
                HallType = type,
                IsActive = true
            };
            db.Halls.Add(hall);
            db.SaveChanges();

            // Create seats
            CreateSeatsForHall(hall.HallId);
            Console.WriteLine($"  ✅ Created hall: {name} ({type}) with 50 seats");
        }
        else
        {
            Console.WriteLine($"  ⏭️ Hall exists: {name} ({type})");
        }
        return hall;
    }

    private Movie GetOrCreateMovie(string title, string genre, int duration, string rating, string description, string trailer)
    {
        var movie = db.Movies.FirstOrDefault(m => m.Title == title);
        if (movie == null)
        {
            movie = new Movie
            {
                Title = title,
                Description = description,
                Genre = genre,
                DurationMinutes = duration,
                Rating = rating,
                Director = "Test Director",
                Writer = "Test Writer",
                PremierDate = DateTime.Now.AddDays(-30),
                PosterUrl = $"/images/movies/{title.ToLower().Replace(" ", "-")}.jpg",
                BannerUrl = $"/images/movies/{title.ToLower().Replace(" ", "-")}-banner.jpg",
                TrailerUrl = trailer
            };
            db.Movies.Add(movie);
            db.SaveChanges();
            Console.WriteLine($"  ✅ Created movie: {title}");
        }
        else
        {
            Console.WriteLine($"  ⏭️ Movie exists: {title}");
        }
        return movie;
    }

    private int CreateShowtimes(List<Movie> movies, List<Hall> halls)
    {
        var today = DateTime.Today;
        var timeSlots = new[] { 10, 13, 16, 19, 21 };
        var count = 0;

        foreach (var movie in movies)
        {
            for (int day = 0; day < 7; day++)
            {
                var date = today.AddDays(day);

                // Each movie shows in 2-3 random halls per day
                var selectedHalls = halls.OrderBy(x => Guid.NewGuid()).Take(3).ToList();

                foreach (var hall in selectedHalls)
                {
                    // 2 showtimes per hall per day
                    var selectedTimes = timeSlots.OrderBy(x => Guid.NewGuid()).Take(2).ToList();

                    foreach (var hour in selectedTimes)
                    {
                        var startTime = date.AddHours(hour);

                        // Check if exists
                        if (!db.ShowTimes.Any(st => st.MovieId == movie.MovieId &&
                                                    st.HallId == hall.HallId &&
                                                    st.StartTime == startTime))
                        {
                            decimal price = hall.HallType switch
                            {
                                "Premium" => 20.00m,
                                "IMAX" => 25.00m,
                                "4DX" => 30.00m,
                                _ => 15.00m
                            };

                            if (hour >= 19) price += 3.00m;

                            db.ShowTimes.Add(new ShowTime
                            {
                                MovieId = movie.MovieId,
                                HallId = hall.HallId,
                                StartTime = startTime,
                                TicketPrice = price,
                                IsActive = true
                            });
                            count++;
                        }
                    }
                }
            }
        }

        db.SaveChanges();
        return count;
    }

    private Member GetOrCreateTestMember()
    {
        var member = db.Members.FirstOrDefault(m => m.Email == "test@example.com");
        if (member == null)
        {
            member = new Member
            {
                UserId = "M" + DateTime.Now.Ticks.ToString().Substring(0, 4),
                Name = "Test User",
                Email = "test@example.com",
                Gender = "M",
                PasswordHash = "test123", // Simple password for testing
                Phone = "0123456789",
                PhotoURL = "/images/default-avatar.jpg"
            };
            db.Members.Add(member);
            db.SaveChanges();
        }
        return member;
    }

    private int CreateSampleBookings(Member member)
    {
        var count = 0;
        var sampleShowtimes = db.ShowTimes
            .Include(st => st.Hall)
                .ThenInclude(h => h.Seats)
            .Take(3)
            .ToList();

        foreach (var showtime in sampleShowtimes)
        {
            if (!db.Bookings.Any(b => b.ShowTimeId == showtime.ShowTimeId))
            {
                var seatsToBook = showtime.Hall.Seats
                    .Where(s => s.SeatIdentifier == "A1" || s.SeatIdentifier == "A2")
                    .ToList();

                if (seatsToBook.Any())
                {
                    var booking = new Booking
                    {
                        MemberId = member.UserId,
                        ShowTimeId = showtime.ShowTimeId,
                        TicketQuantity = seatsToBook.Count,
                        TotalPrice = seatsToBook.Count * showtime.TicketPrice,
                        BookingDate = DateTime.Now
                    };
                    db.Bookings.Add(booking);
                    db.SaveChanges();

                    foreach (var seat in seatsToBook)
                    {
                        db.BookingSeats.Add(new BookingSeat
                        {
                            BookingId = booking.BookingId,
                            SeatId = seat.SeatId
                        });
                    }
                    db.SaveChanges();
                    count++;
                }
            }
        }
        return count;
    }

    private void CreateSeatsForHall(int hallId)
    {
        if (db.Seats.Any(s => s.HallId == hallId))
        {
            return; // Seats already exist
        }

        for (int row = 0; row < 5; row++)
        {
            for (int col = 1; col <= 10; col++)
            {
                db.Seats.Add(new Seat
                {
                    HallId = hallId,
                    SeatIdentifier = $"{(char)('A' + row)}{col}",
                    IsPremium = row >= 3,
                    IsWheelchair = row == 0 && (col == 1 || col == 10),
                    IsActive = true
                });
            }
        }
        db.SaveChanges();
    }

    [HttpGet]
    public IActionResult TestMovieList()
    {
        var movies = db.Movies.OrderBy(m => m.Title).ToList();
        return View(movies);
    }

    [HttpGet]
    public IActionResult CleanTestData()
    {
        try
        {
            Console.WriteLine("🧹 Cleaning test data...");

            // Remove in reverse order of dependencies
            db.BookingSeats.RemoveRange(db.BookingSeats);
            db.SaveChanges();

            db.Bookings.RemoveRange(db.Bookings);
            db.SaveChanges();

            db.ShowTimes.RemoveRange(db.ShowTimes);
            db.SaveChanges();

            db.Seats.RemoveRange(db.Seats);
            db.SaveChanges();

            db.Halls.RemoveRange(db.Halls);
            db.SaveChanges();

            db.Outlets.RemoveRange(db.Outlets);
            db.SaveChanges();

            // Don't remove movies if you want to keep them
            // db.Movies.RemoveRange(db.Movies);
            // db.SaveChanges();

            TempData["Info"] = "✅ All test data cleaned successfully!";
            return RedirectToAction("TestMovieList");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error cleaning data: {ex.Message}";
            return RedirectToAction("TestMovieList");
        }
    }
}