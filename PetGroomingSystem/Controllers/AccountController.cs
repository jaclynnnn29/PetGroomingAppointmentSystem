using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using PetGroomingSystem.Models;
using PetGroomingSystem.ViewModels;
using System.Security.Claims;

namespace PetGroomingSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<Member> _passwordHasher;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<Member>();
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
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var member = _context.Members
                .FirstOrDefault(m => m.Email == model.Email);

            // Email not found
            if (member == null)
            {
                ModelState.AddModelError("", "Invalid email or password.");
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

                    return View(model);
                }

                _context.SaveChanges();

                ModelState.AddModelError(
                    "",
                    $"Invalid email or password. Failed attempts: {member.FailedLoginAttempts}/3"
                );

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
    }
}