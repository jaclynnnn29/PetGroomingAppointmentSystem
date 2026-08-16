using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetGroomingSystem.Models;

namespace PetGroomingSystem.Controllers
{
    // Restrict access so only admins can execute these dashboard actions
    [Authorize(Roles = "Admin")]
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

        // Action: Manage Appointments (UPDATED TO ACCEPT FILTER PARAMETER)
        public async Task<IActionResult> Appointments(string filter = "all")
        {
            var query = _context.Appointments
                .Include(a => a.GroomingService)
                .AsQueryable();

            if (filter == "paid")
            {
                // Filter only paid bookings
                query = query.Where(a => a.IsPaid);
            }
            else if (filter == "revenue")
            {
                // Filter revenue-generating active/paid bookings
                query = query.Where(a => a.IsPaid && a.Status != "Cancelled");
            }

            var appointments = await query
                .OrderByDescending(a => a.Date)
                .ToListAsync();

            ViewBag.CurrentFilter = filter;
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