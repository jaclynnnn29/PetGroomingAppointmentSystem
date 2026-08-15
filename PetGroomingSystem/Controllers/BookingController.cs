using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PetGroomingSystem.Models;
using PetGroomingSystem.ViewModels;
using PetGroomingSystem.Services; // <-- Added this using statement

namespace PetGroomingSystem.Controllers;

// <-- Added IEmailService to the constructor injection
public class BookingController(ApplicationDbContext db, IEmailService emailService) : Controller
{
    // GET: Booking/Index (Services Catalog)
    public IActionResult Index()
    {
        var m = db.GroomingServices.ToList();

        if (Request.IsAjax()) return PartialView("_Index", m);

        return View(m);
    }

    // GET: Booking/Create (Renders Booking Form)
    [Authorize]
    public IActionResult Create(int? serviceId)
    {
        ViewBag.ServicesList = new SelectList(db.GroomingServices, "Id", "Name", serviceId);
        return View();
    }

    // POST: Booking/Create (Processes Appointment - Replaces Checkout)
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(BookingAppointmentVM vm) // <-- Changed to async Task<IActionResult>
    {
        if (ModelState.IsValid)
        {
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "member@example.com";

            var appointment = new Appointment
            {
                MemberEmail = userEmail,
                GroomingServiceId = vm.ServiceId,
                PetType = vm.PetType,
                PetName = vm.PetName,
                Date = DateOnly.FromDateTime(vm.AppointmentDate!.Value),
                TimeSlot = vm.TimeSlot,
                SpecialRequests = vm.SpecialRequests,
                Status = "Confirmed"
            };

            db.Appointments.Add(appointment);
            await db.SaveChangesAsync(); 

            // ==========================================
            // SEND THE CONFIRMATION EMAIL
            // ==========================================
            var subject = "🐾 Appointment Confirmed!";
            var body = $@"
                <h3>Thank you for booking with Teddy PetGrooming System!</h3>
                <p>Your appointment for <strong>{vm.PetName}</strong> is confirmed.</p>
                <ul>
                    <li><strong>Date:</strong> {appointment.Date.ToString("yyyy-MM-dd")}</li>
                    <li><strong>Time:</strong> {appointment.TimeSlot}</li>
                </ul>
                <p>We look forward to pampering your pet!</p>";

            // Send the email in the background
            await emailService.SendEmailAsync(userEmail, subject, body);

            return RedirectToAction("BookingComplete", new { id = appointment.Id });
        }

        ViewBag.ServicesList = new SelectList(db.GroomingServices, "Id", "Name", vm.ServiceId);
        return View(vm);
    }

    // ==========================================
    // AJAX: Get Available Time Slots
    // ==========================================
    [HttpGet]
    public IActionResult GetAvailableTimeSlots(string date)
    {
        if (!DateTime.TryParse(date, out DateTime selectedDate))
            return Json(new List<string>());

        var dateOnly = DateOnly.FromDateTime(selectedDate);

        // 1. Define all possible shop operating hours
        var allSlots = new List<string>
        {
            "10:00 AM", "11:30 AM", "02:00 PM", "03:30 PM", "05:00 PM"
        };

        // 2. Query the database to see which slots are already taken on this date
        var bookedSlots = db.Appointments
            .Where(a => a.Date == dateOnly && a.Status != "Cancelled")
            .Select(a => a.TimeSlot)
            .ToList();

        // 3. Remove the booked slots from the available list
        var availableSlots = allSlots.Except(bookedSlots).ToList();

        return Json(availableSlots);
    }

    // GET: Booking/BookingComplete (Replaces OrderComplete)
    [Authorize]
    public IActionResult BookingComplete(int id)
    {
        ViewBag.Id = id;
        return View();
    }

    // GET: Booking/Appointments (Replaces Order List)
    [Authorize]
    public IActionResult Appointments()
    {
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "";
        var m = db.Appointments
                  .Include(a => a.GroomingService)
                  .Where(a => a.MemberEmail == email)
                  .OrderByDescending(a => a.Id)
                  .ToList();

        if (Request.IsAjax()) return PartialView("_Appointments", m);

        return View(m);
    }

    // GET: Booking/AppointmentDetail (Replaces OrderDetail)
    [Authorize]
    public IActionResult AppointmentDetail(int id)
    {
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "";
        var m = db.Appointments
                  .Include(a => a.GroomingService)
                  .FirstOrDefault(a => a.Id == id && a.MemberEmail == email);

        if (m == null) return RedirectToAction("Appointments");

        return View(m);
    }

    // POST: Booking/ResetAll (Resets Test Records)
    [HttpPost]
    public IActionResult ResetAll()
    {
        // Delete all appointment records
        db.Appointments.ExecuteDelete();

        // Reseed identity column
        db.Database.ExecuteSqlRaw("DBCC CHECKIDENT (Appointments, RESEED, 0);");

        return RedirectToAction("Appointments");
    }
}