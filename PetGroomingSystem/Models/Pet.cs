using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetGroomingSystem.Models
{
    public class Pet
    {
        [Key]
        public int PetID { get; set; }

        [Required]
        public int MemberID { get; set; }

        [Required]
        [StringLength(100)]
        public string PetName { get; set; }

        [Required]
        [StringLength(50)]
        public string PetType { get; set; }

        [StringLength(100)]
        public string Breed { get; set; }

        [Required]
        [StringLength(20)]
        public string Gender { get; set; }

        [Range(0, 50)]
        public int Age { get; set; }

        [Range(0, 200)]
        [Column(TypeName = "decimal(6,2)")]
        public decimal Weight { get; set; }

        [StringLength(500)]
        public string MedicalNotes { get; set; }

        // Relationship with Member
        [ForeignKey("MemberID")]
        public Member Member { get; set; }

        // Multiple photos
        public ICollection<PetPhoto> Photos { get; set; } = new List<PetPhoto>();
    }
}
