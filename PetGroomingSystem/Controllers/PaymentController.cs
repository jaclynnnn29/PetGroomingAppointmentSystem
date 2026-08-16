using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetGroomingSystem.Models;
using Stripe;
using Stripe.Checkout;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PetGroomingSystem.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public PaymentController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpGet]
        public IActionResult CreateCheckoutSession(int appointmentId)
        {
            var appointment = _context.Appointments
                .Include(a => a.GroomingService)
                .FirstOrDefault(a => a.Id == appointmentId);

            if (appointment == null || appointment.GroomingService == null)
            {
                return NotFound();
            }

            StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];

            // Calculate price in cents (e.g., RM 50.00 -> 5000 cents)
            long priceInCents = (long)(appointment.GroomingService.Price * 100);

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
                                Name = $"Grooming: {appointment.GroomingService.Name} ({appointment.PetName})",
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
            var appointment = _context.Appointments.FirstOrDefault(a => a.Id == appointmentId);
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

        [HttpGet]
        public IActionResult DownloadReceipt(int appointmentId)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var appointment = _context.Appointments
                .Include(a => a.GroomingService)
                .FirstOrDefault(a => a.Id == appointmentId);

            if (appointment == null)
            {
                return NotFound();
            }

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(50);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .Text("PET GROOMING RECEIPT")
                        .SemiBold().FontSize(24).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(20)
                        .Column(x =>
                        {
                            x.Spacing(10);

                            x.Item().Text($"Receipt #: REC-{appointment.Id:D6}").Bold();
                            x.Item().Text($"Date: {DateTime.Now:dd/MM/yyyy HH:mm}");
                            x.Item().Text($"Pet Name: {appointment.PetName}");

                            x.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                            x.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Text("Service").Bold();
                                    header.Cell().Text("Price").Bold();
                                });

                                table.Cell().Text(appointment.GroomingService?.Name ?? "Grooming Service");
                                table.Cell().Text($"RM {appointment.GroomingService?.Price:F2}");
                            });

                            x.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                            x.Item().AlignRight().Text($"Total Paid: RM {appointment.GroomingService?.Price:F2}").Bold().FontSize(14);
                            x.Item().AlignRight().Text("Status: PAID ✓").Bold().FontColor(Colors.Green.Medium);
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text("Thank you for using our Pet Grooming Service!")
                        .FontSize(10).FontColor(Colors.Grey.Medium);
                });
            });

            byte[] pdfBytes = document.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"Receipt_Appointment_{appointmentId}.pdf");
        }
    }
}