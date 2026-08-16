using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PetGroomingSystem.Models;
using PetGroomingSystem.ViewModels;
using PetGroomingSystem.Services;

namespace PetGroomingSystem.Controllers;

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

    // POST: Booking/Create (Processes Appointment - Saves & Redirects to Payment)
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(BookingAppointmentVM vm)
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

            // ==========================================
            // REDIRECT TO STRIPE PAYMENT CHECKOUT
            // ==========================================
            return RedirectToAction("CreateCheckoutSession", "Payment", new { appointmentId = appointment.Id });
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

    // POST: Booking/CancelAppointment
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CancelAppointment(int id)
    {
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "";

        var appointment = await db.Appointments
            .Include(a => a.GroomingService)
            .FirstOrDefaultAsync(a => a.Id == id && a.MemberEmail == email);

        if (appointment != null && appointment.Status != "Cancelled")
        {
            appointment.Status = "Cancelled";
            await db.SaveChangesAsync();

            // Send Cancellation Confirmation Email
            var subject = "❌ Appointment Cancelled - Teddy PetGrooming";
            var body = $@"
                <h3>Appointment Cancellation Confirmed</h3>
                <p>Your appointment <strong>#{appointment.Id}</strong> for <strong>{appointment.PetName}</strong> on <strong>{appointment.Date:yyyy-MM-dd} at {appointment.TimeSlot}</strong> has been cancelled.</p>
                {(appointment.IsPaid ? "<p><em>Since this was a paid booking, our support team will process your refund shortly.</em></p>" : "")}
                <p>We hope to see you and {appointment.PetName} again soon!</p>";

            await emailService.SendEmailAsync(email, subject, body);
        }

        return RedirectToAction("Appointments");
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