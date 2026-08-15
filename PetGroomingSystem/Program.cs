using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PetGroomingSystem.Models;
using PetGroomingSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// 1. Add Session Service
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Cookie Authentication Configuration
builder.Services.AddAuthentication("MyCookieAuth")
    .AddCookie("MyCookieAuth", options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    });

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddTransient<IEmailService, EmailService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 2. Enable Session Middleware (must be before UseAuthentication & UseAuthorization)
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Seed Database Safely
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Apply migrations automatically
    db.Database.Migrate();

    // Seed Admin Account
    if (!db.Members.Any(m => m.Email == "admin@petgrooming.com"))
    {
        var admin = new Member
        {
            Name = "System Admin",
            Email = "admin@petgrooming.com",
            Phone = "0123456789",
            Role = "Admin",
            FailedLoginAttempts = 0,
            LockedUntil = null
        };

        var passwordHasher = new PasswordHasher<Member>();

        admin.PasswordHash = passwordHasher.HashPassword(
            admin,
            "Admin123!"
        );

        db.Members.Add(admin);
        db.SaveChanges();
    }

    if (!db.GroomingServices.Any())
{
    db.GroomingServices.AddRange(
        new GroomingService
        {
            Name = "Basic Grooming",
            Price = 50.00m,
            Description = "Includes bath, blow-dry, and nail trim."
        },
        new GroomingService
        {
            Name = "Full Grooming",
            Price = 90.00m,
            Description = "Full hair cut, sanitary trim, and ear cleaning."
        },
        new GroomingService
        {
            Name = "Medicated Bath",
            Price = 80.00m,
            Description = "A specialty bath using medicated shampoo designed for pets with skin issues, fungus, or ringworm."
        },
        new GroomingService
        {
            Name = "Lion Cut",
            Price = 110.00m,
            Description = "A full body shave that leaves the face, mane, legs, and the tip of the tail intact for a majestic look."
        },
        new GroomingService
        {
            Name = "Premium Spa",
            Price = 130.00m,
            Description = "A relaxing treatment featuring an oatmeal scrub, aromatic salt bath, or Ayurveda mud mask."
        },
        // 6th Service added here:
        new GroomingService
        {
            Name = "De-Shedding & Dental",
            Price = 70.00m,
            Description = "Undercoat blowout, deep brush-out, and fresh breath dental cleaning."
        }
    );

    db.SaveChanges();
}
}

app.Run();