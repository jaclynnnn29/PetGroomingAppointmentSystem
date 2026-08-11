using System;
using System.ComponentModel.DataAnnotations;

namespace PetGroomingSystem.ViewModels;

public class AppointmentBookingVM
{
    [Required(ErrorMessage = "Please select a grooming service.")]
    public int ServiceId { get; set; }

    [Required(ErrorMessage = "Please select a date.")]
    [DataType(DataType.Date)]
    public DateTime? AppointmentDate { get; set; }

    [Required(ErrorMessage = "Please select a time slot.")]
    public string TimeSlot { get; set; } = string.Empty;

    [StringLength(250, ErrorMessage = "Special requests cannot exceed 250 characters.")]
    public string? SpecialRequests { get; set; }
}