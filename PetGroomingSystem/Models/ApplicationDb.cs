using Microsoft.EntityFrameworkCore;

namespace PetGroomingSystem.Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<GroomingService> GroomingServices { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
}