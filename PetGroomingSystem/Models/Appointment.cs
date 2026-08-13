using System;
using System.ComponentModel.DataAnnotations;

namespace PetGroomingSystem.Models;

public class Appointment
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string MemberEmail { get; set; } = string.Empty;

    [Required, MaxLength(10)]
    public string PetType { get; set; } = "Dog"; 

    [Required, MaxLength(50)]
    public string PetName { get; set; } = string.Empty;

    [Required]
    public int GroomingServiceId { get; set; }

    public DateOnly Date { get; set; }

    [Required, MaxLength(50)]
    public string TimeSlot { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? SpecialRequests { get; set; }

    public string Status { get; set; } = "Confirmed";

    public GroomingService? GroomingService { get; set; }
}