using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetGroomingSystem.Models;

namespace PetGroomingSystem.Controllers
{
    [Authorize]
    public class PetController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PetController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // VIEW MY PETS
        // =========================

        public IActionResult Index()
        {
            var memberId = int.Parse(
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value
            );

            var pets = _context.Pets
                .Where(p => p.MemberID == memberId)
                .Include(p => p.Photos)
                .ToList();

            return View(pets);
        }

        // =========================
        // PET DETAILS
        // =========================

        public IActionResult Details(int id)
        {
            var memberId = int.Parse(
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value
            );

            var pet = _context.Pets
                .Include(p => p.Photos)
                .FirstOrDefault(p =>
                    p.PetID == id &&
                    p.MemberID == memberId
                );

            if (pet == null)
            {
                return NotFound();
            }

            return View(pet);
        }

        // =========================
        // ADD PET - GET
        // =========================

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // =========================
        // ADD PET - POST
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Pet pet)
        {
            var memberId = int.Parse(
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value
            );

            // Important:
            // Do not allow users to choose another MemberID.
            // Automatically assign pet to logged-in member
            pet.MemberID = memberId;

            if (!ModelState.IsValid)
            {
                return View(pet);
            }

            

            _context.Pets.Add(pet);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // EDIT PET - GET
        // =========================

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var memberId = int.Parse(
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value
            );

            var pet = _context.Pets
                .FirstOrDefault(p =>
                    p.PetID == id &&
                    p.MemberID == memberId
                );

            if (pet == null)
            {
                return NotFound();
            }

            return View(pet);
        }

        // =========================
        // EDIT PET - POST
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Pet pet)
        {
            var memberId = int.Parse(
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value
            );

            if (id != pet.PetID)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(pet);
            }

            var existingPet = _context.Pets
                .FirstOrDefault(p =>
                    p.PetID == id &&
                    p.MemberID == memberId
                );

            if (existingPet == null)
            {
                return NotFound();
            }

            existingPet.PetName = pet.PetName;
            existingPet.PetType = pet.PetType;
            existingPet.Breed = pet.Breed;
            existingPet.Gender = pet.Gender;
            existingPet.Age = pet.Age;
            existingPet.Weight = pet.Weight;
            existingPet.MedicalNotes = pet.MedicalNotes;

            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // DELETE PET - GET
        // =========================

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var memberId = int.Parse(
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value
            );

            var pet = _context.Pets
                .FirstOrDefault(p =>
                    p.PetID == id &&
                    p.MemberID == memberId
                );

            if (pet == null)
            {
                return NotFound();
            }

            return View(pet);
        }

        // =========================
        // DELETE PET - POST
        // =========================

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var memberId = int.Parse(
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value
            );

            var pet = _context.Pets
                .FirstOrDefault(p =>
                    p.PetID == id &&
                    p.MemberID == memberId
                );

            if (pet == null)
            {
                return NotFound();
            }

            _context.Pets.Remove(pet);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }
    }
}