using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PetGroomingSystem.Models;
using PetGroomingSystem.ViewModels;

namespace PetGroomingSystem.Controllers;

public class BookingController(ApplicationDbContext db) : Controller
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
    public IActionResult Create(BookingAppointmentVM vm)
    {
        if (ModelState.IsValid)
        {
            var appointment = new Appointment
            {
                MemberEmail = User.Identity?.Name ?? "member@example.com",
                GroomingServiceId = vm.ServiceId,
                PetType = vm.PetType,
                PetName = vm.PetName,
                Date = DateOnly.FromDateTime(vm.AppointmentDate!.Value),
                TimeSlot = vm.TimeSlot,
                SpecialRequests = vm.SpecialRequests,
                Status = "Confirmed"
            };

            db.Appointments.Add(appointment);
            db.SaveChanges();

            return RedirectToAction("BookingComplete", new { id = appointment.Id });
        }

        ViewBag.ServicesList = new SelectList(db.GroomingServices, "Id", "Name", vm.ServiceId);
        return View(vm);
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
        var email = User.Identity?.Name ?? "";
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
        var email = User.Identity?.Name ?? "";
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