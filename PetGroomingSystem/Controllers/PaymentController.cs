using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetGroomingSystem.Models;
using Stripe;
using Stripe.Checkout;

namespace PetGroomingSystem.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDb _context;
        private readonly IConfiguration _config;

        public PaymentController(ApplicationDb context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost]
        public IActionResult CreateCheckoutSession(int appointmentId)
        {
            var appointment = _context.Appointments
                .Include(a => a.Service)
                .FirstOrDefault(a => a.AppointmentID == appointmentId);

            if (appointment == null) return NotFound();

            StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];

            // Calculate price in cents (e.g. RM 50.00 -> 5000)
            long priceInCents = (long)((appointment.Service?.Price ?? appointment.TotalPrice) * 100);

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = priceInCents,
                            Currency = "myr",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Grooming Service: {appointment.Service?.ServiceName ?? "Grooming"}",
                            },
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                SuccessUrl = Url.Action("Success", "Payment", new { appointmentId }, Request.Scheme) + "&session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = Url.Action("Cancel", "Payment", null, Request.Scheme),
            };

            var service = new SessionService();
            Session session = service.Create(options);

            appointment.StripeSessionId = session.Id;
            _context.SaveChanges();

            return Redirect(session.Url);
        }

        public IActionResult Success(int appointmentId, string session_id)
        {
            var appointment = _context.Appointments.FirstOrDefault(a => a.AppointmentID == appointmentId);
            if (appointment != null)
            {
                appointment.IsPaid = true;
                _context.SaveChanges();
            }

            ViewBag.AppointmentID = appointmentId;
            return View();
        }

        public IActionResult Cancel()
        {
            return View();
        }
    }
}