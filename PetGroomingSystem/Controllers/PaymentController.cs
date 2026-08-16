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

            // Default to minimum 1.00 if price is 0 to avoid Stripe exception
            decimal servicePrice = appointment.GroomingService.Price > 0 ? appointment.GroomingService.Price : 1.00m;
            long priceInCents = (long)(servicePrice * 100);

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

            var serviceName = appointment.GroomingService?.Name ?? "Grooming Service";
            var price = appointment.GroomingService?.Price ?? 0;

            var customerName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                            ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                            ?? "Valued Customer";

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(35);
                    page.PageColor("#EBF5FF");
                    page.DefaultTextStyle(x => x.FontSize(10).FontColor("#1E293B"));

                    // 1. CENTERED HEADER SECTION
                    page.Header().Column(header =>
                    {
                        // Centered Shop Info Header
                        header.Item().Column(col =>
                        {
                            col.Item().AlignCenter().Text("🐶 🐱 🐾 TEDDY PET GROOMING").ExtraBold().FontSize(20).FontColor("#1E3A8A");
                            col.Item().AlignCenter().Text("No. 28, Jalan 3/23A, Taman Setapak Indah,").FontSize(9).FontColor("#475569");
                            col.Item().AlignCenter().Text("Setapak, 53300 Kuala Lumpur, Wilayah Persekutuan Kuala Lumpur").FontSize(9).FontColor("#475569");
                            col.Item().AlignCenter().Text("+60 12-345 6789 | teddypetgroomingsystem@gmail.com").FontSize(9).FontColor("#475569");
                        });

                        header.Item().PaddingVertical(10).LineHorizontal(1).LineColor("#BFDBFE");

                        // Details Block (Customer on Left | Invoice Metadata on Right)
                        header.Item().Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text($"TO: {customerName}").Bold().FontSize(11).FontColor("#1E3A8A");
                                col.Item().Text($"Pet Name: {appointment.PetName}").Bold().FontSize(10);
                                col.Item().Text($"Appointment Date: {appointment.Date:yyyy-MM-dd} ({appointment.TimeSlot})").FontSize(9).FontColor("#334155");
                            });

                            row.RelativeItem().AlignRight().Column(col =>
                            {
                                col.Item().Text("INVOICE").ExtraBold().FontSize(15).FontColor("#2563EB");
                                col.Item().Text($"No: REC-{appointment.Id:D6}").Bold().FontSize(10);
                                col.Item().Text($"Date: {DateTime.Now:dd/MM/yyyy}").FontSize(9);
                            });
                        });
                    });

                    // 2. MAIN TABLE CONTENT SECTION
                    page.Content().PaddingVertical(15).Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background("#1E3A8A").Padding(8).Text("NAME OF SERVICES").Bold().FontColor(Colors.White);
                                header.Cell().Background("#1E3A8A").Padding(8).AlignRight().Text("PRICE").Bold().FontColor(Colors.White);
                                header.Cell().Background("#1E3A8A").Padding(8).AlignCenter().Text("QTY").Bold().FontColor(Colors.White);
                                header.Cell().Background("#1E3A8A").Padding(8).AlignRight().Text("TOTAL").Bold().FontColor(Colors.White);
                            });

                            table.Cell().Background("#DBEAFE").Padding(8).Text(serviceName);
                            table.Cell().Background("#DBEAFE").Padding(8).AlignRight().Text($"RM {price:F2}");
                            table.Cell().Background("#DBEAFE").Padding(8).AlignCenter().Text("1");
                            table.Cell().Background("#DBEAFE").Padding(8).AlignRight().Text($"RM {price:F2}");
                        });

                        col.Item().PaddingTop(20).Row(row =>
                        {
                            row.RelativeItem().Column(left =>
                            {
                                left.Item().Text("PAYMENT METHOD").Bold().FontSize(10).FontColor("#1E3A8A");
                                left.Item().Text("Card Payment ").FontSize(9);
                                left.Item().Text("Status: PAID ✓").Bold().FontColor(Colors.Green.Medium).FontSize(11);
                            });

                            row.RelativeItem().AlignRight().Column(right =>
                            {
                                right.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("SUBTOTAL:").Bold();
                                    r.RelativeItem().AlignRight().Text($"RM {price:F2}");
                                });
                                right.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("TAX (0%):").Bold();
                                    r.RelativeItem().AlignRight().Text("RM 0.00");
                                });
                                right.Item().PaddingVertical(4).LineHorizontal(1).LineColor("#93C5FD");
                                right.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("TOTAL:").ExtraBold().FontSize(12).FontColor("#1E3A8A");
                                    r.RelativeItem().AlignRight().Text($"RM {price:F2}").ExtraBold().FontSize(12).FontColor("#1E3A8A");
                                });
                            });
                        });

                        col.Item().PaddingTop(30).Column(terms =>
                        {
                            terms.Item().Text("TERMS AND CONDITIONS").Bold().FontSize(9).FontColor("#1E3A8A");
                            terms.Item().Text("1. This receipt is computer-generated upon payment confirmation.").FontSize(8).FontColor("#64748B");
                            terms.Item().Text("2. Please show this receipt during your appointment check-in.").FontSize(8).FontColor("#64748B");
                        });
                    });

                    // 3. FOOTER
                    page.Footer().AlignCenter().Text("Thank you for your booking! 🐶🐱").Bold().FontSize(11).FontColor("#1E3A8A");
                });
            });

            byte[] pdfBytes = document.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"Receipt_Appointment_{appointmentId}.pdf");
        }
    }
}