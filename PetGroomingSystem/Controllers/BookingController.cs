using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PetGroomingSystem.Models;
using PetGroomingSystem.ViewModels;

namespace PetGroomingSystem.Controllers;

public class BookingController(ApplicationDbContext db) : Controller
{
    public IActionResult Index()
    {
        var services = db.GroomingServices.ToList();
        return View(services);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.ServicesList = new SelectList(db.GroomingServices, "Id", "Name");
        return View();
    }

    [HttpPost]
    public IActionResult Create(AppointmentBookingVM vm)
    {
        if (ModelState.IsValid)
        {
            var appointment = new Appointment
            {
                MemberEmail = User.Identity?.Name ?? "guest@example.com",
                GroomingServiceId = vm.ServiceId,
                Date = DateOnly.FromDateTime(vm.AppointmentDate!.Value),
                TimeSlot = vm.TimeSlot,
                SpecialRequests = vm.SpecialRequests
            };

            db.Appointments.Add(appointment);
            db.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        ViewBag.ServicesList = new SelectList(db.GroomingServices, "Id", "Name", vm.ServiceId);
        return View(vm);
    }
}