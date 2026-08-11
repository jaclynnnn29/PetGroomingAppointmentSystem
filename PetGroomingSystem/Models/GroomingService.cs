using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace PetGroomingSystem.Models;

public class GroomingService
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Precision(6, 2)]
    public decimal Price { get; set; }

    public string Description { get; set; } = string.Empty;
}