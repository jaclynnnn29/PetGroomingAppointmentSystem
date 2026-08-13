using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using PetGroomingSystem.Models;
using PetGroomingSystem.ViewModels;
using System.Security.Claims;


namespace PetGroomingSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
