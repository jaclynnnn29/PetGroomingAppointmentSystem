using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PetGroomingSystem.Models;
using PetGroomingSystem.Services;
using PetGroomingSystem.ViewModels;
using System.Security.Claims;

namespace PetGroomingSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<Member> _passwordHasher;
        private readonly IEmailService _emailService;

        public AccountController(ApplicationDbContext context, IEmailService emailService)
            {
            _context = context;
            _passwordHasher = new PasswordHasher<Member>();
            _emailService = emailService; // <-- 3. Assign here
        }

        // =========================
        // REGISTER
        // =========================

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check whether email already exists
            var existingMember = _context.Members
                .FirstOrDefault(m => m.Email == model.Email);

            if (existingMember != null)
            {
                ModelState.AddModelError(
                    "Email",
                    "This email is already registered."
                );

                return View(model);
            }

            // Create new Member
            var member = new Member
            {
                Name = model.Name,
                Email = model.Email,
                Phone = model.Phone,

                // IMPORTANT:
                // Normal registration is always Customer
                Role = "Customer",

                FailedLoginAttempts = 0,
                LockedUntil = null
            };

            // Hash password
            member.PasswordHash =
                _passwordHasher.HashPassword(
                    member,
                    model.Password
                );

            // Save to database
            _context.Members.Add(member);
            _context.SaveChanges();

            TempData["SuccessMessage"] =
                "Registration successful! You can now login.";

            return RedirectToAction("Login");
        }


        // =========================
        // LOGIN PAGE
        // =========================

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            GenerateCaptcha();

            return View();
        }

        // =========================
        // LOGIN
        // =========================

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginVM model)
        {

            // =========================
            // CHECK CAPTCHA
            // =========================

            var correctAnswer = TempData["CaptchaAnswer"]?.ToString();

            if (string.IsNullOrEmpty(correctAnswer) ||
                string.IsNullOrEmpty(model.CaptchaUserAnswer) ||
                !model.CaptchaUserAnswer.Equals(
                    correctAnswer,
                    StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    "CaptchaUserAnswer",
                    "Incorrect CAPTCHA answer."
                );
            }

            if (!ModelState.IsValid)
            {
                GenerateCaptcha();
                return View(model);
            }

            var member = _context.Members
                .FirstOrDefault(m => m.Email == model.Email);

            // Email not found
            if (member == null)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                GenerateCaptcha();
                return View(model);
            }

            // Check whether account is temporarily locked
            if (member.LockedUntil.HasValue &&
                member.LockedUntil.Value > DateTime.Now)
            {
                ModelState.AddModelError(
                    "",
                    "Your account is temporarily locked. Please try again after 1 minute."
                );

                GenerateCaptcha();
                return View(model);
            }

            // Verify password
            var result = _passwordHasher.VerifyHashedPassword(
                member,
                member.PasswordHash,
                model.Password
            );

            if (result == PasswordVerificationResult.Failed)
            {
                member.FailedLoginAttempts++;
                
                // Lock after 3 failed attempts
                if (member.FailedLoginAttempts >= 3)
                {
                    member.LockedUntil = DateTime.Now.AddMinutes(1);
                    member.FailedLoginAttempts = 0;

                    _context.SaveChanges();

                    ModelState.AddModelError(
                        "",
                        "Too many failed attempts. Your account is locked for 1 minute."
                    );

                    GenerateCaptcha();
                    return View(model);
                }

                _context.SaveChanges();

                ModelState.AddModelError(
                    "",
                    $"Invalid email or password. Failed attempts: {member.FailedLoginAttempts}/3"
                );

                GenerateCaptcha();
                return View(model);
            }

            // Successful login
            member.FailedLoginAttempts = 0;
            member.LockedUntil = null;

            _context.SaveChanges();

            // Create authentication cookie
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, member.MemberID.ToString()),
                new Claim(ClaimTypes.Name, member.Name),
                new Claim(ClaimTypes.Email, member.Email),
                new Claim(ClaimTypes.Role, member.Role),
                new Claim("FullName", member.Name)          //Display full name
            };

            var identity = new ClaimsIdentity(
                claims,
                "MyCookieAuth"
            );

            var principal = new ClaimsPrincipal(identity);

            //Remember Me
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe
                    ? DateTimeOffset.UtcNow.AddDays(7)
                    : DateTimeOffset.UtcNow.AddHours(1)
            };

            HttpContext.SignInAsync(
                "MyCookieAuth",
                principal,
                authProperties
            ).GetAwaiter().GetResult();

            // Redirect according to role
            if (member.Role == "Admin")
            {
                return RedirectToAction("Index", "Admin");
            }

            return RedirectToAction("Index", "Home");
        }

        private void GenerateCaptcha()
        {
            const string characters = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

            var random = new Random();

            string captcha = "";

            for (int i = 0; i < 5; i++)
            {
                captcha += characters[random.Next(characters.Length)];
            }

            TempData["CaptchaAnswer"] = captcha;

            ViewData["CaptchaCode"] = captcha;
        }

        // =========================
        // LOGOUT
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.SignOutAsync("MyCookieAuth")
                .GetAwaiter()
                .GetResult();

            return RedirectToAction("Login", "Account");
        }

        // =========================
        // ACCESS DENIED
        // =========================

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // =========================
        // USER PREFERENCES
        // =========================

        [HttpPost]
        [Authorize]
        public IActionResult SavePreferences(string theme, string navOrder)
        {
            // Get logged-in user's email from claims
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return BadRequest();
            }

            var member = _context.Members.FirstOrDefault(m => m.Email == email);

            if (member != null)
            {
                if (!string.IsNullOrEmpty(theme))
                {
                    member.PreferredTheme = theme;
                }

                if (!string.IsNullOrEmpty(navOrder))
                {
                    member.NavOrder = navOrder;
                }

                _context.SaveChanges();
                return Json(new { success = true });
            }

            return BadRequest();
        }

        // GET: /Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword() => View();

        // POST: /Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Please enter your email.");
                return View();
            }

            var member = _context.Members.FirstOrDefault(m => m.Email == email);
            if (member != null)
            {
                // 1. Generate secure token & 15-minute expiration
                string token = Guid.NewGuid().ToString();
                member.ResetToken = token;
                member.ResetTokenExpiry = DateTime.Now.AddMinutes(15);
                _context.SaveChanges();

                // 2. Build reset link
                var resetLink = Url.Action("ResetPassword", "Account", new { token, email = member.Email }, Request.Scheme);

                // 3. Send email via your EmailService
                string body = $"Click here to reset your password: <a href='{resetLink}'>Reset Password</a>";
                await _emailService.SendEmailAsync(member.Email, "Reset Password", body);
            }

            // Always show generic message to avoid exposing registered emails
            ViewBag.Message = "If your email exists in our system, a password reset link has been sent.";
            return View();
        }

        // GET: /Account/ResetPassword
        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            var member = _context.Members.FirstOrDefault(m => m.Email == email && m.ResetToken == token);

            if (member == null || !member.ResetTokenExpiry.HasValue || member.ResetTokenExpiry.Value < DateTime.Now)
            {
                return View("Error"); // Invalid or expired token
            }

            ViewBag.Token = token;
            ViewBag.Email = email;
            return View();
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(string token, string email, string newPassword)
        {
            var member = _context.Members.FirstOrDefault(m => m.Email == email && m.ResetToken == token);

            if (member == null || !member.ResetTokenExpiry.HasValue || member.ResetTokenExpiry.Value < DateTime.Now)
            {
                ModelState.AddModelError("", "Invalid or expired reset token.");
                return View();
            }

            // Hash the new password and clear the reset token
            member.PasswordHash = _passwordHasher.HashPassword(member, newPassword);
            member.ResetToken = null;
            member.ResetTokenExpiry = null;
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Password reset successful! You can now log in with your new password.";
            return RedirectToAction("Login");
        }
    } 
} 
