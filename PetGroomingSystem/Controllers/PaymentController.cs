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

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(35);
                    page.PageColor("#EBF5FF"); // Pastel light blue background theme
                    page.DefaultTextStyle(x => x.FontSize(10).FontColor("#1E293B"));

                    // 1. HEADER SECTION
                    page.Header().Column(header =>
                    {
                        header.Item().Row(row =>
                        {
                            // Left: Shop Info
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("🐶🐱🐾 TEDDY PET GROOMING ").ExtraBold().FontSize(18).FontColor("#1E3A8A");
                                col.Item().Text("No. 28, Jalan 3/23A,\r\n\r\nTaman Setapak Indah,\r\n\r\nSetapak, 53300 Kuala Lumpur,\r\n\r\nWilayah Persekutuan Kuala Lumpur").FontSize(9).FontColor("#475569");
                                col.Item().Text("+60 12-345 6789 | teddypetgroomingsystem@gmail.com").FontSize(9).FontColor("#475569");
                            });

                            // Right: Invoice Title & Number
                            row.RelativeItem().AlignRight().Column(col =>
                            {
                                col.Item().Text("INVOICE / RECEIPT").ExtraBold().FontSize(18).FontColor("#2563EB");
                                col.Item().Text($"No: REC-{appointment.Id:D6}").Bold().FontSize(10);
                                col.Item().Text($"Date: {DateTime.Now:dd/MM/yyyy}").FontSize(9);
                            });
                        });

                        header.Item().PaddingVertical(10).LineHorizontal(1).LineColor("#BFDBFE");

                        // Customer & Appointment Info Block
                        header.Item().Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("TO:").Bold().FontSize(10).FontColor("#1E3A8A");
                                col.Item().Text($"Pet Name: {appointment.PetName}").Bold().FontSize(11);
                                col.Item().Text($"Appointment Date: {appointment.Date:yyyy-MM-dd} ({appointment.TimeSlot})").FontSize(9).FontColor("#334155");
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
                                columns.RelativeColumn(3); // Item Name
                                columns.RelativeColumn(1); // Price
                                columns.RelativeColumn(1); // Qty
                                columns.RelativeColumn(1); // Total
                            });

                            // Styled Table Header
                            table.Header(header =>
                            {
                                header.Cell().Background("#1E3A8A").Padding(8).Text("ITEM NAME").Bold().FontColor(Colors.White);
                                header.Cell().Background("#1E3A8A").Padding(8).AlignRight().Text("PRICE").Bold().FontColor(Colors.White);
                                header.Cell().Background("#1E3A8A").Padding(8).AlignCenter().Text("QTY").Bold().FontColor(Colors.White);
                                header.Cell().Background("#1E3A8A").Padding(8).AlignRight().Text("TOTAL").Bold().FontColor(Colors.White);
                            });

                            // Service Line Item
                            table.Cell().Background("#DBEAFE").Padding(8).Text(serviceName);
                            table.Cell().Background("#DBEAFE").Padding(8).AlignRight().Text($"RM {price:F2}");
                            table.Cell().Background("#DBEAFE").Padding(8).AlignCenter().Text("1");
                            table.Cell().Background("#DBEAFE").Padding(8).AlignRight().Text($"RM {price:F2}");
                        });

                        col.Item().PaddingTop(20).Row(row =>
                        {
                            // Left: Payment Method & Status
                            row.RelativeItem().Column(left =>
                            {
                                left.Item().Text("PAYMENT METHOD").Bold().FontSize(10).FontColor("#1E3A8A");
                                left.Item().Text("Card Payment (Stripe Sandbox)").FontSize(9);
                                left.Item().Text("Status: PAID ✓").Bold().FontColor(Colors.Green.Medium).FontSize(11);
                            });

                            // Right: Subtotal and Total Summary
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

                        // Terms and Conditions
                        col.Item().PaddingTop(30).Column(terms =>
                        {
                            terms.Item().Text("TERMS AND CONDITIONS").Bold().FontSize(9).FontColor("#1E3A8A");
                            terms.Item().Text("1. This receipt is computer-generated upon payment confirmation.").FontSize(8).FontColor("#64748B");
                            terms.Item().Text("2. Please present this receipt during your appointment check-in.").FontSize(8).FontColor("#64748B");
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