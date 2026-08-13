using System.ComponentModel.DataAnnotations;

namespace PetGroomingSystem.Models
{
    public class Member
    {
        [Key]
        public int MemberID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(20)]
        public string Phone { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        [StringLength(20)]
        public string Role { get; set; } = "Customer";

        // Security feature
        public int FailedLoginAttempts { get; set; } = 0;

        public DateTime? LockedUntil { get; set; }

        // Navigation property
        public ICollection<Pet> Pets { get; set; } = new List<Pet>();
    }
}

