using System.Net;
using System.Net.Mail;

namespace PetGroomingSystem.Services;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body);
}

public class EmailService : IEmailService
{
    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        // Replace these with your actual sender email details
        var senderEmail = "teddypetgroomingsystem@gmail.com";
        var appPassword = "frzajrmujkjpxqrj"; // Use an App Password, not your real password!

        var client = new SmtpClient("smtp.gmail.com", 587)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(senderEmail, appPassword)
        };

        var mailMessage = new MailMessage(
            from: senderEmail,
            to: toEmail,
            subject: subject,
            body: body
        )
        {
            IsBodyHtml = true // Allows us to send nice-looking HTML emails
        };

        await client.SendMailAsync(mailMessage);
    }
}