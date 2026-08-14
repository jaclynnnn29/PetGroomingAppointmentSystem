using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace PetGroomingSystem.Models
{
    public class PetPhoto
    {
        [Key]
        public int PetPhotoID { get; set; }

        [Required]
        public int PetID { get; set; }

        [Required]
        [StringLength(500)]
        public string PhotoPath { get; set; }

        // Relationship with Pet
        [ForeignKey("PetID")]
        public Pet Pet { get; set; }
    }
}
