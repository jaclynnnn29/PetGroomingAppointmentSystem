using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PetGroomingSystem.Models;

namespace PetGroomingSystem.Controllers;

public class HomeController(ApplicationDbContext db) : Controller
{
    // GET: Home/Index 
    public IActionResult Index()
    {
        // Fetch up to 3 services to feature on the homepage hero section
        var featuredServices = db.GroomingServices.Take(3).ToList();
        return View(featuredServices);
    }

    // GET: Home/About
    public IActionResult About()
    {
        ViewData["Title"] = "About Us";
        return View();
    }

    // GET: Home/Contact
    public IActionResult Contact()
    {
        ViewData["Title"] = "Contact & Operating Hours";
        return View();
    }

    // GET: Home/Privacy
    public IActionResult Privacy()
    {
        return View();
    }

    // GET: Home/Error
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}