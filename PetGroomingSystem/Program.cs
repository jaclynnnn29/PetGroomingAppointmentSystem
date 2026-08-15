using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PetGroomingSystem.Models;
using PetGroomingSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

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
                Name = "Basic Bath & Brush",
                Price = 50.00m,
                Description = "Includes bath, blow-dry, and nail trim."
            },
            new GroomingService
            {
                Name = "Full Styling & Haircut",
                Price = 90.00m,
                Description = "Full hair cut, sanitary trim, and ear cleaning."
            }
        );

        db.SaveChanges();
    }

    

}

app.Run();