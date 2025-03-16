using BCrypt.Net;
using EducationPlatformN.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Session;
using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure Entity Framework Core with SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found in appsettings.json.");
}
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add session services with secure options
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session timeout
    options.Cookie.HttpOnly = true; // Protect cookies from JavaScript access
    options.Cookie.IsEssential = true; // Make cookies essential
    options.Cookie.SameSite = SameSiteMode.Strict; // Increase security
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Ensure cookies are sent over HTTPS
});

// Add IHttpContextAccessor for accessing HttpContext in controllers
builder.Services.AddHttpContextAccessor();

// Add DatabaseDeveloperPageExceptionFilter for development
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();
}

// Configure logging
builder.Services.AddLogging();

// Build the application
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // Ensure proper error handling in development
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // Enable HSTS for security in production
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "uploads")),
    RequestPath = "/uploads"
}); // Serve files from the uploads folder
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "invoices")),
    RequestPath = "/invoices"
}); // Serve files from the invoices folder

// Ensure routing, session, and authorization middleware are in the correct order
app.UseRouting();
app.UseSession(); // Must be before UseAuthorization
app.UseAuthorization();

// Ensure database is created and migrated at startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate(); // Apply pending migrations
}

// Configure controller routes with explicit method support
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "admin",
    pattern: "Admin/{action=Dashboard}/{id?}",
    defaults: new { controller = "Admin", action = "Dashboard" });

// Remove MapFallbackToController as it may conflict with POST requests
// app.MapFallbackToController("AddUser", "Admin");

app.Run();