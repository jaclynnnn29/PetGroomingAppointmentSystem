using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

            var existingMember = _context.Members
                .FirstOrDefault(m => m.Email == model.Email);

            if (existingMember != null)
            {
                ModelState.AddModelError("Email", "This email is already registered.");
                return View(model);
            }

            var member = new Member
            {
                Name = model.Name,
                Email = model.Email,
                Phone = model.Phone,
                Role = "Customer",
                FailedLoginAttempts = 0,
                LockedUntil = null
            };

            member.PasswordHash = _passwordHasher.HashPassword(member, model.Password);

            _context.Members.Add(member);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Registration successful! You can now login.";
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

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var member = _context.Members.FirstOrDefault(m => m.Email == model.Email);

            if (member == null)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            // Check if account is currently locked out
            if (member.LockedUntil != null && member.LockedUntil > DateTime.UtcNow)
            {
                ModelState.AddModelError("", "Account locked due to multiple failed attempts. Try again later.");
                return View(model);
            }

            // Verify Password Hash
            var verificationResult = _passwordHasher.VerifyHashedPassword(member, member.PasswordHash, model.Password);

            if (verificationResult == PasswordVerificationResult.Failed)
            {
                member.FailedLoginAttempts++;
                if (member.FailedLoginAttempts >= 5)
                {
                    member.LockedUntil = DateTime.UtcNow.AddMinutes(15);
                }
                _context.SaveChanges();

                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            // Reset failed login attempts on success
            member.FailedLoginAttempts = 0;
            member.LockedUntil = null;
            _context.SaveChanges();

            // Build Claims Identity
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, member.MemberID.ToString()),
                new Claim(ClaimTypes.Name, member.Email),
                new Claim(ClaimTypes.GivenName, member.Name),
                new Claim(ClaimTypes.Role, member.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        // =========================
        // LOGOUT
        // =========================

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // =========================
        // ACCESS DENIED
        // =========================

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}