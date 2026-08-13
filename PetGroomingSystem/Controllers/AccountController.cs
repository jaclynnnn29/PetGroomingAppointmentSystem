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
        public IActionResult Register(RegisterViewModel model)
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