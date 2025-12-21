global using AssignmentC_.Models;
using AssignmentC_;
using AssignmentC_.Hubs;
using Rotativa.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSqlServer<DB>($@"
    Data Source=(LocalDB)\MSSQLLocalDB;
    AttachDbFilename={builder.Environment.ContentRootPath}\DB.mdf;
");

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddSignalR();
builder.Services.AddScoped<Helper>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDataProtection();
builder.Services.AddMemoryCache();

// =======================
// Add Cookie Authentication
// =======================
builder.Services.AddAuthentication("MyCookieAuth")
    .AddCookie("MyCookieAuth", options =>
    {
        options.LoginPath = "/User/Login"; // redirect here if not logged in
        options.AccessDeniedPath = "/User/AccessDenied"; // optional

        // Redirect with returnUrl
        options.Events.OnRedirectToLogin = context =>
        {
            var returnUrl = context.Request.Path + context.Request.QueryString;
            context.Response.Redirect($"/User/Login?returnUrl={returnUrl}");
            return Task.CompletedTask;
        };
    });
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
var app = builder.Build();

app.UseSession();
app.UseRouting();
app.UseHttpsRedirection();
app.UseStaticFiles();

// Enable Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultControllerRoute();
app.MapHub<SeatHub>("/seatHub");
app.Run();