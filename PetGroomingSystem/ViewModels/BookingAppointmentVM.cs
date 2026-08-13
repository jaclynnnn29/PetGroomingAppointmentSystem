using System;
using System.ComponentModel.DataAnnotations;

namespace PetGroomingSystem.ViewModels;

public class BookingAppointmentVM
{
    [Required(ErrorMessage = "Please select a grooming service.")]
    public int ServiceId { get; set; }

    [Required(ErrorMessage = "Please specify if your pet is a Dog or Cat.")]
    [RegularExpression("Dog|Cat", ErrorMessage = "Pet type must be either Dog or Cat.")]
    public string PetType { get; set; } = "Dog";

    [Required(ErrorMessage = "Please enter your pet's name.")]
    [StringLength(50, ErrorMessage = "Pet name cannot exceed 50 characters.")]
    public string PetName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a date.")]
    [DataType(DataType.Date)]
    [FutureDate(ErrorMessage = "Appointment date must be today or in the future.")]
    public DateTime? AppointmentDate { get; set; }

    [Required(ErrorMessage = "Please select a time slot.")]
    public string TimeSlot { get; set; } = string.Empty;

    [StringLength(250, ErrorMessage = "Special requests cannot exceed 250 characters.")]
    public string? SpecialRequests { get; set; }
}

public class FutureDateAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is DateTime dateTime)
        {
            return dateTime.Date >= DateTime.Today;
        }
        return true;
    }
}