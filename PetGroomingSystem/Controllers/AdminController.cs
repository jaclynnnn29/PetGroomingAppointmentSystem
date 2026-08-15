using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetGroomingSystem.Models;

namespace PetGroomingSystem.Controllers
{
    // Restrict access so only admins can execute these dashboard actions
    // [Authorize(Roles = "Admin")] 
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Action: Dashboard Main Page
        public IActionResult Index()
        {
            return View();
        }

        // Action: Manage Services
        public async Task<IActionResult> Services()
        {
            var services = await _context.GroomingServices.ToListAsync();
            return View(services);
        }

        // Action: Manage Appointments
        public async Task<IActionResult> Appointments()
        {
            var appointments = await _context.Appointments
                .Include(a => a.GroomingService)
                .OrderByDescending(a => a.Date)
                .ToListAsync();

            return View(appointments);
        }

        // Action: Approve/Cancel Appointment
        [HttpPost]
        public async Task<IActionResult> UpdateAppointmentStatus(int id, string status)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                appointment.Status = status; // e.g., "Confirmed", "Cancelled"
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Appointments));
        }

        // Action: Manage Users
        public async Task<IActionResult> Users()
        {
            var users = await _context.Members.ToListAsync();
            return View(users);
        }
    }
}