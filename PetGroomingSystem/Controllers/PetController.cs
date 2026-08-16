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

        public IActionResult Index(string search)
        {
            var memberId = int.Parse(
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value
            );

            var pets = _context.Pets
                .Where(p => p.MemberID == memberId)
                .Include(p => p.Photos)
                .AsQueryable();

            // Search by pet name
            if (!string.IsNullOrWhiteSpace(search))
            {
                // Case-insensitive search
                search = search.Trim().ToLower();

                pets = pets.Where(p =>
                    p.PetName.Contains(search)
                );
            }

            ViewBag.Search = search;

            return View(pets.ToList());
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

        // =========================
        // UPLOAD PET PHOTO
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UploadPhoto(int petId, IFormFile photo)
        {
            var memberId = int.Parse(
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value
            );

            // Check whether the pet belongs to the logged-in member
            var pet = _context.Pets
                .FirstOrDefault(p =>
                    p.PetID == petId &&
                    p.MemberID == memberId
                );

            if (pet == null)
            {
                return NotFound();
            }

            // Check whether a file was selected
            if (photo == null || photo.Length == 0)
            {
                return RedirectToAction(
                    nameof(Details),
                    new { id = petId }
                );
            }

            // Only allow image files
            var allowedExtensions = new[]
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".gif",
                ".webp"
            };

            var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return RedirectToAction(
                    nameof(Details),
                    new { id = petId }
                );
            }

            // Create upload folder
            var uploadFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                "pets"
            );

            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            // Generate unique file name
            var fileName = Guid.NewGuid().ToString() + extension;

            var filePath = Path.Combine(
                uploadFolder,
                fileName
            );

            // Save physical image file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                photo.CopyTo(stream);
            }

            // Save photo information into database
            var petPhoto = new PetPhoto
            {
                PetID = petId,
                PhotoPath = "/uploads/pets/" + fileName
            };

            _context.PetPhotos.Add(petPhoto);
            _context.SaveChanges();

            return RedirectToAction(
                nameof(Details),
                new { id = petId }
            );
        }

        // =========================
        // DELETE PET PHOTO
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePhoto(int photoId)
        {
            var memberId = int.Parse(
                User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier
                )!.Value
            );

            // Find photo and make sure it belongs to the logged-in member
            var photo = _context.PetPhotos
                .Include(p => p.Pet)
                .FirstOrDefault(p =>
                    p.PetPhotoID == photoId &&
                    p.Pet.MemberID == memberId
                );

            if (photo == null)
            {
                return NotFound();
            }

            // Delete physical image file
            var filePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                photo.PhotoPath.TrimStart('/')
                    .Replace("/", Path.DirectorySeparatorChar.ToString())
            );

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            // Delete photo record from database
            var petId = photo.PetID;

            _context.PetPhotos.Remove(photo);
            _context.SaveChanges();

            return RedirectToAction(
                nameof(Details),
                new { id = petId }
            );
        }

    }
}